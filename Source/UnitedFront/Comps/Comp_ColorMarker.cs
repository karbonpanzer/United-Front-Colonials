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
                if (parent.Stuff != null) return parent.Stuff.stuffProps.color;
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
            Color c = Floor(GetZone(index));
            if (parent is Apparel ap && ap.WornByCorpse)
                c = PawnRenderUtility.GetRottenColor(c);
            return c;
        }

        public Graphic? ItemGraphic(Graphic fallback)
        {
            if (!_recacheItemGraphic) return _cachedItemGraphic;

            _recacheItemGraphic = false;
            _cachedItemGraphic = BuildItemGraphic() ?? fallback;
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

            Color colorOne = Floor(apparel.DrawColor);
            Color colorTwo = DisplayZone(1);
            if (colorTwo.IndistinguishableFrom(Color.white)) return null;

            Graphic coloured = GraphicDatabase.Get<Graphic_Single>(
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

        public void PreviewZones(List<Color> colors)
        {
            ZoneColors = new List<Color>(colors);
            EnsureZoneDefaults();
            SetDirty();
        }

        public void CommitZones(List<Color> colors)
        {
            ZoneColors = new List<Color>(colors);
            _zonesCustomized = true;
            EnsureZoneDefaults();
            Colorable?.Disable();
            SetDirty();
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