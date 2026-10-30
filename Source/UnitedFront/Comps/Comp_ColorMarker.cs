using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using UnitedFront.UI;
using UnitedFront.Utils;

namespace UnitedFront.Comps
{
    [StaticConstructorOnStartup]
    public sealed class Comp_ColorMarker : ThingComp
    {
        private static readonly Texture2D DevEditColorsIcon = ContentFinder<Texture2D>.Get("UFR/UI/Paint/UFR_CustomizeColor", false);

        // Zone was used because this project originally started with a custom shader and using Zones for areas instead of the usual colorOne and colorTwo, I was an idiot starting with this.
        public List<Color> zoneColors = new List<Color>();
        private bool zonesCustomized;

        public CompProperties_ColorMarker Props => (CompProperties_ColorMarker)props;
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
            if (zonesCustomized) return;

            EnsureZoneDefaults();
            for (int i = 0; i < zoneColors.Count; i++)
                zoneColors[i] = DefaultZone(i);

            SetDirty();
        }

        private void EnsureZoneDefaults()
        {
            zoneColors ??= new List<Color>();
            while (zoneColors.Count < ZoneCount)
                zoneColors.Add(DefaultZone(zoneColors.Count));
            if (zoneColors.Count > ZoneCount)
                zoneColors.RemoveRange(ZoneCount, zoneColors.Count - ZoneCount);
        }

        public Color GetZone(int index) => (index >= 0 && index < zoneColors.Count) ? zoneColors[index] : Color.white;

        public Color DisplayZone(int index)
        {
            if (parent is Apparel ap && ap.WornByCorpse)
                return PawnRenderUtility.GetRottenColor(GetZone(index));
            return GetZone(index);
        }

        public sealed class EditSnapshot
        {
            public List<Color> zones = null!;
            public bool customized;
            public bool colorableActive;
            public Color colorableColor;
            public Color? desiredColor;
        }

        public EditSnapshot BeginEdit()
        {
            EnsureZoneDefaults();

            CompColorable? colorable = Colorable;
            if (colorable != null && colorable.Active && zoneColors.Count > 0
                && !zoneColors[0].IndistinguishableFrom(colorable.Color))
                zoneColors[0] = colorable.Color;

            return new EditSnapshot
            {
                zones = new List<Color>(zoneColors),
                customized = zonesCustomized,
                colorableActive = colorable != null && colorable.Active,
                colorableColor = colorable?.Color ?? Color.white,
                desiredColor = colorable?.DesiredColor
            };
        }

        public void PreviewZones(List<Color> colors)
        {
            zoneColors = new List<Color>(colors);
            EnsureZoneDefaults();
            SyncColorableToPrimary();
            SetDirty();
        }

        public void CommitZones(List<Color> colors)
        {
            zoneColors = new List<Color>(colors);
            zonesCustomized = true;
            EnsureZoneDefaults();
            SyncColorableToPrimary();
            SetDirty();
        }

        public void RestoreEdit(EditSnapshot snapshot)
        {
            zoneColors = new List<Color>(snapshot.zones);
            zonesCustomized = snapshot.customized;
            EnsureZoneDefaults();

            CompColorable? colorable = Colorable;
            if (colorable != null)
            {
                if (snapshot.colorableActive && colorable.Active
                    && !colorable.Color.IndistinguishableFrom(snapshot.colorableColor))
                    colorable.SetColor(snapshot.colorableColor);

                colorable.DesiredColor = snapshot.desiredColor;
            }

            SetDirty();
        }

        private void SyncColorableToPrimary()
        {
            CompColorable? colorable = Colorable;
            if (colorable == null || !colorable.Active || zoneColors.Count == 0) return;
            if (!colorable.Color.IndistinguishableFrom(zoneColors[0]))
                colorable.SetColor(zoneColors[0]);
        }

        public override Color? ForceColor()
        {
            if (zoneColors.Count == 0) return null;
            return zoneColors[0];
        }

        public void SyncPrimaryFromColorable(Color dyed)
        {
            if (props == null) return;

            EnsureZoneDefaults();
            if (zoneColors.Count == 0 || zoneColors[0].IndistinguishableFrom(dyed)) return;

            zoneColors[0] = dyed;
            zonesCustomized = true;
            SetDirty();
        }

        private void SetDirty()
        {
            parent.Notify_ColorChanged();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref zoneColors, "UnitedFrontZoneColors", LookMode.Value);
            Scribe_Values.Look(ref zonesCustomized, "UnitedFrontZonesCustomized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                EnsureZoneDefaults();
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
                yield return gizmo;

            if (!Prefs.DevMode || !DebugSettings.godMode) yield break;

            Pawn? wearer = (parent as Apparel)?.Wearer;
            if (wearer == null || !wearer.RaceProps.Humanlike || ColorMarkerUtil.FirstMarkerOn(wearer) != this) yield break;

            yield return new Command_Action
            {
                defaultLabel = "UFR_DevEditColors".Translate(),
                defaultDesc = "UFR_DevEditColorsDesc".Translate(),
                icon = DevEditColorsIcon,
                action = () => Find.WindowStack.Add(new Dialog_EditColors(wearer))
            };
        }
    }
}