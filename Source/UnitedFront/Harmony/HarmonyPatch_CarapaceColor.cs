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
            if (rec.graphic.Shader != ShaderDatabase.CutoutComplex) return;

            CompColorMarker comp = apparel.GetComp<CompColorMarker>();
            if (comp == null || comp.ZoneColors.Count < 2) return;

            Color colorTwo = comp.DisplayZone(1);
            if (colorTwo.IndistinguishableFrom(Color.white)) return;

            Color colorOne = CompColorMarker.DisplayPrimary(apparel, rec.graphic.color);

            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(
                rec.graphic.path, rec.graphic.Shader, rec.graphic.drawSize, colorOne, colorTwo);

            if (graphic != null)
                rec = new ApparelGraphicRecord(graphic, apparel);
        }
    }
}