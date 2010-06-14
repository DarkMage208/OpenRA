#region Copyright & License Information
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

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	class TiberiumRefineryInfo : ITraitInfo
	{
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Red;
		public readonly int Capacity = 0;
		public readonly int ProcessTick = 25;
		public readonly int ProcessAmount = 50;
		public readonly string DeathWeapon = null;
		public object Create(Actor self) { return new TiberiumRefinery(self, this); }
	}

	class TiberiumRefinery : ITick, IAcceptOre, INotifyDamage, IPips
	{
		readonly Actor self;
		readonly TiberiumRefineryInfo Info;
		readonly PlayerResources Player;

		[Sync]
		int nextProcessTime = 0;
		[Sync]
		public int Tiberium = 0;
		
		public TiberiumRefinery(Actor self, TiberiumRefineryInfo info)
		{
			this.self = self;
			Info = info;
			Player = self.Owner.PlayerActor.traits.Get<PlayerResources>();

		}
		
		public void GiveOre(int amount)
		{
			Tiberium += amount;
			if (Tiberium > Info.Capacity)
				Tiberium = Info.Capacity;
		}
		
		public void Tick(Actor self)
		{
			if (--nextProcessTime <= 0)
			{
				// Convert resources to cash
				int amount = Math.Min(Tiberium, Info.ProcessAmount);
					amount = Math.Min(amount, Player.OreCapacity - Player.Ore);
				
				if (amount > 0)
				{
					Tiberium -=amount;
					Player.GiveOre(amount);
				}
				nextProcessTime = Info.ProcessTick;
			}
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead && Tiberium > 0)
			{
				if (Info.DeathWeapon != null)
				{
					Combat.DoExplosion(e.Attacker, Info.DeathWeapon,
									  self.CenterLocation.ToInt2(), 0);
				}
			}
		}
		
		public int2 DeliverOffset {	get { return new int2(0, 2); } }
		public void OnDock(Actor harv, DeliverResources dockOrder)
		{
			// Todo: need to be careful about cancellation and multiple harvs
			harv.QueueActivity(new Move(self.Location + new int2(1,1), self));
			harv.QueueActivity(new Turn(96));
			harv.QueueActivity( new CallFunc( () => 
				self.traits.Get<RenderBuilding>().PlayCustomAnimThen(self, "active", () => {
					harv.traits.Get<Harvester>().Deliver(harv, self);
					harv.QueueActivity(new Move(self.Location + DeliverOffset, self));
					harv.QueueActivity(new Harvest());
			})));
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Graphics.Util.MakeArray( Info.PipCount, 
				i => (Tiberium * 1.0f / Info.Capacity > i * 1.0f / Info.PipCount) 
					? Info.PipColor : PipType.Transparent );
		}
	}
}
