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
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class OreRefineryDockActionInfo : TraitInfo<OreRefineryDockAction> {}

	class OreRefineryDockAction : IAcceptOreDockAction
	{
		public void OnDock(Actor self, Actor harv, DeliverResources dockOrder)
		{
			var unit = harv.traits.Get<Unit>();
			if (unit.Facing != 64)
				harv.QueueActivity (new Turn (64));
			
			harv.QueueActivity (new CallFunc (() =>
			{
				var renderUnit = harv.traits.Get<RenderUnit> ();
				if (renderUnit.anim.CurrentSequence.Name != "empty")
					renderUnit.PlayCustomAnimation (harv, "empty", () =>
					{
						harv.traits.Get<Harvester>().Deliver(harv, self);
						harv.QueueActivity (new Harvest ());
					});
			}));
		}
	}
}
