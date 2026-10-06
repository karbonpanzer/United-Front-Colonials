using System.Collections.Generic;
using UnityEngine;

namespace UnitedFront.Utils
{
    public static class UFRColors
    {
        public static readonly Color ArmorWhite       = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color RoleMedic        = new Color(0.12f, 0.56f, 0.84f);
        public static readonly Color RoleEngineer     = new Color(0.85f, 0.45f, 0.08f);
        public static readonly Color RolePolice       = new Color(0.09f, 0.16f, 0.34f);
        public static readonly Color RoleCommander    = new Color(0.80f, 0.65f, 0.18f);
        public static readonly Color RoleAssault      = new Color(0.70f, 0.12f, 0.15f);
        public static readonly Color RoleScout        = new Color(0.35f, 0.42f, 0.20f);
        public static readonly Color RoleVeteran      = new Color(0.16f, 0.16f, 0.17f);

        public static readonly Color RoleHeavy        = new Color(0.42f, 0.22f, 0.12f);
        public static readonly Color RoleMarksman     = new Color(0.38f, 0.45f, 0.52f);
        public static readonly Color RoleSignals      = new Color(0.92f, 0.80f, 0.16f);
        public static readonly Color RoleArmored      = new Color(0.10f, 0.46f, 0.45f);
        public static readonly Color RoleChaplain     = new Color(0.36f, 0.20f, 0.52f);
        public static readonly Color RoleQuartermaster = new Color(0.66f, 0.56f, 0.36f);
        public static readonly Color RoleHazmat       = new Color(0.56f, 0.74f, 0.18f);
        public static readonly Color RoleGuard        = new Color(0.36f, 0.08f, 0.20f);

        private static List<Color>? _palette;

        public static List<Color> Palette
        {
            get
            {
                if (_palette != null) return _palette;

                _palette = new List<Color>
                {
                    ArmorWhite, RoleMedic, RoleEngineer, RolePolice,
                    RoleCommander, RoleAssault, RoleScout, RoleVeteran,
                    RoleHeavy, RoleMarksman, RoleSignals, RoleArmored,
                    RoleChaplain, RoleQuartermaster, RoleHazmat, RoleGuard
                };
                return _palette;
            }
        }
    }
}