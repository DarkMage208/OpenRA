﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Building : ITick, INotifyBuildComplete
	{
		public Building(Actor self)
		{
		}

		bool first = true;
		public void Tick(Actor self)
		{
			if (first)
				self.CenterLocation = Game.CellSize * (float2)self.Location + 0.5f * self.SelectedSize;

			first = false;
		}

		public void BuildingComplete(Actor self)
		{
			UnitInfo.BuildingInfo bi = self.unitInfo as UnitInfo.BuildingInfo;
			if (bi == null) return;

			self.Owner.Power += bi.Power;
		}
	}
}
