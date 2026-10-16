using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnitedFront.Comps;
using UnityEngine;
using Verse;

namespace UnitedFront.Harmony
{
    [HarmonyPatch]
    public static class HarmonyPatch_CarapaceColor
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel",
                new[] { typeof(Apparel), typeof(BodyTypeDef), typeof(bool), typeof(ApparelGraphicRecord).MakeByRefType() });
        }

        public static void Postfix(Apparel apparel, bool forStatue, ref ApparelGraphicRecord rec, bool __result)
        {
            if (forStatue || !__result || apparel == null || rec.graphic == null) return;

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null || comp.ZoneColors.Count < 2) return;

            if (!UsesMask(apparel)) return;

            Color colorOne = apparel.DrawColor;
            Color colorTwo = comp.DisplayZone(1);

            if (colorTwo.IndistinguishableFrom(Color.white)) return;

            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(
                rec.graphic.path, ShaderDatabase.CutoutComplex, rec.graphic.drawSize, colorOne, colorTwo);

            if (graphic != null)
                rec = new ApparelGraphicRecord(graphic, apparel);
        }

        private static bool UsesMask(Apparel apparel)
        {
            if (apparel.StyleDef?.graphicData.shaderType != null)
                return apparel.StyleDef.graphicData.shaderType.Shader == ShaderDatabase.CutoutComplex;

            if (apparel.StyleDef == null) return apparel.def.apparel.useWornGraphicMask;
            return apparel.StyleDef.UseWornGraphicMask;
        }
    }
}
