using HarmonyLib;
using RimWorld;
using UnitedFront.Comps;
using UnityEngine;
using Verse;

namespace UnitedFront.Harmony
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawColorTwo), MethodType.Getter)]
    public static class HarmonyPatch_Thing_DrawColorTwo
    {
        public static void Postfix(Thing __instance, ref Color __result)
        {
            if (__instance is not Apparel apparel) return;

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null || comp.ZoneCount < 2) return;

            __result = comp.DisplayZone(1);
        }
    }
    
}