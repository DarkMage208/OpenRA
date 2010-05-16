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

namespace OpenRA.Mods.RA
{
	class AirstrikePowerInfo : SupportPowerInfo
	{
		public readonly string UnitType = "badr.bomber";
		public readonly string FlareType = null;
		public override object Create(Actor self) { return new AirstrikePower(self, this); }
	}

	class AirstrikePower : SupportPower, IResolveOrder
	{
		public AirstrikePower(Actor self, AirstrikePowerInfo info) : base(self, info) { }

		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new GenericSelectTarget(Owner.PlayerActor, Info.OrderName, "ability");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == Info.OrderName)
			{
				var startPos = Owner.World.ChooseRandomEdgeCell();

				Owner.World.AddFrameEndTask(w =>
					{
						var flareType = (Info as AirstrikePowerInfo).FlareType;
						var flare = flareType != null ? w.CreateActor(flareType, order.TargetLocation, Owner) : null;

						var a = w.CreateActor((Info as AirstrikePowerInfo).UnitType, startPos, Owner);
						a.traits.Get<Unit>().Facing = Util.GetFacing(order.TargetLocation - startPos, 0);
						a.traits.Get<Unit>().Altitude = a.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
						a.traits.Get<CarpetBomb>().SetTarget(order.TargetLocation);

						a.CancelActivity();
						a.QueueActivity(new Fly(order.TargetLocation));

						if (flare != null)
							a.QueueActivity(new CallFunc(() => Owner.World.AddFrameEndTask(_w => _w.Remove(flare))));

						a.QueueActivity(new FlyOffMap { Interruptible = false });
						a.QueueActivity(new RemoveSelf());
					});

				if (Owner == Owner.World.LocalPlayer)
					Game.controller.CancelInputMode();

				FinishActivate();
			}
		}
	}
}
