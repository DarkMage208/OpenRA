﻿using System;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class AttackTurreted : AttackBase
	{
		public AttackTurreted( Actor self ) : base(self) { self.traits.Get<Turreted>(); }

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if( !CanAttack( self ) ) return;

			var turreted = self.traits.Get<Turreted>();
			turreted.desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turreted.turretFacing );
			if( turreted.desiredFacing != turreted.turretFacing )
				return;

			DoAttack( self );
		}

		protected override void QueueAttack( Actor self, Order order )
		{
			var bi = self.Info as BuildingInfo;
			if (bi != null && bi.Powered && self.Owner.GetPowerState() != PowerState.Normal)
			{
				if (self.Owner == Game.LocalPlayer) Sound.Play("nopowr1.aud");
				return;
			}

			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = order.Subject.Info.Primary ?? order.Subject.Info.Secondary;

			self.QueueActivity( new Traits.Activities.Follow( order.TargetActor,
				Math.Max( 0, (int)Rules.WeaponInfo[ weapon ].Range - RangeTolerance ) ) );
			self.traits.Get<AttackTurreted>().target = order.TargetActor;
		}
	}
}
