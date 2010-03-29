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
using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	class ShroudInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Shroud(self, this); }
	}

	class Shroud
	{
		Map map;
		int[,] visibleCells;
		bool[,] exploredCells;

		public Shroud(Actor self, ShroudInfo info)
		{
			map = self.World.Map;
			visibleCells = new int[map.MapSize, map.MapSize];
			exploredCells = new bool[map.MapSize, map.MapSize];

			self.World.ActorAdded += AddActor;
			self.World.ActorRemoved += RemoveActor;
		}

		// cache of positions that were added, so no matter what crazy trait code does, it
		// can't make us invalid.
		class ActorVisibility { public int range; public int2[] vis; }
		Dictionary<Actor, ActorVisibility> vis = new Dictionary<Actor, ActorVisibility>();

		void AddActor(Actor a)
		{
			if (a.Owner != a.Owner.World.LocalPlayer) return;

			var v = new ActorVisibility
			{
				range = a.Info.Traits.Get<OwnedActorInfo>().Sight,
				vis = GetVisOrigins(a).ToArray()
			};

			foreach (var p in v.vis)
				foreach (var q in a.World.FindTilesInCircle(p, v.range))
				{
					++visibleCells[q.X, q.Y];
					exploredCells[q.X, q.Y] = true;
				}

			vis[a] = v;
		}

		static IEnumerable<int2> GetVisOrigins(Actor a)
		{
			if (a.Info.Traits.Contains<BuildingInfo>())
			{
				var bi = a.Info.Traits.Get<BuildingInfo>();
				return Footprint.Tiles(a.Info.Name, bi, a.Location);
			}
			else
			{
				var mobile = a.traits.GetOrDefault<Mobile>();
				if (mobile != null)
					return new[] { mobile.fromCell, mobile.toCell };
				else
					return new[] { (1f / Game.CellSize * a.CenterLocation).ToInt2() };
			}
		}

		void RemoveActor(Actor a)
		{
			ActorVisibility v;
			if (!vis.TryGetValue(a, out v)) return;

			foreach (var p in v.vis)
				foreach (var q in a.World.FindTilesInCircle(p, v.range))
					--visibleCells[q.X, q.Y];

			vis.Remove(a);
		}

		public void UpdateActor(Actor a)
		{
			if (a.Owner != a.Owner.World.LocalPlayer) return;
			RemoveActor(a); AddActor(a);
		}
	}
}
