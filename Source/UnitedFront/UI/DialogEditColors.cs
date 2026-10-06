using System.Collections.Generic;
using RimWorld;
using UnitedFront.Comps;
using UnitedFront.Utils;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UnitedFront.UI
{
    [StaticConstructorOnStartup]
    public sealed class DialogEditColors : Window
    {
        private sealed class Piece
        {
            public Apparel Apparel = null!;
            public CompColorMarker Comp = null!;
            public List<Color> Working = null!;
            public List<Color> Original = null!;
        }

        private const int ColorCount = 2;

        private readonly Pawn _pawn;
        private readonly List<Piece> _pieces = new List<Piece>();
        private int _sel;
        private bool _committed;
        private List<Color>? _allColors;

        private static readonly Vector2 ButSize = new Vector2(200f, 40f);
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
        private const float PortraitZoom = 1.3f;
        private const float LeftRectPercent = 0.42f;
        private const float TabMargin = 18f;

        private static readonly Color LockedTint = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color LockedVeil = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color LockedBoxFill = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        private static readonly Color LockedBoxBorder = new Color(1f, 1f, 1f, 0.45f);
        private const float LockedBoxPad = 12f;

        private const float SwatchSize = 22f;
        private const float SwatchPad = 2f;
        private const float SectionLabelH = 18f;
        private const float SectionGap = 6f;
        private static readonly Color SectionLabelColor = new Color(1f, 1f, 1f, 0.6f);

        private static readonly Texture2D FavoriteColorTex =
            ContentFinder<Texture2D>.Get("UI/Icons/ColorSelector/ColorFavourite");

        private static readonly Texture2D IdeoColorTex =
            ContentFinder<Texture2D>.Get("UI/Icons/ColorSelector/ColorIdeology");

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
                    CompColorMarker? comp = ap.TryGetComp<CompColorMarker>();
                    if (comp == null) continue;

                    _pieces.Add(new Piece
                    {
                        Apparel = ap,
                        Comp = comp,
                        Working = Normalized(comp.ZoneColors),
                        Original = new List<Color>(comp.ZoneColors)
                    });
                }
            }
        }

        private static List<Color> Normalized(List<Color> source)
        {
            var list = new List<Color>(source);
            while (list.Count < ColorCount) list.Add(Color.white);
            if (list.Count > ColorCount) list.RemoveRange(ColorCount, list.Count - ColorCount);
            return list;
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
            bool locked = ZoneLocked(index);
            Color prevGUI = GUI.color;
            if (locked)
            {
                GUI.color = LockedTint;
                if (Event.current.type == EventType.MouseDown && Mouse.IsOver(row)) Event.current.Use();
            }

            float labelH = 26f;
            float defaultH = 26f;
            float btnH = 24f;
            float gap = 8f;

            Widgets.Label(new Rect(row.x, row.y, row.width, labelH),
                index == 0 ? "UFR_ColorPrimary".Translate() : "UFR_ColorSecondary".Translate());

            Color c = p.Working[index];
            Color original = c;
            Color defColor = p.Comp.DefaultZone(index);

            Rect btnRow = new Rect(row.x, row.yMax - btnH, row.width, btnH);
            Rect paletteArea = new Rect(row.x, row.y + labelH + defaultH + 4f, row.width,
                                        row.height - labelH - defaultH - 4f - btnH - gap);

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

            float roleH = SectionHeight(paletteArea.width, UFRColors.Palette.Count);

            Rect roleLabel = new Rect(paletteArea.x, paletteArea.y, paletteArea.width, SectionLabelH);
            Rect roleRect = new Rect(paletteArea.x, roleLabel.yMax, paletteArea.width, roleH);
            Rect otherLabel = new Rect(paletteArea.x, roleRect.yMax + SectionGap, paletteArea.width, SectionLabelH);
            Rect otherRect = new Rect(paletteArea.x, otherLabel.yMax, paletteArea.width,
                                      paletteArea.yMax - otherLabel.yMax);

            DrawSectionLabel(roleLabel, "UFR_ColorSectionRole".Translate());
            Widgets.ColorSelector(roleRect, ref c, UFRColors.Palette, out _, null, 22, 2);

            DrawSectionLabel(otherLabel, "UFR_ColorSectionOther".Translate());
            Widgets.ColorSelector(otherRect, ref c, AllColors(), out _, null, 22, 2, ColorSelecterExtraOnGUI);

            List<string> labels = new List<string>();
            List<Color> picks = new List<Color>();

            labels.Add("UFR_ColorRandom".Translate());
            picks.Add(Color.clear);

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

            if (locked)
            {
                GUI.color = prevGUI;
                DrawLockedVeil(row);
                return;
            }

            if (!original.IndistinguishableFrom(c))
            {
                p.Working[index] = c;
                p.Comp.PreviewZones(p.Working);
                PortraitsCache.SetDirty(_pawn);
            }
        }

        private void ColorSelecterExtraOnGUI(Color color, Rect boxRect)
        {
            Texture2D? icon = null;
            TaggedString tip = default;
            bool over = Mouse.IsOver(boxRect);

            if (TryGetFavoriteColor(_pawn, out Color fav) && color.IndistinguishableFrom(fav))
            {
                icon = FavoriteColorTex;
                if (over) tip = "FavoriteColorPickerTip".Translate(_pawn.Named("PAWN"));
            }
            else if (ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode
                     && color.IndistinguishableFrom(_pawn.Ideo.ApparelColor))
            {
                icon = IdeoColorTex;
                if (over) tip = "IdeoColorPickerTip".Translate(_pawn.Named("PAWN"));
            }

            if (icon != null)
            {
                Rect position = boxRect.ContractedBy(4f);
                GUI.color = Color.black.ToTransparent(0.2f);
                GUI.DrawTexture(new Rect(position.x + 2f, position.y + 2f, position.width, position.height), icon);
                GUI.color = Color.white.ToTransparent(0.8f);
                GUI.DrawTexture(position, icon);
                GUI.color = Color.white;
            }

            if (!tip.NullOrEmpty()) TooltipHandler.TipRegion(boxRect, tip);
        }

        private static void DrawLockedVeil(Rect row)
        {
            Widgets.DrawBoxSolid(row, LockedVeil);

            TextAnchor anchor = Text.Anchor;
            GameFont font = Text.Font;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;

            string label = "UFR_ColorPrimaryLocked".Translate();
            float maxW = row.width - LockedBoxPad * 4f;
            Vector2 size = Text.CalcSize(label);
            float boxW = Mathf.Min(size.x, maxW) + LockedBoxPad * 2f;
            float boxH = Mathf.Max(Text.CalcHeight(label, boxW - LockedBoxPad * 2f), size.y) + LockedBoxPad * 2f;

            Rect box = new Rect(0f, 0f, boxW, boxH) { center = row.center };
            Widgets.DrawBoxSolid(box, LockedBoxFill);

            Color prev = GUI.color;
            GUI.color = LockedBoxBorder;
            Widgets.DrawBox(box);
            GUI.color = prev;

            Widgets.Label(box.ContractedBy(LockedBoxPad), label);

            Text.Font = font;
            Text.Anchor = anchor;
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
                    p.Working = Normalized(p.Original);
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

            _allColors = new List<Color>();

            if (ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode)
                AddUnique(_allColors, _pawn.Ideo.ApparelColor);

            if (TryGetFavoriteColor(_pawn, out Color favColor))
                AddUnique(_allColors, favColor);

            foreach (ColorDef def in DefDatabase<ColorDef>.AllDefs)
            {
                bool allowed = def.colorType == ColorType.Ideo
                            || def.colorType == ColorType.Misc;

                if (allowed) AddUnique(_allColors, def.color);
            }

            _allColors.SortByColor((Color x) => x);
            return _allColors;
        }

        private static float SectionHeight(float width, int count)
        {
            int perRow = Mathf.Max(1, Mathf.FloorToInt(width / (SwatchSize + SwatchPad * 2f)));
            int rows = Mathf.CeilToInt(count / (float)perRow);
            return rows * (SwatchSize + SwatchPad * 2f);
        }

        private static void DrawSectionLabel(Rect rect, string label)
        {
            GameFont font = Text.Font;
            Color prev = GUI.color;
            Text.Font = GameFont.Tiny;
            GUI.color = SectionLabelColor;
            Widgets.Label(rect, label);
            GUI.color = prev;
            Text.Font = font;
        }

        private static void AddUnique(List<Color> list, Color color)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].IndistinguishableFrom(color)) return;
            list.Add(color);
        }

        private bool ZoneLocked(int index) => index == 0 && ModsConfig.IdeologyActive;

        private static bool IsHelmet(Apparel ap)
        {
            List<BodyPartGroupDef>? groups = ap.def.apparel?.bodyPartGroups;
            if (groups == null) return false;
            return groups.Contains(BodyPartGroupDefOf.UpperHead) || groups.Contains(BodyPartGroupDefOf.FullHead);
        }

        private static bool TryGetFavoriteColor(Pawn pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive || pawn?.story == null || pawn.DevelopmentalStage.Baby()) return false;
            ColorDef? def = pawn.story.favoriteColor;
            if (def == null) return false;
            c = def.color;
            return true;
        }
    }
}