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

using System.Linq;

namespace OpenRa.Traits.Activities
{
	public class DeliverOre : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool isDocking;
		Actor refinery;

		public DeliverOre() { }

		public DeliverOre( Actor refinery )
		{
			this.refinery = refinery;
		}

		public IActivity Tick( Actor self )
		{
			var mobile = self.traits.Get<Mobile>();

			if( NextActivity != null )
				return NextActivity;

			if( refinery != null && refinery.IsDead )
				refinery = null;

			if( refinery == null || self.Location != refinery.Location + refinery.traits.Get<IAcceptOre>().DeliverOffset )
			{
				var search = new PathSearch
				{
					heuristic = PathSearch.DefaultEstimator( self.Location ),
					umt = mobile.GetMovementType(),
					checkForBlocked = false,
				};
				var refineries = self.World.Queries.OwnedBy[self.Owner]
					.Where(x => x.traits.Contains<IAcceptOre>())
					.ToList();
				if( refinery != null )
					search.AddInitialCell(self.World, refinery.Location + refinery.traits.Get<IAcceptOre>().DeliverOffset);
				else
					foreach( var r in refineries )
						search.AddInitialCell(self.World, r.Location + r.traits.Get<IAcceptOre>().DeliverOffset);

				var path = self.World.PathFinder.FindPath( search );
				path.Reverse();
				if( path.Count != 0 )
				{
					refinery = refineries.FirstOrDefault(x => x.Location + x.traits.Get<IAcceptOre>().DeliverOffset == path[0]);
					return new Move( () => path ) { NextActivity = this };
				}
				else
					// no refineries reachable?
					return this;
			}
			else if (!isDocking)
			{
				isDocking = true;
				refinery.traits.Get<IAcceptOre>().OnDock(self, this);
			}
			
			return this;
		}

		public void Cancel(Actor self)
		{
			// TODO: allow canceling of deliver orders?
		}
	}
}
