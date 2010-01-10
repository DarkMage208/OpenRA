﻿using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AttackHeliInfo : AttackBaseInfo
	{
		public override object Create(Actor self) { return new AttackHeli(self); }
	}

	class AttackHeli : AttackFrontal
	{
		public AttackHeli(Actor self) : base(self, 20) { }

		protected override void QueueAttack(Actor self, Order order)
		{
			target = order.TargetActor;
			self.QueueActivity(new HeliAttack(order.TargetActor));
			self.QueueActivity(new HeliReturn());
		}
	}
}
