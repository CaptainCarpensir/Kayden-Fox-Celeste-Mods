﻿using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Celeste;
using FactoryHelper.Components;
using Microsoft.Xna.Framework.Graphics;

namespace FactoryHelper.Entities
{
    class DashNegator : Entity
    {
        public FactoryActivatorComponent Activator;

        private Sprite[] _turretSprites;
        private Solid[] _turretSolids;

        public static ParticleType P_NegatorField = new ParticleType
        {
            Size = 1f,
            Color = Calc.HexToColor("800000"),
            Color2 = Calc.HexToColor("c40000"),
            ColorMode = ParticleType.ColorModes.Blink,
            FadeMode = ParticleType.FadeModes.Late,
            SpeedMin = 2f,
            SpeedMax = 8f,
            SpinMin = 0.3f,
            SpinMax = 0.8f,
            DirectionRange = (float)Math.PI * 2f,
            Direction = 0f,
            LifeMin = 0.8f,
            LifeMax = 1.2f
        };
        private PlayerCollider _pc;

        public DashNegator(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height, data.Attr("activationId"), data.Bool("startActive"))
        {
        }

        public DashNegator(Vector2 position, int width, int height, string activationId, bool startActive) : base(position)
        {
            Add(Activator = new FactoryActivatorComponent());
            Activator.ActivationId = activationId == string.Empty ? null : activationId;
            Activator.StartOn = startActive;
            Activator.OnStartOff = OnStartOff;
            Activator.OnStartOn = OnStartOn;
            Activator.OnTurnOff = OnTurnOff;
            Activator.OnTurnOn = OnTurnOn;

            Collider = new Hitbox(width - 4, height, 2, 0);

            width = 16 * (width / 16);

            Depth = -9000;

            Add(new StaticMover
            {
                OnShake = OnShake,
                SolidChecker = IsRiding,
                OnDestroy = RemoveSelf
            });
            Add(_pc = new PlayerCollider(OnPlayer));

            int length = width / 16;
            _turretSprites = new Sprite[length];
            _turretSolids = new Solid[length];

            for (int i = 0; i < length; i++)
            {
                Add(_turretSprites[i] = new Sprite(GFX.Game, "danger/FactoryHelper/dashNegator/"));
                _turretSprites[i].Add("inactive", "turret", 1f, 0);
                _turretSprites[i].Add("rest", "turret", 0.2f, "active", 0);
                _turretSprites[i].Add("active", "turret", 0.05f, "rest");
                _turretSprites[i].Position = new Vector2(-2 + 16 * i, -2);

                _turretSolids[i] = new Solid(position + new Vector2(2 + 16 * i, 0), 12, 8, false);
            }

        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            foreach (var solid in _turretSolids)
            {
                scene.Add(solid);
            }
            Activator.Added(scene);
        }

        public override void Removed(Scene scene)
        {
            foreach (var solid in _turretSolids)
            {
                scene.Remove(solid);
            }
            base.Removed(scene);
        }

        public override void Update()
        {
            base.Update();

            if (Visible && Activator.IsOn && Scene.OnInterval(0.05f))
            {
                SceneAs<Level>().ParticlesFG.Emit(P_NegatorField, 1, Center, new Vector2(Width, Height));
            }
        }

        public override void Render()
        {
            base.Render();
            Color color = Color.DarkRed * 0.3f;
            if (Visible && Activator.IsOn)
            {
                Draw.Rect(Collider, color);
            }
        }

        private void OnTurnOn()
        {
            foreach(var sprite in _turretSprites)
            {
                sprite.Play("active", true);
            }
        }

        private void OnTurnOff()
        {
            foreach (var sprite in _turretSprites)
            {
                sprite.Play("inactive");
            }
            FizzleOff();
        }

        private void OnStartOn()
        {
            foreach (var sprite in _turretSprites)
            {
                sprite.Play("active", true);
            }
        }

        private void OnStartOff()
        {
            foreach (var sprite in _turretSprites)
            {
                sprite.Play("inactive");
            }
        }

        private void FizzleOff()
        {
            for (int i = 0; i < Width; i += 8)
            {
                for (int j = 0; j < Height; j += 8)
                {
                    SceneAs<Level>().ParticlesFG.Emit(P_NegatorField, 1, Position + new Vector2(4 + i * 8, 4 + j * 8), new Vector2(4, 4));
                }
            }
        }

        private void OnPlayer(Player player)
        {
            if (Activator.IsOn && player.StartedDashing)
            {
                ShootClosestLaserToPlayer(player);
                player.Die(Vector2.UnitY);
            }
        }

        private void ShootClosestLaserToPlayer(Player player)
        {
            Audio.Play("event:/char/badeline/boss_laser_fire", player.Position);
            Vector2 beamPosition = new Vector2(Position.X, Position.Y + 8);
            beamPosition.X += Math.Min((int)(player.Center.X - Left) / 16 * 16, Width - 12) + 8;
            Scene.Add(new DashNegatorBeam(beamPosition));
        }

        private void OnShake(Vector2 pos)
        {
            foreach (Component component in Components)
            {
                if (component is Image)
                {
                    (component as Image).Position = pos;
                }
            }
        }

        private bool IsRiding(Solid solid)
        {
            return CollideCheck(solid);
        }
    }
}