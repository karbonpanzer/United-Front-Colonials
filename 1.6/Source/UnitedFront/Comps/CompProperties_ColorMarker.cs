using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompPropertiesColorMarker : CompProperties
    {
        public int zoneCount = 2;

        public bool setColorOne = false;
        public Color colorOne = Color.white;

        public bool setColorTwo = true;
        public Color colorTwo = new Color(0.2f, 0.2f, 0.2f);

        public CompPropertiesColorMarker() => compClass = typeof(CompColorMarker);

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (zoneCount < 1)
                yield return parentDef.defName + " has CompColorMarker with zoneCount below 1.";

            if (parentDef.apparel != null && !parentDef.apparel.useWornGraphicMask)
                yield return parentDef.defName + " has CompColorMarker but apparel.useWornGraphicMask is false; zone colors will not render on the pawn.";
        }
    }
}