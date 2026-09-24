using System.Collections.Generic;
using RimWorld;
using UnitedFront.Defs;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompColorMarker : ThingComp
    {
        public List<Color> ZoneColors = new List<Color>();
        private bool _zonesCustomized;

        public CompPropertiesColorMarker Props => (CompPropertiesColorMarker)props;
        public int ZoneCount => Props.zoneCount;

        private CompColorable Colorable => parent.TryGetComp<CompColorable>();

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
            ArmorColorExtension ext = parent.def.GetModExtension<ArmorColorExtension>();
            if (ext != null)
            {
                if (index == 0 && ext.setColorOne) return ext.colorOne;
                if (index == 1 && ext.setColorTwo) return ext.colorTwo;
            }

            if (Props.defaultZoneColors != null && index >= 0 && index < Props.defaultZoneColors.Count)
                return Props.defaultZoneColors[index];

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
            {
                int i = ZoneColors.Count;
                Color d = (Props.defaultZoneColors != null && i < Props.defaultZoneColors.Count)
                    ? Props.defaultZoneColors[i]
                    : Color.white;
                ZoneColors.Add(d);
            }
            if (ZoneColors.Count > ZoneCount)
                ZoneColors.RemoveRange(ZoneCount, ZoneColors.Count - ZoneCount);
        }

        public Color GetZone(int index) => (index >= 0 && index < ZoneColors.Count) ? ZoneColors[index] : Color.white;

        public Color DisplayZone(int index)
        {
            Color c = GetZone(index);
            if (parent is Apparel ap && ap.WornByCorpse)
                c = PawnRenderUtility.GetRottenColor(c);
            return c;
        }

        public List<Color> DisplayZones()
        {
            var list = new List<Color>(ZoneColors.Count);
            for (int i = 0; i < ZoneColors.Count; i++) list.Add(DisplayZone(i));
            return list;
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