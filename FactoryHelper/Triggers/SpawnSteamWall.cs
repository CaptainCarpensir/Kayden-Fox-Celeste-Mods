﻿using Celeste;
using Celeste.Mod.Entities;
using FactoryHelper.Entities;
using Microsoft.Xna.Framework;

namespace FactoryHelper.Triggers
{
    [CustomEntity("FactoryHelper/SpawnSteamWallTrigger")]
    class SpawnSteamWall : Trigger
    {
        private bool _spawned = false;

        public SpawnSteamWall(EntityData data, Vector2 offset) : base(data, offset)
        {
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            if (!_spawned)
            {
                Level level = Scene as Level;
                level.Add(new SteamWall(level.Camera.Left - level.Bounds.Left));
                _spawned = true;
            }
        }
    }
}
