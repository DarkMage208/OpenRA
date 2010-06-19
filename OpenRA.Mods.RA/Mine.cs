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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class MineInfo : ITraitInfo
	{
		public readonly UnitMovementType[] TriggeredBy = { };
		public readonly string Weapon = "ATMine";
		public readonly bool AvoidFriendly = true;

		public object Create(ActorInitializer init) { return new Mine(init); }
	}

	class Mine : ICrushable, IOccupySpace
	{
		readonly Actor self;
		[Sync]
		readonly int2 location;

		public Mine(ActorInitializer init)
		{
			this.self = init.self;
			this.location = init.location;
			self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, this);
		}

		public void OnCrush(Actor crusher)
		{
			if (crusher.traits.Contains<MineImmune>() && crusher.Owner == self.Owner)
				return;

			var info = self.Info.Traits.Get<MineInfo>();
			Combat.DoExplosion(self, info.Weapon, crusher.CenterLocation.ToInt2(), 0);
			self.QueueActivity(new RemoveSelf());
		}

		public bool IsPathableCrush(UnitMovementType umt, Player player)
		{
			return !self.Info.Traits.Get<MineInfo>().AvoidFriendly || (player != self.Owner);
		}

		public bool IsCrushableBy(UnitMovementType umt, Player player)
		{
			return self.Info.Traits.Get<MineInfo>().TriggeredBy.Contains(umt);
		}

		public int2 TopLeft { get { return location; } }

		public IEnumerable<int2> OccupiedCells() { yield return TopLeft; }
	}

	/* tag trait for stuff that shouldnt trigger mines */
	class MineImmuneInfo : TraitInfo<MineImmune> { }
	class MineImmune { }
}
