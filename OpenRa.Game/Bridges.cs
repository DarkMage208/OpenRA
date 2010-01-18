﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;

namespace OpenRa
{
	static class Bridges
	{
		public static void MakeBridges(World w)
		{
			var mini = w.Map.XOffset; var maxi = w.Map.XOffset + w.Map.Width;
			var minj = w.Map.YOffset; var maxj = w.Map.YOffset + w.Map.Height;

			for (var j = minj; j < maxj; j++)
				for (var i = mini; i < maxi; i++)
					if (IsBridge(w, w.Map.MapTiles[i, j].tile))
						ConvertBridgeToActor(w, i, j);
		}

		static void ConvertBridgeToActor(World w, int i, int j)
		{
			var tile = w.Map.MapTiles[i, j].tile;
			var image = w.Map.MapTiles[i, j].image;
			var template = w.TileSet.walk[tile];

			// base position of the tile
			var ni = i - image % template.Size.X;
			var nj = j - image / template.Size.X;

			var replacedTiles = new Dictionary<int2, int>();
			for (var y = nj; y < nj + template.Size.Y; y++)
				for (var x = ni; x < ni + template.Size.X; x++)
				{
					var n = (x - ni) + template.Size.X * (y - nj);
					if (!template.TerrainType.ContainsKey(n)) continue;

					if (w.Map.IsInMap(x, y))
						if (w.Map.MapTiles[x, y].tile == tile 
							&& w.Map.MapTiles[x,y].image == n)
						{
							// stash it
							replacedTiles[new int2(x, y)] = w.Map.MapTiles[x, y].image;
							// remove the tile from the actual map
							w.Map.MapTiles[x, y].tile = 0xfffe;
							w.Map.MapTiles[x, y].image = 0;
						}
				}

			if (replacedTiles.Any())
			{
				var a = w.CreateActor("Bridge", new int2(ni, nj), null);
				var br = a.traits.Get<Bridge>();
				br.SetTiles(template, replacedTiles);
			}
		}

		static bool IsBridge(World w, ushort t)
		{
			return w.TileSet.walk[t].IsBridge;
		}
	}
}
