using HarmonyLib;
using RimWorld;
using UnitedFront.Comps;
using UnitedFront.Utils;
using UnityEngine;
using Verse;

namespace UnitedFront.Patch
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
    public static class Thing_CarapaceItemGraphic_Patch
    {
        public static void Postfix(Thing __instance, ref Graphic __result)
        {
            if (__instance is not Apparel apparel) return;
            if (apparel.StyleDef?.Graphic != null) return;

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null || comp.ZoneColors.NullOrEmpty()) return;

            GraphicData gd = apparel.def.graphicData;
            if (gd == null || gd.texPath.NullOrEmpty()) return;
            if (gd.shaderType?.Shader != ShaderDatabase.CutoutComplex) return;

            __result = MultiColorGraphicUtil.Get(gd.texPath, null, ShaderDatabase.CutoutComplex,
                gd.drawSize, comp.DisplayZones(), typeof(Graphic_Single));
        }
    }
}