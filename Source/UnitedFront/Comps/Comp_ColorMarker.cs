using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompColorMarker : ThingComp
    {
        public List<Color> ZoneColors = new List<Color>();
        private bool _zonesCustomized;

        private const float MinBrightness = 0.3f;

        private Graphic? _cachedItemGraphic;
        private bool _recacheItemGraphic = true;

        public CompPropertiesColorMarker Props => (CompPropertiesColorMarker)props;
        public int ZoneCount => Props.zoneCount;

        private CompColorable? Colorable => parent.TryGetComp<CompColorable>();

        private Color BaseColor
        {
            get
            {
                if (parent.Stuff != null) return parent.def.GetColorForStuff(parent.Stuff);
                return parent.def.graphicData != null ? parent.def.graphicData.color : Color.white;
            }
        }

        public Color DefaultZone(int index)
        {
            if (index == 0 && Props.setColorOne) return Props.colorOne;
            if (index == 1 && Props.setColorTwo) return Props.colorTwo;
            return BaseColor;
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            ApplyDefaults();
        }

        private void ApplyDefaults()
        {
            if (_zonesCustomized) return;

            EnsureZoneDefaults();
            for (int i = 0; i < ZoneColors.Count; i++)
                ZoneColors[i] = DefaultZone(i);

            SetDirty();
        }

        private void EnsureZoneDefaults()
        {
            ZoneColors ??= new List<Color>();
            while (ZoneColors.Count < ZoneCount)
                ZoneColors.Add(DefaultZone(ZoneColors.Count));
            if (ZoneColors.Count > ZoneCount)
                ZoneColors.RemoveRange(ZoneCount, ZoneColors.Count - ZoneCount);
        }

        public Color GetZone(int index) => (index >= 0 && index < ZoneColors.Count) ? ZoneColors[index] : Color.white;

        public static Color Floor(Color colour)
        {
            float max = Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b));
            if (max >= MinBrightness || max <= 0.0001f) return colour;

            float scale = MinBrightness / max;
            return new Color(Mathf.Min(colour.r * scale, 1f),
                             Mathf.Min(colour.g * scale, 1f),
                             Mathf.Min(colour.b * scale, 1f),
                             colour.a);
        }

        public Color DisplayZone(int index)
        {
            if (parent is Apparel ap && ap.WornByCorpse)
                return PawnRenderUtility.GetRottenColor(GetZone(index));
            return Floor(GetZone(index));
        }

        public static Color DisplayPrimary(Apparel apparel, Color vanillaColor)
        {
            return apparel.WornByCorpse ? vanillaColor : Floor(vanillaColor);
        }

        public Graphic? ItemGraphic()
        {
            if (parent.StyleDef?.Graphic != null) return null;

            if (_recacheItemGraphic)
            {
                _recacheItemGraphic = false;
                _cachedItemGraphic = BuildItemGraphic();
            }
            return _cachedItemGraphic;
        }

        private Graphic? BuildItemGraphic()
        {
            if (parent is not Apparel apparel) return null;
            if (apparel.StyleDef?.Graphic != null) return null;
            if (ZoneColors.Count < 2) return null;

            GraphicData gd = parent.def.graphicData;
            if (gd == null || gd.texPath.NullOrEmpty()) return null;
            if (gd.shaderType?.Shader != ShaderDatabase.CutoutComplex) return null;

            Color colorOne = DisplayPrimary(apparel, apparel.DrawColor);
            Color colorTwo = DisplayZone(1);
            if (colorTwo.IndistinguishableFrom(Color.white)) return null;

            Graphic coloured = GraphicDatabase.Get(gd.graphicClass ?? typeof(Graphic_Single),
                gd.texPath, ShaderDatabase.CutoutComplex, gd.drawSize, colorOne, colorTwo);

            if (coloured == null) return null;

            return gd.onGroundRandomRotateAngle > 0.01f
                ? new Graphic_RandomRotated(coloured, gd.onGroundRandomRotateAngle)
                : coloured;
        }

        public void SetZone(int index, Color c, bool markCustomized = true)
        {
            EnsureZoneDefaults();
            if (index < 0 || index >= ZoneColors.Count) return;
            ZoneColors[index] = c;
            if (markCustomized) _zonesCustomized = true;
            SetDirty();
        }

        public sealed class EditSnapshot
        {
            public List<Color> Zones = null!;
            public bool Customized;
            public bool ColorableActive;
            public Color ColorableColor;
            public Color? DesiredColor;
        }

        public EditSnapshot BeginEdit()
        {
            EnsureZoneDefaults();

            CompColorable? colorable = Colorable;
            if (colorable != null && colorable.Active && ZoneColors.Count > 0
                && !ZoneColors[0].IndistinguishableFrom(colorable.Color))
                ZoneColors[0] = colorable.Color;

            return new EditSnapshot
            {
                Zones = new List<Color>(ZoneColors),
                Customized = _zonesCustomized,
                ColorableActive = colorable != null && colorable.Active,
                ColorableColor = colorable?.Color ?? Color.white,
                DesiredColor = colorable?.DesiredColor
            };
        }

        public void PreviewZones(List<Color> colors)
        {
            ZoneColors = new List<Color>(colors);
            EnsureZoneDefaults();
            SyncColorableToPrimary();
            SetDirty();
        }

        public void CommitZones(List<Color> colors)
        {
            ZoneColors = new List<Color>(colors);
            _zonesCustomized = true;
            EnsureZoneDefaults();
            SyncColorableToPrimary();
            SetDirty();
        }

        public void RestoreEdit(EditSnapshot snapshot)
        {
            ZoneColors = new List<Color>(snapshot.Zones);
            _zonesCustomized = snapshot.Customized;
            EnsureZoneDefaults();

            CompColorable? colorable = Colorable;
            if (colorable != null)
            {
                if (snapshot.ColorableActive && colorable.Active
                    && !colorable.Color.IndistinguishableFrom(snapshot.ColorableColor))
                    colorable.SetColor(snapshot.ColorableColor);

                colorable.DesiredColor = snapshot.DesiredColor;
            }

            SetDirty();
        }

        private void SyncColorableToPrimary()
        {
            CompColorable? colorable = Colorable;
            if (colorable == null || !colorable.Active || ZoneColors.Count == 0) return;
            if (!colorable.Color.IndistinguishableFrom(ZoneColors[0]))
                colorable.SetColor(ZoneColors[0]);
        }

        public override Color? ForceColor()
        {
            if (ZoneColors.Count == 0) return null;
            return ZoneColors[0];
        }

        public void SyncPrimaryFromColorable(Color dyed)
        {
            if (props == null) return;

            EnsureZoneDefaults();
            if (ZoneColors.Count == 0 || ZoneColors[0].IndistinguishableFrom(dyed)) return;

            ZoneColors[0] = dyed;
            _zonesCustomized = true;
            SetDirty();
        }

        private void SetDirty()
        {
            _recacheItemGraphic = true;
            _cachedItemGraphic = null;

            if (parent is Apparel ap && ap.Wearer != null)
                ap.Wearer.Drawer?.renderer?.SetAllGraphicsDirty();
            else
                parent.Notify_ColorChanged();
        }

        public override void Notify_ColorChanged()
        {
            base.Notify_ColorChanged();
            _recacheItemGraphic = true;
            _cachedItemGraphic = null;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref ZoneColors, "UnitedFrontZoneColors", LookMode.Value);
            Scribe_Values.Look(ref _zonesCustomized, "UnitedFrontZonesCustomized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                EnsureZoneDefaults();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            ApplyDefaults();
        }
    }
}