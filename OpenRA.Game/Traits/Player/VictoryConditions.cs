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

using System.Linq;

namespace OpenRA.Traits
{
	class VictoryConditionsInfo : ITraitInfo
	{
		public object Create(Actor self) { return new VictoryConditions( self ); }
	}

	interface IVictoryConditions { bool HasLost { get; } bool HasWon { get; } }

	class VictoryConditions : ITick, IVictoryConditions
	{
		public bool HasLost { get; private set; }
		public bool HasWon { get; private set; }

		public VictoryConditions(Actor self) { }

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<VictoryConditionsInfo>();
			var hasAnything = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<MustBeDestroyed>().Any();

			var hasLost = !hasAnything && self.Owner != self.World.NeutralPlayer;

			if (hasLost && !HasLost)
			{
				Game.Debug("{0} is defeated.".F(self.Owner.PlayerName));
				foreach(var a in self.World.Queries.OwnedBy[self.Owner])
					a.InflictDamage(a,a.Health,null);
				
				self.Owner.Shroud.Disabled = true;
			}
			HasLost = hasLost;
		}
	}

	/* tag trait for things that must be destroyed for a short game to end */

	class MustBeDestroyedInfo : TraitInfo<MustBeDestroyed> { }
	class MustBeDestroyed { }
}
