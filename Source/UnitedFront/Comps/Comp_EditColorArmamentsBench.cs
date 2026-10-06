using System.Collections.Generic;
using RimWorld;
using UnitedFront.Jobs;
using UnitedFront.Utils;
using Verse;
using Verse.AI;

namespace UnitedFront.Comps
{
    public class CompEditColorsStation : ThingComp
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