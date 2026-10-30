using HarmonyLib;
using Verse;

namespace UnitedFront.Harmony
{
    [StaticConstructorOnStartup]
    public static class Harmony_Bootstrap
    {
        static Harmony_Bootstrap()
        {
            new HarmonyLib.Harmony("UnitedFront").PatchAll();
            Log.Message("[UnitedFront] Harmony patches applied.");
        }
    }
}
