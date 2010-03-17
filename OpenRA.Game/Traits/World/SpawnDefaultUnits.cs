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
using System;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	class SpawnDefaultUnitsInfo : StatelessTraitInfo<SpawnDefaultUnits> { }

	class SpawnDefaultUnits : IGameStarted
	{
		public void GameStarted(World world)
		{
			var taken = Game.LobbyInfo.Clients.Where(c => c.SpawnPoint != 0)
				.Select(c => world.Map.SpawnPoints.ElementAt(c.SpawnPoint - 1)).ToList();

			var available = world.Map.SpawnPoints.Except(taken).ToList();

			foreach (var client in Game.LobbyInfo.Clients)
			{
				SpawnUnitsForPlayer(world.players[client.Index],
					(client.SpawnPoint == 0)
					? ChooseSpawnPoint(world, available, taken)
					: world.Map.SpawnPoints.ElementAt(client.SpawnPoint - 1));
			}	
		}

		void SpawnUnitsForPlayer(Player p, int2 sp)
		{
			p.World.CreateActor("mcv", sp, p);
		}

		static int2 ChooseSpawnPoint(World world, List<int2> available, List<int2> taken)
		{
			if (available.Count == 0)
				throw new InvalidOperationException("No free spawnpoint.");

			var n = taken.Count == 0
				? world.SharedRandom.Next(available.Count)
				: available			// pick the most distant spawnpoint from everyone else
					.Select((k, i) => Pair.New(k, i))
					.OrderByDescending(a => taken.Sum(t => (t - a.First).LengthSquared))
					.Select(a => a.Second)
					.First();

			var sp = available[n];
			available.RemoveAt(n);
			taken.Add(sp);
			return sp;
		}
	}
}
