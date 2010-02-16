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

namespace OpenRa.Traits
{
	class UnitInfo : OwnedActorInfo, ITraitInfo
	{
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;

		public object Create( Actor self ) { return new Unit( self ); }
	}

	public class Unit : INotifyDamage
	{
		[Sync]
		public int Facing;
		[Sync]
		public int Altitude;

		public Unit( Actor self ) { }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
					Sound.PlayToPlayer(self.Owner,
						self.Info.Traits.Get<OwnedActorInfo>().WaterBound ? "navylst1.aud" : "unitlst1.aud");
		}
	}
}
