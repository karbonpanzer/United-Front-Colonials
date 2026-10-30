using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnitedFront.Jobs;
using UnitedFront.Utils;

namespace UnitedFront.Comps
{
    public class Comp_EditColorArmamentsBench : ThingComp
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (selPawn == null || !selPawn.RaceProps.Humanlike) yield break;
            if (!ColorMarkerUtil.Wears(selPawn)) yield break;

            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly)
                || !selPawn.CanReserve(parent))
            {
                yield return new FloatMenuOption("UFR_EditArmorUnreachable".Translate(), null);
                yield break;
            }

            yield return new FloatMenuOption("UFR_EditArmor".Translate(), delegate
            {
                Job job = JobMaker.MakeJob(JobDefOf_PaintCarapaceArmor.UFR_EditColorsAtStation, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}