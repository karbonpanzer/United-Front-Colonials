using System.Collections.Generic;
using RimWorld;
using Verse;
using UnitedFront.Comps;

namespace UnitedFront.Utils
{
    public static class ColorMarkerUtil
    {
        public static Comp_ColorMarker? FirstMarkerOn(Pawn? pawn)
        {
            Pawn_ApparelTracker? tracker = pawn?.apparel;
            if (tracker == null) return null;

            List<Apparel> worn = tracker.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                Comp_ColorMarker? comp = worn[i].TryGetComp<Comp_ColorMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        public static bool Wears(Pawn? pawn) => FirstMarkerOn(pawn) != null;
    }
}