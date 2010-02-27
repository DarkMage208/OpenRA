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

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GiveCashCrateActionInfo : ITraitInfo
	{
		public int Amount = 2000;
		public int SelectionShares = 10;
		public object Create(Actor self) { return new GiveCashCrateAction(self); }
	}

	class GiveCashCrateAction : ICrateAction
	{
		Actor self;
		public GiveCashCrateAction(Actor self)
		{
			this.self = self;
		}

		public int SelectionShares
		{
			get { return self.Info.Traits.Get<GiveCashCrateActionInfo>().SelectionShares; }
		}

		public void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var amount = self.Info.Traits.Get<GiveCashCrateActionInfo>().Amount;
				collector.Owner.GiveCash(amount);
				w.Add(new CrateEffect(collector, "dollar"));
			});
		}
	}
}
