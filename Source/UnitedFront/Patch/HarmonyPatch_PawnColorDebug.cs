using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnitedFront.Comps;
using UnitedFront.Utils;
using UnityEngine;
using Verse;

namespace UnitedFront.Patch
{
    [HarmonyPatch]
    public static class ApparelGraphic_CarapaceColor_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel",
                new[] { typeof(Apparel), typeof(BodyTypeDef), typeof(bool), typeof(ApparelGraphicRecord).MakeByRefType() });
        }

        public static void Postfix(Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec, bool __result)
        {
            if (forStatue || !__result || apparel == null) return;

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null || comp.ZoneColors.NullOrEmpty()) return;

            string basePath = apparel.WornGraphicPath;
            if (basePath.NullOrEmpty()) return;

            if (!UsesMask(apparel)) return;

            if (bodyType == null) bodyType = BodyTypeDefOf.Male;

            ApparelLayerDef last = apparel.def.apparel.LastLayer;
            bool perBodyType = last != ApparelLayerDefOf.Overhead
                               && last != ApparelLayerDefOf.EyeCover
                               && !apparel.RenderAsPack()
                               && basePath != BaseContent.PlaceholderImagePath
                               && basePath != BaseContent.PlaceholderGearImagePath;

            string path = perBodyType ? basePath + "_" + bodyType.defName : basePath;

            Graphic graphic = MultiColorGraphicUtil.Get(path, null, ShaderDatabase.CutoutComplex,
                apparel.def.graphicData.drawSize, comp.DisplayZones());

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