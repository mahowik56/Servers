﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UTanksServer.ECS.ECSCore;

namespace UTanksServer.ECS.Components.Battle.Weapon
{
	[TypeUid(-210386542725124770)]
	public class FreezeDamageComponent : WeaponComponent
	{
		static public new long Id { get; set; }
		static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }

		public float damagePerSecondProperty { get; set; }
		public float energyChargeSpeedProperty { get; set; }
		public float energyRechargeSpeedProperty { get; set; }
		public float temperatureLimitProperty { get; set; }
		public float deltaTemperaturePerSecondProperty { get; set; }
		public float maxDamageDistanceProperty { get; set; }
		public float minDamageDistanceProperty { get; set; }
		public float minDamagePercentProperty { get; set; }

        public override TankConstructionComponent UpdateComponent(TankConstructionComponent tankComponent)
        {
            throw new NotImplementedException();
        }
    }
}
