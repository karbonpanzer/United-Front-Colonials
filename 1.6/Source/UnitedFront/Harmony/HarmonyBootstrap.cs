using HarmonyLib;
using Verse;

namespace UnitedFront.Harmony
{
    [StaticConstructorOnStartup]
    public static class HarmonyBootstrap
    {
        static HarmonyBootstrap()
        {
            new HarmonyLib.Harmony("UnitedFront").PatchAll();
            Log.Message("[UnitedFront] Harmony patches applied.");
        }
    }
}
