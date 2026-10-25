using System.Collections.Generic;
using HarmonyLib;
using UnitedFront.UI;
using UnitedFront.Utils;
using UnityEngine;
using Verse;

namespace UnitedFront.Harmony
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class HarmonyPatch_PawnColorDebugGizmo
    {
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UFR/UI/Paint/UFR_CustomizeColor", false);

        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo g in __result)
                yield return g;

            if (!Prefs.DevMode || !DebugSettings.godMode) yield break;
            if (__instance == null || !__instance.RaceProps.Humanlike) yield break;
            if (!ColorMarkerUtil.Wears(__instance)) yield break;

            yield return new Command_Action
            {
                defaultLabel = "UFR_DevEditColors".Translate(),
                defaultDesc = "UFR_DevEditColorsDesc".Translate(),
                icon = Icon,
                action = () => Find.WindowStack.Add(new DialogEditColors(__instance))
            };
        }
    }
}