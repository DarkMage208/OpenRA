﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class ProductionSurround : Production
	{
		public ProductionSurround(Actor self) : base(self) { }

		static int2? FindAdjacentTile(Actor a, UnitMovementType umt)
		{
			var tiles = Footprint.Tiles(a, a.traits.Get<Traits.Building>());
			var min = tiles.Aggregate(int2.Min) - new int2(1, 1);
			var max = tiles.Aggregate(int2.Max) + new int2(1, 1);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (Game.IsCellBuildable(new int2(i, j), umt))
						return new int2(i, j);

			return null;
		}

		public override int2? CreationLocation(Actor self, UnitInfo producee)
		{
			return FindAdjacentTile(self, producee.WaterBound ?
					UnitMovementType.Float : UnitMovementType.Wheel);	/* hackety hack */
		}

		public override int CreationFacing(Actor self, Actor newUnit)
		{
			return Util.GetFacing(newUnit.CenterLocation - self.CenterLocation, 128);
		}
	}
}
