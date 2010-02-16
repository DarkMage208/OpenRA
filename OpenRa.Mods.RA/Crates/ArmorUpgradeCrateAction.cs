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

using OpenRa.Mods.RA.Effects;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class ArmorUpgradeCrateActionInfo : ITraitInfo
	{
		public float Multiplier = 2.0f;
		public int SelectionShares = 10;
		public object Create(Actor self) { return new ArmorUpgradeCrateAction(self); }
	}

	class ArmorUpgradeCrateAction : ICrateAction
	{
		Actor self;
		public ArmorUpgradeCrateAction(Actor self)
		{
			this.self = self;
		}

		public int SelectionShares
		{
			get { return self.Info.Traits.Get<ArmorUpgradeCrateActionInfo>().SelectionShares; }
		}

		public void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, "armorup1.aud");
			collector.World.AddFrameEndTask(w =>
			{
				var multiplier = self.Info.Traits.Get<ArmorUpgradeCrateActionInfo>().Multiplier;
				collector.traits.Add(new ArmorUpgrade(multiplier));
				w.Add(new CrateEffect(collector, "armor"));
			});
		}
	}

	class ArmorUpgrade : IDamageModifier
	{
		float multiplier;
		public ArmorUpgrade(float multiplier) { this.multiplier = 1/multiplier; }
		public float GetDamageModifier() { return multiplier; }
	}
}
