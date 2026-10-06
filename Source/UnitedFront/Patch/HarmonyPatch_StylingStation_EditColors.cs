using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnitedFront.Jobs;
using UnitedFront.Utils;
using Verse;
using Verse.AI;

namespace UnitedFront.Patch
{
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetFloatMenuOptions))]
    public static class HarmonyPatch_StylingStation_EditColors
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result,
                                                           ThingWithComps __instance, Pawn selPawn)
        {
            foreach (FloatMenuOption option in __result)
                yield return option;

            if (__instance is not Building_StylingStation)
                yield break;

            if (selPawn == null || !selPawn.RaceProps.Humanlike || !ColorMarkerUtil.Wears(selPawn))
                yield break;

            if (!selPawn.CanReach(__instance, PathEndMode.InteractionCell, Danger.Deadly)
                || !selPawn.CanReserve(__instance))
            {
                yield return new FloatMenuOption("UFR_EditArmorUnreachable".Translate(), null);
                yield break;
            }

            yield return new FloatMenuOption("UFR_EditArmor".Translate(), delegate
            {
                Job job = JobMaker.MakeJob(JobDefOf_PaintCarapaceArmor.UFR_EditColorsAtStation, __instance);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}
