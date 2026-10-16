using HarmonyLib;
using RimWorld;
using UnitedFront.Comps;
using UnityEngine;
using Verse;

namespace UnitedFront.Harmony
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
    public static class HarmonyPatch_CarapaceGraphic
    {
        public static void Postfix(Thing __instance, ref Graphic __result)
        {
            if (__instance is not Apparel apparel || __result == null) return;
            if (apparel.StyleDef?.Graphic != null) return;

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null || comp.ZoneColors.Count < 2) return;

            if (apparel.def.graphicData?.shaderType?.Shader != ShaderDatabase.CutoutComplex) return;

            Color colorOne = CompColorMarker.Floor(apparel.DrawColor);
            Color colorTwo = comp.DisplayZone(1);

            if (colorTwo.IndistinguishableFrom(Color.white)) return;

            __result = GraphicDatabase.Get<Graphic_Single>(
                __result.path, ShaderDatabase.CutoutComplex, __result.drawSize, colorOne, colorTwo);
        }
    }
}