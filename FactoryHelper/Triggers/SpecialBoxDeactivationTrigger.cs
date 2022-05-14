﻿using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using FactoryHelper.Entities;
using Monocle;

namespace FactoryHelper.Triggers
{
    [CustomEntity("FactoryHelper/SpecialBoxDeactivationTrigger")]
    public class SpecialBoxDeactivationTrigger : Trigger
    {
        public SpecialBoxDeactivationTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Add(new HoldableCollider(OnCollide));
        }

        private void OnCollide(Holdable holdable)
        {
            Entity item = holdable.Entity;
            if (item != null && item is ThrowBox)
            {
                (item as ThrowBox).StopBeingSpecial();
            }
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            Entity heldItem = player.Holding?.Entity;
            if (heldItem != null || heldItem is ThrowBox)
            {
                (heldItem as ThrowBox).StopBeingSpecial();
            }
        }
    }
}
