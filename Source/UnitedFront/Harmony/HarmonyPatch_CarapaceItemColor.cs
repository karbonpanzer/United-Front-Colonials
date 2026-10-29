using HarmonyLib;
using RimWorld;
using UnitedFront.Comps;
using UnityEngine;

namespace UnitedFront.Harmony
{

    [HarmonyPatch(typeof(Apparel), nameof(Apparel.DrawColor), MethodType.Getter)]
    public static class HarmonyPatch_Apparel_DrawColor
    {
        public static void Postfix(Apparel __instance, ref Color __result)
        {
            if (__instance.GetComp<CompColorMarker>() == null) return;

            __result = CompColorMarker.DisplayPrimary(__instance, __result);
        }
    }
}