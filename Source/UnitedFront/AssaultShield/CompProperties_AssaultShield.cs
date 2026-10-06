using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.AssaultShield
{
    public class CompProperties_AssaultShield : CompProperties_Shield
    {
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

        private Material bubbleMat;

        public Material BubbleMat => bubbleMat;

        public CompProperties_AssaultShield()
        {
            compClass = typeof(CompShield_AssaultShield);

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                string path = bubbleTexPath.NullOrEmpty() ? "UFR/Other/ShieldBubble" : bubbleTexPath;
                bubbleMat = MaterialPool.MatFrom(path, ShaderDatabase.TransparentPostLight);
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