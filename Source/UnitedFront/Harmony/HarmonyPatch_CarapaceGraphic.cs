using HarmonyLib;
using RimWorld;
using UnitedFront.Comps;
using Verse;

namespace UnitedFront.Harmony
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
    public static class HarmonyPatch_CarapaceGraphic
    {
        public static void Postfix(Thing __instance, ref Graphic __result)
        {
            if (__instance is not Apparel apparel || __result == null) return;

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null) return;

            __result = comp.ItemGraphic() ?? __result;
        }
    }
}