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

using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class ParatroopersPowerInfo : SupportPowerInfo
	{
		public string[] DropItems = { };
		public string UnitType = "badr";
		public string FlareType = "flare";

		public override object Create(ActorInitializer init) { return new ParatroopersPower(init.self, this); }
	}

	class ParatroopersPower : SupportPower, IResolveOrder
	{
		public ParatroopersPower(Actor self, ParatroopersPowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = 
				new GenericSelectTarget( Owner.PlayerActor, "ParatroopersActivate", "ability" );
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "ParatroopersActivate")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();

				DoParadrop(Owner, order.TargetLocation, 
					self.Info.Traits.Get<ParatroopersPowerInfo>().DropItems);

				FinishActivate();
			}
		}

		void DoParadrop(Player owner, int2 p, string[] items)
		{
			var startPos = owner.World.ChooseRandomEdgeCell();
			owner.World.AddFrameEndTask(w =>
			{
				var flareType = (Info as ParatroopersPowerInfo).FlareType;
				var flare = flareType != null ? w.CreateActor(flareType, p, owner) : null;

				var a = w.CreateActor((Info as ParatroopersPowerInfo).UnitType, startPos, owner);
				a.traits.Get<Unit>().Facing = Util.GetFacing(p - startPos, 0);
				a.traits.Get<Unit>().Altitude = a.Info.Traits.Get<PlaneInfo>().CruiseAltitude;

				a.CancelActivity();
				a.QueueActivity(new FlyCircle(p));
				a.traits.Get<ParaDrop>().SetLZ(p, flare, false);

				var cargo = a.traits.Get<Cargo>();
				foreach (var i in items)
					cargo.Load(a, new Actor(owner.World, i.ToLowerInvariant(), 
						new int2(0,0), a.Owner));
			});
		}
	}
}
