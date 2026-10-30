using System.Reflection;
using HarmonyLib;
using RimWorld;
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
            if (forStatue || !__result || rec.graphic == null) return;
            if (rec.graphic.Shader != ShaderDatabase.CutoutComplex) return;

            Color colorTwo = apparel.DrawColorTwo;
            if (colorTwo.IndistinguishableFrom(rec.graphic.colorTwo)) return;

            rec = new ApparelGraphicRecord(rec.graphic.GetColoredVersion(rec.graphic.Shader, rec.graphic.color, colorTwo), apparel);
        }
    }
}