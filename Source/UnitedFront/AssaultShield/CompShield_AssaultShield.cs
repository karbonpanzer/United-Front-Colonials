using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UnitedFront.AssaultShield
{
    public class CompShield_AssaultShield : CompShield
    {
        private const float MaxDamagedJitterDist = 0.06f;
        private const int JitterDurationTicks = 8;

        private Vector3 incomingAngleVect;
        private int lastAbsorbDamageTick = -9999;

        private CompProperties_AssaultShield PropsUFR => (CompProperties_AssaultShield)props;

        private float EnergyMax => parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax);

        public float ShieldEnergyCur => energy;
        public float ShieldEnergyMax => EnergyMax;
        public int ResetTicksRemaining => ticksToReset;
        public int ResetTicksTotal => Props.startingTicksToReset;

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (ShieldState != ShieldState.Active || PawnOwner == null)
            {
                return;
            }

            if (dinfo.Def == DamageDefOf.EMP)
            {
                energy = 0f;
                BreakShield();
                return;
            }

            if (dinfo.Def.ignoreShields)
            {
                return;
            }

            bool blocked = dinfo.Def.isRanged
                        || (dinfo.Def.isExplosive && PropsUFR.blocksExplosiveDamage)
                        || (PropsUFR.extraBlockedDamageDefs != null
                            && PropsUFR.extraBlockedDamageDefs.Contains(dinfo.Def));

            if (!blocked)
            {
                return;
            }

            energy -= dinfo.Amount * Props.energyLossPerDamage * EnergyLossMultiplier(dinfo.Def);

            if (energy < 0f)
            {
                BreakShield();
            }
            else
            {
                AbsorbDamage(dinfo);
            }

            absorbed = true;
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (IsApparel)
            {
                foreach (Gizmo g in StatusGizmos()) yield return g;
            }
            foreach (Gizmo g in DevGizmos()) yield return g;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!IsApparel)
            {
                foreach (Gizmo g in StatusGizmos()) yield return g;
            }
        }

        private IEnumerable<Gizmo> StatusGizmos()
        {
            if (PawnOwner == null || Find.Selector.SingleSelectedThing != PawnOwner) yield break;

            bool visibleToPlayer = PawnOwner.Faction == Faction.OfPlayer
                                || (parent is Pawn p && p.RaceProps.IsMechanoid);
            if (!visibleToPlayer) yield break;

            yield return new GizmoStatus_AssaultShield { shield = this };
        }

        private IEnumerable<Gizmo> DevGizmos()
        {
            if (!DebugSettings.ShowDevGizmos) yield break;

            yield return new Command_Action
            {
                defaultLabel = "DEV: Break",
                action = BreakShield
            };

            if (ticksToReset > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Clear reset",
                    action = delegate { ticksToReset = 0; }
                };
            }
        }

        private float EnergyLossMultiplier(DamageDef def)
        {
            string category = def.armorCategory?.defName;
            if (category == "Sharp") return PropsUFR.sharpEnergyLossMultiplier;
            if (category == "Blunt") return PropsUFR.bluntEnergyLossMultiplier;
            if (category == "Heat") return PropsUFR.heatEnergyLossMultiplier;
            return 1f;
        }

        private void AbsorbDamage(DamageInfo dinfo)
        {
            if (PawnOwner.Spawned)
            {
                incomingAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
                Vector3 loc = PawnOwner.TrueCenter() + incomingAngleVect.RotatedBy(180f) * 0.5f;

                SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(
                    new TargetInfo(PawnOwner.Position, PawnOwner.Map));

                float scale = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
                FleckMaker.Static(loc, PawnOwner.Map, FleckDefOf.ExplosionFlash, scale);

                int puffs = (int)scale;
                for (int i = 0; i < puffs; i++)
                {
                    FleckMaker.ThrowDustPuff(loc, PawnOwner.Map, Rand.Range(0.8f, 1.2f));
                }
            }

            lastAbsorbDamageTick = Find.TickManager.TicksGame;
            KeepDisplaying();
        }

        private void BreakShield()
        {
            if (PawnOwner != null && PawnOwner.Spawned)
            {
                EffecterDefOf.Shield_Break.SpawnAttached(
                    parent, parent.MapHeld, PropsUFR.breakEffecterScale);

                if (PropsUFR.breakFlashScale > 0f)
                {
                    FleckMaker.Static(PawnOwner.TrueCenter(), PawnOwner.Map,
                        FleckDefOf.ExplosionFlash, PropsUFR.breakFlashScale);
                }

                for (int i = 0; i < PropsUFR.breakDustPuffs; i++)
                {
                    Vector3 offset = Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360))
                                     * Rand.Range(0.3f, 0.6f);
                    FleckMaker.ThrowDustPuff(PawnOwner.TrueCenter() + offset, PawnOwner.Map,
                        Rand.Range(0.8f, 1.2f));
                }
            }

            energy = 0f;
            ticksToReset = Props.startingTicksToReset;
        }

        private void DrawBubble()
        {
            if (ShieldState != ShieldState.Active || !ShouldDisplay)
            {
                return;
            }

            float size = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, energy);

            Vector3 drawPos = PawnOwner.Drawer.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            int sinceHit = Find.TickManager.TicksGame - lastAbsorbDamageTick;
            if (sinceHit < JitterDurationTicks)
            {
                float jitter = (float)(JitterDurationTicks - sinceHit)
                               / JitterDurationTicks * MaxDamagedJitterDist;
                drawPos += incomingAngleVect * jitter;
                size -= jitter;
            }

            float angle = PropsUFR.randomizeRotation ? Rand.Range(0, 360) : 0f;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(size, 1f, size));

            Material mat = PropsUFR.BubbleMat;
            if (mat == null)
            {
                return;
            }

            if (energy <= PropsUFR.lowEnergyPulseThreshold * EnergyMax)
            {
                mat = FadedMaterialPool.FadedVersionOf(
                    mat, 0.5f + 0.5f * Mathf.PingPong(Time.time, 1f));
            }

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void CompDrawWornExtras()
        {
            if (IsApparel)
            {
                DrawBubble();
            }
        }

        public override void PostDraw()
        {
            if (!IsApparel)
            {
                DrawBubble();
            }
        }
    }
}