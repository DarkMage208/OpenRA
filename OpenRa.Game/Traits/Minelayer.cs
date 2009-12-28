﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Minelayer : IOrder
	{
		public Minelayer(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return null;

			if (mi.Button == MouseButton.Right && underCursor == self)
				return new Order("Deploy", self, null, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
				if (limitedAmmo != null)
					limitedAmmo.Attacking(self);

				// todo: delay a bit?

				Game.world.AddFrameEndTask(
					w => w.Add(new Actor(Rules.UnitInfo[self.Info.Primary], self.Location, self.Owner)));
			}
		}
	}
}
