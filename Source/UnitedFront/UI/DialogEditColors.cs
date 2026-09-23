using System.Collections.Generic;
using RimWorld;
using UnitedFront.Comps;
using UnitedFront.Defs;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UnitedFront.UI
{
    public sealed class DialogEditColors : Window
    {
        private sealed class Piece
        {
            public Apparel Apparel;
            public CompColorMarker Comp;
            public List<Color> Working;
            public List<Color> Original;
        }

        private const int ColorCount = 2;

        private readonly Pawn _pawn;
        private readonly List<Piece> _pieces = new List<Piece>();
        private int _sel;
        private bool _committed;
        private List<Color> _allColors;

        private static readonly Vector2 ButSize = new Vector2(200f, 40f);
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
        private const float PortraitZoom = 1.3f;
        private const float LeftRectPercent = 0.42f;
        private const float TabMargin = 18f;

        public override Vector2 InitialSize => new Vector2(1000f, 760f);

        public DialogEditColors(Pawn pawn)
        {
            _pawn = pawn;
            forcePause = true;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            if (pawn.apparel != null)
            {
                foreach (Apparel ap in pawn.apparel.WornApparel)
                {
                    CompColorMarker comp = ap.TryGetComp<CompColorMarker>();
                    if (comp == null) continue;

                    var working = new List<Color>(comp.ZoneColors);
                    while (working.Count < ColorCount) working.Add(Color.white);
                    if (working.Count > ColorCount) working.RemoveRange(ColorCount, working.Count - ColorCount);

                    _pieces.Add(new Piece
                    {
                        Apparel = ap,
                        Comp = comp,
                        Working = working,
                        Original = new List<Color>(comp.ZoneColors)
                    });
                }
            }
        }

        private static bool IsHelmet(Apparel ap)
        {
            List<BodyPartGroupDef> groups = ap.def.apparel?.bodyPartGroups;
            if (groups == null) return false;
            return groups.Contains(BodyPartGroupDefOf.UpperHead) || groups.Contains(BodyPartGroupDefOf.FullHead);
        }

        public override void Close(bool doCloseSound = true)
        {
            foreach (Piece p in _pieces)
            {
                if (_committed) p.Comp.CommitZones(p.Working);
                else p.Comp.PreviewZones(p.Original);
            }
            PortraitsCache.SetDirty(_pawn);
            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_pawn.Destroyed) { Close(false); return; }

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect) { height = Text.LineHeight * 2f };
            Widgets.Label(titleRect, "UFR_EditColorsTitle".Translate(_pawn.Name.ToStringShort));
            Text.Font = GameFont.Small;
            inRect.yMin = titleRect.yMax + 4f;

            if (_pieces.Count == 0)
            {
                Widgets.NoneLabelCenteredVertically(new Rect(inRect.x, inRect.y, inRect.width, inRect.height - ButSize.y));
                DrawBottomButtons(inRect);
                return;
            }

            Rect leftRect = inRect;
            leftRect.width *= LeftRectPercent;
            leftRect.yMax -= ButSize.y + 4f;
            DrawPawn(leftRect);

            Rect rightRect = inRect;
            rightRect.xMin = leftRect.xMax + 10f;
            rightRect.yMax -= ButSize.y + 4f;
            DrawRight(rightRect);

            DrawBottomButtons(inRect);
        }

        private void DrawPawn(Rect rect)
        {
            Widgets.BeginGroup(rect);
            Rect inner = new Rect(0f, 0f, rect.width, rect.height).ContractedBy(4f);
            RenderTexture portrait = PortraitsCache.Get(
                _pawn, new Vector2(inner.width, inner.height), Rot4.South,
                PortraitOffset, PortraitZoom,
                supersample: true, compensateForUIScale: true,
                renderHeadgear: true, renderClothes: true);
            GUI.DrawTexture(inner, portrait);
            Widgets.EndGroup();
        }

        private void DrawRight(Rect rect)
        {
            var tabs = new List<TabRecord>(_pieces.Count);
            for (int i = 0; i < _pieces.Count; i++)
            {
                int idx = i;
                string label = IsHelmet(_pieces[i].Apparel) ? "UFR_ColorTab_Helmet".Translate() : "UFR_ColorTab_Armor".Translate();
                tabs.Add(new TabRecord(label, () => _sel = idx, _sel == i));
            }

            Widgets.DrawMenuSection(rect);
            TabDrawer.DrawTabs(rect, tabs);
            rect = rect.ContractedBy(TabMargin);

            if (_sel < 0 || _sel >= _pieces.Count) _sel = 0;
            Piece p = _pieces[_sel];

            float rowGap = 14f;
            float rowH = (rect.height - rowGap * (ColorCount - 1)) / ColorCount;
            for (int c = 0; c < ColorCount; c++)
            {
                Rect row = new Rect(rect.x, rect.y + c * (rowH + rowGap), rect.width, rowH);
                DrawColorRow(row, p, c);
            }
        }

        private void DrawColorRow(Rect row, Piece p, int index)
        {
            float labelH = 26f;
            float defaultH = 26f;
            float btnH = 24f;
            float gap = 8f;

            Widgets.Label(new Rect(row.x, row.y, row.width, labelH),
                index == 0 ? "UFR_ColorPrimary".Translate() : "UFR_ColorSecondary".Translate());

            Color c = p.Working[index];
            Color original = c;

            bool hasDefault = TryGetDefaultColor(p, index, out Color defColor);
            float defaultBlockH = hasDefault ? defaultH + 4f : 0f;

            Rect btnRow = new Rect(row.x, row.yMax - btnH, row.width, btnH);
            Rect palette = new Rect(row.x, row.y + labelH + defaultBlockH, row.width,
                                    row.height - labelH - defaultBlockH - btnH - gap);

            // Default swatch + button, sitting directly above the palette.
            if (hasDefault)
            {
                Rect defRow = new Rect(row.x, row.y + labelH, row.width, defaultH);

                Rect swatch = new Rect(defRow.x, defRow.y + 2f, defaultH - 4f, defaultH - 4f);
                Widgets.DrawBoxSolid(swatch, defColor);
                Widgets.DrawBox(swatch);
                if (defColor.IndistinguishableFrom(c)) Widgets.DrawBox(swatch.ExpandedBy(1f), 2);

                Rect defBtn = new Rect(swatch.xMax + 6f, defRow.y, 170f, defaultH);
                if (Widgets.ButtonText(defBtn, "UFR_ColorDefault".Translate()))
                {
                    c = defColor;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            float paletteHeight;
            Widgets.ColorSelector(palette, ref c, AllColors(), out paletteHeight, null, 22, 2);

            // Remaining quick-pick buttons, laid out evenly along the bottom row.
            List<string> labels = new List<string>();
            List<Color> picks = new List<Color>();

            labels.Add("UFR_ColorRandom".Translate());
            picks.Add(Color.clear);                 // index 0 is handled as random

            if (TryGetFavoriteColor(_pawn, out Color favColor))
            {
                labels.Add("UFR_ColorFavorite".Translate());
                picks.Add(favColor);
            }
            if (ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode)
            {
                labels.Add("UFR_ColorIdeoligion".Translate());
                picks.Add(_pawn.Ideo.ApparelColor);
            }

            float btnGap = 6f;
            float btnW = (btnRow.width - btnGap * (labels.Count - 1)) / labels.Count;
            for (int i = 0; i < labels.Count; i++)
            {
                Rect r = new Rect(btnRow.x + i * (btnW + btnGap), btnRow.y, btnW, btnH);
                if (!Widgets.ButtonText(r, labels[i])) continue;

                if (i == 0)
                {
                    List<Color> colors = AllColors();
                    if (colors.Count > 0) c = colors[Rand.Range(0, colors.Count)];
                }
                else
                {
                    c = picks[i];
                }
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            if (!original.IndistinguishableFrom(c))
            {
                p.Working[index] = c;
                p.Comp.PreviewZones(p.Working);
                PortraitsCache.SetDirty(_pawn);
            }
        }

        private static bool TryGetDefaultColor(Piece p, int index, out Color c)
        {
            c = Color.white;
            Color drawColor = p.Apparel.DrawColor;
            ArmorColorExtension ext = p.Apparel.def.GetModExtension<ArmorColorExtension>();

            if (ext == null)
            {
                c = drawColor;
                return true;
            }

            if (index == 0) c = ext.setColorOne ? ext.colorOne : drawColor;
            else if (index == 1) c = ext.setColorTwo ? ext.colorTwo : drawColor;
            else return false;

            return true;
        }

        private void DrawBottomButtons(Rect inRect)
        {
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "UFR_Cancel".Translate()))
            {
                _committed = false;
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMin + inRect.width / 2f - ButSize.x / 2f, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "UFR_Reset".Translate()))
            {
                foreach (Piece p in _pieces)
                {
                    p.Working = new List<Color>(p.Original);
                    while (p.Working.Count < ColorCount) p.Working.Add(Color.white);
                    p.Comp.PreviewZones(p.Working);
                }
                PortraitsCache.SetDirty(_pawn);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMax - ButSize.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "UFR_Accept".Translate()))
            {
                _committed = true;
                Close();
            }
        }

        private List<Color> AllColors()
        {
            if (_allColors != null) return _allColors;

            HashSet<Color> colorSet = new HashSet<Color>();

            if (ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode)
                colorSet.Add(_pawn.Ideo.ApparelColor);

            if (TryGetFavoriteColor(_pawn, out Color favColor))
                colorSet.Add(favColor);

            foreach (ColorDef def in DefDatabase<ColorDef>.AllDefs)
            {
                if (def.colorType == ColorType.Ideo || def.colorType == ColorType.Misc || def.colorType == ColorType.Structure)
                {
                    bool duplicate = false;
                    foreach (Color c in colorSet)
                    {
                        if (c.IndistinguishableFrom(def.color))
                        {
                            duplicate = true;
                            break;
                        }
                    }
                    if (!duplicate)
                        colorSet.Add(def.color);
                }
            }

            _allColors = new List<Color>(colorSet);
            _allColors.Sort((a, b) =>
            {
                Color.RGBToHSV(a, out float hA, out float sA, out _);
                Color.RGBToHSV(b, out float hB, out float sB, out _);
                int cmp = hA.CompareTo(hB);
                return (cmp != 0) ? cmp : sA.CompareTo(sB);
            });
            return _allColors;
        }

        private static bool TryGetFavoriteColor(Pawn pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive || pawn?.story == null || pawn.DevelopmentalStage.Baby()) return false;
            ColorDef def = pawn.story.favoriteColor;
            if (def == null) return false;
            c = def.color;
            return true;
        }
    }
}