using System.Collections.Generic;
using UnitedFront.UI;
using Verse;
using Verse.AI;

namespace UnitedFront.Jobs
{
    public class JobDriver_EditColorsAtStation : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_General.Do(delegate
            {
                Find.WindowStack.Add(new DialogEditColors(pawn));
            });
        }
    }
}
