using RimWorld;
using UnitedFront.Comps;
using Verse;

namespace UnitedFront.Utils
{
    public static class ColorMarkerUtil
    {
        public static CompColorMarker MarkerOn(Pawn pawn)
        {
            Pawn_ApparelTracker tracker = pawn?.apparel;
            if (tracker == null) return null;

            foreach (Apparel t in tracker.WornApparel)
            {
                if (!t.def.HasComp<CompColorMarker>()) continue;
                CompColorMarker comp = t.TryGetComp<CompColorMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        public static bool Wears(Pawn pawn) => MarkerOn(pawn) != null;
    }
}
