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

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ReturnOnIdleInfo : TraitInfo<ReturnOnIdle> { }

	// fly home or fly-off-map behavior for idle planes

	class ReturnOnIdle : INotifyIdle
	{
		public void Idle(Actor self)
		{
			var altitude = self.traits.Get<Unit>().Altitude;
			if (altitude == 0) return;	// we're on the ground, let's stay there.

			var airfield = ReturnToBase.ChooseAirfield(self);
			if (airfield != null)
			{
				self.QueueActivity(new ReturnToBase(self, airfield));
				self.QueueActivity(new Rearm());
			}
			else
			{
				Game.Debug("Plane has nowhere to land; flying away");
				self.QueueActivity(new FlyOffMap());
			}
		}
	}
}
