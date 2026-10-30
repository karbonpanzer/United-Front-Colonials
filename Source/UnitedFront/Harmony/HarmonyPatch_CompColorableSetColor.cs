using HarmonyLib;
using UnityEngine;
using Verse;
using UnitedFront.Comps;

namespace UnitedFront.Harmony
{
    [HarmonyPatch(typeof(CompColorable), nameof(CompColorable.SetColor))]
    public static class HarmonyPatch_CompColorableSetColor
    {
        public static void Postfix(CompColorable __instance, Color value)
        {
            Comp_ColorMarker marker = __instance.parent.TryGetComp<Comp_ColorMarker>();
            marker?.SyncPrimaryFromColorable(value);
        }
    }
}