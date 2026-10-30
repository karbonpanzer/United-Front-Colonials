using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using UnitedFront.Comps;

namespace UnitedFront.Harmony
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawColorTwo), MethodType.Getter)]
    public static class HarmonyPatch_ThingDrawColorTwo
    {
        public static void Postfix(Thing __instance, ref Color __result)
        {
            if (__instance is not Apparel apparel) return;

            Comp_ColorMarker comp = apparel.GetComp<Comp_ColorMarker>();
            if (comp == null || comp.ZoneCount < 2) return;

            __result = comp.DisplayZone(1);
        }
    }
    
}