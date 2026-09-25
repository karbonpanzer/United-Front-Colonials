using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.AssaultShield
{
    [StaticConstructorOnStartup]
    public class Gizmo_UFRShieldStatus : Gizmo
    {
        public CompShieldUFR shield;

        private static readonly Texture2D FullBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 0.9f));

        private static readonly Texture2D RechargeBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.9f, 0.5f, 0.15f));

        private static readonly Texture2D EmptyBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.03f, 0.03f));

        public Gizmo_UFRShieldStatus()
        {
            Order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect inner = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);

            Rect labelRect = inner;
            labelRect.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(labelRect, shield.parent.LabelCap);

            Rect barRect = inner;
            barRect.yMin = inner.y + inner.height / 2f;

            if (shield.ShieldState == ShieldState.Resetting)
            {
                int remaining = shield.ResetTicksRemaining;
                int total = Mathf.Max(1, shield.ResetTicksTotal);
                float progress = 1f - Mathf.Clamp01((float)remaining / total);

                Widgets.FillableBar(barRect, progress, RechargeBarTex, EmptyBarTex, doBorder: false);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                int secs = Mathf.CeilToInt(remaining / 60f);
                Widgets.Label(barRect, "UFR_ShieldRecharging".Translate(secs));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                float max = shield.ShieldEnergyMax;
                float fill = max > 0f ? shield.ShieldEnergyCur / max : 0f;

                Widgets.FillableBar(barRect, fill, FullBarTex, EmptyBarTex, doBorder: false);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect,
                    (shield.ShieldEnergyCur * 100f).ToString("F0") + " / " + (max * 100f).ToString("F0"));
                Text.Anchor = TextAnchor.UpperLeft;
            }

            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}