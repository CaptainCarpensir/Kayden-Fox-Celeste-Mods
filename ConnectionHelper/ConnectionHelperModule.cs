﻿using Celeste;
using Celeste.Mod;
using ConnectionHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace ConnectionHelper
{
    class ConnectionHelperModule : EverestModule
    {

        public static ConnectionHelperModule Instance;

        public override Type SettingsType => typeof(ConnectionHelperSettings);

        public static ConnectionHelperSettings Settings => Instance._Settings as ConnectionHelperSettings;

        public ConnectionHelperModule()
        {
            Instance = this;
        }

        public override void Load()
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            LoadParticleTypes();
        }

        public override void Unload()
        {
        }

        private static void LoadParticleTypes()
        {
            CompanionSphere.P_Burst = new ParticleType
            {
                Source = GFX.Game[ "particles/shatter" ],
                Color = Color.Pink,
                Color2 = Color.HotPink,
                ColorMode = ParticleType.ColorModes.Fade,
                LifeMin = 0.3f,
                LifeMax = 0.4f,
                Size = 0.8f,
                SizeRange = 0.3f,
                ScaleOut = true,
                Direction = 0f,
                DirectionRange = 0f,
                SpeedMin = 100f,
                SpeedMax = 140f,
                SpeedMultiplier = 1E-05f,
                RotationMode = ParticleType.RotationModes.SameAsDirection
            };
            CompanionSphere.P_BurstNoDash = new ParticleType( CompanionSphere.P_Burst )
            {
                Color = Color.LightBlue,
                Color2 = Color.LightSkyBlue
            };
        }
    }
}
