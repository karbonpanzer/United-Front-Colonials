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

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null || comp.ZoneColors.NullOrEmpty()) return;

            Shader shader = ShaderDatabase.CutoutComplex;

            GraphicData gd = apparel.def.graphicData;
            if (gd == null || gd.texPath.NullOrEmpty()) return;

            __result = MultiColorGraphicUtil.Get(gd.texPath, null, shader, gd.drawSize, comp.DisplayZones(), typeof(Graphic_Single));
        }
    }
}