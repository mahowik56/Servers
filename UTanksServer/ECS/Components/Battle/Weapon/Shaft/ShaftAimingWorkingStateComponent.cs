﻿using System.Numerics;
using UTanksServer.Core;
//using UTanksServer.Core.Battles.BattleWeapons;
using UTanksServer.Core.Protocol;
using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components.Battle.Weapon
{
    [TypeUid(4186891190183470299L)]
	public class ShaftAimingWorkingStateComponent : ECSComponent
    {
        //public void OnAttached(Player player, Entity weapon) =>
        //    ((Shaft) player.BattlePlayer.MatchPlayer.Weapon).StartAiming();

        //public void OnRemove(Player player, Entity weapon) =>
        //    ((Shaft) player.BattlePlayer.MatchPlayer.Weapon).StopAiming();
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }

        public float InitialEnergy { get; set; }
        public float ExhaustedEnergy { get; set; }
        public float VerticalAngle { get; set; }
        public Vector3 WorkingDirection { get; set; }
        public float VerticalSpeed { get; set; }
        public int VerticalElevationDir { get; set; }
        public bool IsActive { get; set; }
        public int ClientTime { get; set; }
	}
}
