using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.AssaultShield
{
    public class CompProperties_ShieldUFR : CompProperties_Shield
    {
        public Material BubbleMat = null!;

        public string bubbleTexPath = null!;

        public List<DamageDef> extraBlockedDamageDefs = new List<DamageDef>();

        public bool blocksExplosiveDamage = true;

        public float sharpEnergyLossMultiplier = 1f;

        public float bluntEnergyLossMultiplier = 1f;

        public float heatEnergyLossMultiplier = 1f;

        public float lowEnergyPulseThreshold = 0.25f;

        public bool randomizeRotation = true;

        public float breakFlashScale = 2f;

        public int breakDustPuffs = 0;

        public float breakEffecterScale = 1f;

        public CompProperties_ShieldUFR()
        {
            compClass = typeof(CompShieldUFR);

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                string path = bubbleTexPath.NullOrEmpty() ? "UFR/Other/ShieldBubble" : bubbleTexPath;
                BubbleMat = MaterialPool.MatFrom(path, ShaderDatabase.TransparentPostLight);
            });
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }

            if (lowEnergyPulseThreshold < 0f || lowEnergyPulseThreshold > 1f)
            {
                yield return "lowEnergyPulseThreshold should be from 0 to 1";
            }
        }
    }
}