using System.Collections.Generic;
using System.Text.RegularExpressions;
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

            public readonly float[] H = new float[ColorCount];
            public readonly float[] S = new float[ColorCount];
            public readonly float[] V = new float[ColorCount];

            public void SyncHsv()
            {
                for (int i = 0; i < ColorCount; i++)
                    Color.RGBToHSV(Working[i], out H[i], out S[i], out V[i]);
            }
        }

        private const int ColorCount = 2;

        private readonly Pawn _pawn;
        private readonly List<Piece> _pieces = new List<Piece>();
        private int _sel;
        private bool _committed;
        private List<Color> _allColors;

        private readonly ColorPicker[] _pickers = new ColorPicker[ColorCount];
        private readonly Vector2[] _paletteScroll = new Vector2[ColorCount];
        private readonly float[] _paletteHeight = new float[ColorCount];

        private static readonly Vector2 ButSize = new Vector2(200f, 40f);
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
        private const float PortraitZoom = 1.3f;
        private const float TabMargin = 18f;
        private const float MinPaletteWidth = 140f;
        private const float PickerGap = 10f;

        private const float PortraitWidth = 380f;
        private const float PaletteTargetWidth = 288f;
        private const float PickerSquare = 220f;
        private const float DefaultRowH = 26f;
        private const float ButtonRowH = 24f;
        private const float BodyGap = 8f;
        private const float ZoneGap = 16f;
        private const float TitleHeight = 64f;
        private const float FieldColumnWidth = 96f;
        private const float FieldLabelWidth = 14f;
        private const float FieldRowH = 24f;
        private const float FieldRowGap = 6f;
        private const float ZonePad = 8f;
        private const float ZoneLabelH = 24f;

        private static readonly Color ZoneFill = new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color ZoneBorder = new Color(1f, 1f, 1f, 0.35f);

        private static readonly string[] ChannelKeys = { "UFR_ColorChannelR", "UFR_ColorChannelG", "UFR_ColorChannelB" };
        private static readonly Regex HexPattern = new Regex("^[0-9a-fA-F]*$");
        private static readonly Regex BytePattern = new Regex("^[0-9]*$");

        private string _editControl;
        private string _editBuffer;

        private static float PickerBlockHeight => PickerSquare + 20f;
        private static float PickerBlockWidth => ColorPicker.WidthFor(PickerBlockHeight);

        private static float ZoneWidth =>
            ZonePad * 2f + PaletteTargetWidth + PickerGap + FieldColumnWidth + PickerGap + PickerBlockWidth;

        private static float ZoneHeight =>
            ZonePad * 2f + ZoneLabelH + 4f + DefaultRowH + 4f + PickerBlockHeight + BodyGap + ButtonRowH;

        public override Vector2 InitialSize => new Vector2(
            PortraitWidth + 10f + ZoneWidth + TabMargin * 2f + StandardMargin * 2f,
            TitleHeight + 4f + ZoneHeight * ColorCount + ZoneGap + TabMargin * 2f
                + ButSize.y + 4f + StandardMargin * 2f);

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 size = new Vector2(
                Mathf.Min(InitialSize.x, Verse.UI.screenWidth),
                Mathf.Min(InitialSize.y, Verse.UI.screenHeight - 35f));

            windowRect = new Rect((Verse.UI.screenWidth - size.x) / 2f, (Verse.UI.screenHeight - size.y) / 2f,
                                  size.x, size.y).Rounded();
        }

        public DialogEditColors(Pawn pawn)
        {
            _pawn = pawn;
            forcePause = true;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            for (int i = 0; i < ColorCount; i++) _pickers[i] = new ColorPicker();

            if (pawn.apparel != null)
            {
                foreach (Apparel ap in pawn.apparel.WornApparel)
                {
                    CompColorMarker comp = ap.TryGetComp<CompColorMarker>();
                    if (comp == null) continue;

                    var piece = new Piece
                    {
                        Apparel = ap,
                        Comp = comp,
                        Working = Normalized(comp.ZoneColors),
                        Original = new List<Color>(comp.ZoneColors)
                    };
                    piece.SyncHsv();
                    _pieces.Add(piece);
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
            leftRect.width = Mathf.Min(PortraitWidth, inRect.width * 0.45f);
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

            float rowGap = ZoneGap;
            float rowH = (rect.height - rowGap * (ColorCount - 1)) / ColorCount;
            for (int c = 0; c < ColorCount; c++)
            {
                Rect zone = new Rect(rect.x, rect.y + c * (rowH + rowGap), rect.width, rowH);
                DrawColorRow(zone, p, c);
            }
        }

        private void DrawColorRow(Rect outer, Piece p, int index)
        {
            float defaultH = DefaultRowH;
            float btnH = ButtonRowH;
            float gap = BodyGap;

            Color c = p.Working[index];
            Color original = c;

            DrawZoneFrame(outer);

            Rect row = new Rect(outer.x + ZonePad, outer.y + ZonePad,
                                outer.width - ZonePad * 2f, outer.height - ZonePad * 2f);

            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, row.width, ZoneLabelH),
                index == 0 ? "UFR_ColorPrimary".Translate() : "UFR_ColorSecondary".Translate());
            Text.Anchor = anchor;

            bool hasDefault = TryGetDefaultColor(p, index, out Color defColor);
            float labelBlockH = ZoneLabelH + 4f;
            float defaultBlockH = defaultH + 4f;

            Rect btnRow = new Rect(row.x, row.yMax - btnH, row.width, btnH);
            Rect body = new Rect(row.x, row.y + labelBlockH + defaultBlockH, row.width,
                                 row.height - labelBlockH - defaultBlockH - btnH - gap);

            Rect defRow = new Rect(row.x, row.y + labelBlockH, row.width, defaultH);

            // Default swatch + button, sitting directly above the palette.
            if (hasDefault)
            {
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

            float pickerH = body.height;
            float pickerW = ColorPicker.WidthFor(pickerH);
            float maxPickerW = body.width - MinPaletteWidth - FieldColumnWidth - PickerGap * 2f;
            if (pickerW > maxPickerW)
            {
                pickerW = Mathf.Max(maxPickerW, 0f);
                pickerH = Mathf.Max(ColorPicker.HeightFor(pickerW), 0f);
            }

            Rect pickerRect = new Rect(body.xMax - pickerW, body.y, pickerW, pickerH);
            Rect fieldCol = new Rect(pickerRect.x - PickerGap - FieldColumnWidth, body.y, FieldColumnWidth, body.height);
            Rect palette = new Rect(body.x, body.y, Mathf.Max(fieldCol.x - PickerGap - body.x, 0f), body.height);

            DrawPalette(palette, ref c, index);
            DrawFieldColumn(fieldCol, index, ref c);

            float h = p.H[index], s = p.S[index], v = p.V[index];
            bool pickerChanged = pickerH > 0f && _pickers[index].Draw(pickerRect, ref h, ref s, ref v);

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

            if (pickerChanged)
            {
                p.H[index] = h;
                p.S[index] = s;
                p.V[index] = v;

                Color picked = Color.HSVToRGB(h, s, v);
                if (!p.Working[index].IndistinguishableFrom(picked))
                {
                    p.Working[index] = picked;
                    Apply(p);
                }
            }
            else if (!original.IndistinguishableFrom(c))
            {
                p.Working[index] = c;
                Color.RGBToHSV(c, out p.H[index], out p.S[index], out p.V[index]);
                Apply(p);
            }
        }

        private static void DrawZoneFrame(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ZoneFill);

            Color prev = GUI.color;
            GUI.color = ZoneBorder;
            Widgets.DrawBox(rect);
            GUI.color = prev;
        }

        private void DrawFieldColumn(Rect col, int index, ref Color c)
        {
            TextAnchor anchor = Text.Anchor;
            float fieldW = col.width - FieldLabelWidth - 4f;

            for (int ch = 0; ch < 3; ch++)
            {
                Rect line = new Rect(col.x, col.y + ch * (FieldRowH + FieldRowGap), col.width, FieldRowH);

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(line.x, line.y, FieldLabelWidth, line.height), ChannelKeys[ch].Translate());
                Text.Anchor = anchor;

                int current = Mathf.RoundToInt(Mathf.Clamp01(c[ch]) * 255f);
                Rect field = new Rect(line.x + FieldLabelWidth + 4f, line.y, fieldW, line.height);

                string typed = DrawBufferedField(field, "UFR_rgb_" + _sel + "_" + index + "_" + ch,
                    current.ToString(), 3, BytePattern, out bool edited);

                if (!edited || !int.TryParse(typed, out int parsed)) continue;

                parsed = Mathf.Clamp(parsed, 0, 255);
                if (parsed != current) c[ch] = parsed / 255f;
            }

            Rect hexLine = new Rect(col.x, col.y + 3f * (FieldRowH + FieldRowGap) + 6f, col.width, FieldRowH);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(hexLine.x, hexLine.y, FieldLabelWidth, hexLine.height), "UFR_ColorHexPrefix".Translate());
            Text.Anchor = anchor;

            Rect hexField = new Rect(hexLine.x + FieldLabelWidth + 4f, hexLine.y, fieldW, hexLine.height);
            string hexTyped = DrawBufferedField(hexField, "UFR_hex_" + _sel + "_" + index,
                ColorUtility.ToHtmlStringRGB(c), 6, HexPattern, out bool hexEdited);

            if (hexEdited && hexTyped.Length == 6 &&
                ColorUtility.TryParseHtmlString("#" + hexTyped, out Color parsedHex) &&
                !parsedHex.IndistinguishableFrom(c))
            {
                c = parsedHex;
            }
        }

        private string DrawBufferedField(Rect rect, string name, string display, int maxLength, Regex validator, out bool edited)
        {
            if (_editControl == name && GUI.GetNameOfFocusedControl() != name) _editControl = null;

            string current = _editControl == name ? _editBuffer : display;

            GUI.SetNextControlName(name);
            string result = Widgets.TextField(rect, current, maxLength, validator);

            if (result != current)
            {
                _editControl = name;
                _editBuffer = result;
            }

            edited = _editControl == name;
            return result;
        }

        private void DrawPalette(Rect rect, ref Color c, int index)
        {
            if (rect.width < 40f || rect.height < 40f) return;

            Rect view = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(_paletteHeight[index], rect.height));
            Widgets.BeginScrollView(rect, ref _paletteScroll[index], view);
            Widgets.ColorSelector(view, ref c, AllColors(), out _paletteHeight[index], null, 22, 2);
            Widgets.EndScrollView();
        }

        private void Apply(Piece p)
        {
            p.Comp.PreviewZones(p.Working);
            PortraitsCache.SetDirty(_pawn);
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
                    p.Working = Normalized(p.Original);
                    p.SyncHsv();
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