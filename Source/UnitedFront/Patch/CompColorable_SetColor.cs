using HarmonyLib;
using UnitedFront.Comps;
using UnityEngine;
using Verse;

namespace UnitedFront.Patches
{
    [HarmonyPatch(typeof(CompColorable), nameof(CompColorable.SetColor))]
    public static class Patch_CompColorable_SetColor
    {
        public static void Postfix(CompColorable __instance, Color __0)
        {
            CompColorMarker marker = __instance.parent.TryGetComp<CompColorMarker>();
            marker?.SyncPrimaryFromColorable(__0);
        }
    }
}