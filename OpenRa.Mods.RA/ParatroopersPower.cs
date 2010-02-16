﻿#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRa.Traits;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA
{
	class ParatroopersPowerInfo : SupportPowerInfo
	{
		public string[] DropItems = { };
		public override object Create(Actor self) { return new ParatroopersPower(self,this); }
	}

	class ParatroopersPower : SupportPower, IResolveOrder
	{
		public ParatroopersPower(Actor self, ParatroopersPowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		class SelectTarget : IOrderGenerator
		{
			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
					yield return new Order("ParatroopersActivate", world.LocalPlayer.PlayerActor, xy);
			}

			public void Tick(World world) {}
			public void Render(World world) {}

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				return "ability";
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ParatroopersActivate")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();

				DoParadrop(Owner, order.TargetLocation, 
					self.Info.Traits.Get<ParatroopersPowerInfo>().DropItems);

				FinishActivate();
			}
		}

		static void DoParadrop(Player owner, int2 p, string[] items)
		{
			var startPos = owner.World.ChooseRandomEdgeCell();
			owner.World.AddFrameEndTask(w =>
			{
				var a = w.CreateActor("BADR", startPos, owner);

				a.CancelActivity();
				a.QueueActivity(new FlyCircle(p));
				a.traits.Get<ParaDrop>().SetLZ(p);

				var cargo = a.traits.Get<Cargo>();
				foreach (var i in items)
					cargo.Load(a, new Actor(owner.World, i.ToLowerInvariant(), 
						new int2(0,0), a.Owner));
			});
		}
	}
}
