using RimWorld;
using Verse;

namespace UnitedFront.Jobs
{
    [DefOf]
    public static class JobDefOf_PaintCarapaceArmor
    {
        public static JobDef? UFR_EditColorsAtStation;

        static JobDefOf_PaintCarapaceArmor() => DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_PaintCarapaceArmor));
    }
}
