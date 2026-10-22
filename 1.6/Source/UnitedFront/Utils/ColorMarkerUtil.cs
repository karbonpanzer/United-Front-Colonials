using System.Collections.Generic;
using RimWorld;
using UnitedFront.Comps;
using Verse;

namespace UnitedFront.Utils
{
    public static class ColorMarkerUtil
    {
        public static CompColorMarker? FirstMarkerOn(Pawn? pawn)
        {
            Pawn_ApparelTracker? tracker = pawn?.apparel;
            if (tracker == null) return null;

            List<Apparel> worn = tracker.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                CompColorMarker? comp = worn[i].TryGetComp<CompColorMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        public static bool Wears(Pawn? pawn) => FirstMarkerOn(pawn) != null;
    }
}