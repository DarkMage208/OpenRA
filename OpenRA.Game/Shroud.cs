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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public class Shroud
	{
		Traits.Shroud shroud;
		Sprite[] shadowBits = SpriteSheetBuilder.LoadAllSprites("shadow");
		Sprite[,] sprites, fogSprites;
		
		bool dirty = true;
		bool hasGPS = false;
		Player owner;
		Map map;

		public Rectangle? bounds { get { return shroud.exploredBounds; } }

		public Shroud(Player owner, Map map)
		{
			this.shroud = owner.World.WorldActor.traits.Get<Traits.Shroud>();
			this.owner = owner;
			this.map = map;
			
			sprites = new Sprite[map.MapSize, map.MapSize];
			fogSprites = new Sprite[map.MapSize, map.MapSize];
		}

		public bool HasGPS
		{
			get { return hasGPS; }
			set { hasGPS = value; dirty = true;}
		}

		public bool IsExplored(int2 xy) { return IsExplored(xy.X, xy.Y); }
		bool IsExplored(int x, int y) { return shroud.exploredCells[x,y]; }

		public bool DisplayOnRadar(int x, int y) { return IsExplored(x, y); }
		
		public void ResetExplored() { }	// todo

		public void Explore(World w, int2 center, int range) { dirty = true; }
		public void Explore(Actor a) { if (a.Owner == a.World.LocalPlayer) dirty = true; }

		static readonly byte[][] SpecialShroudTiles =
		{
			new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
			new byte[] { 32, 32, 25, 25, 19, 19, 20, 20 },
			new byte[] { 33, 33, 33, 33, 26, 26, 26, 26, 21, 21, 21, 21, 23, 23, 23, 23 },
			new byte[] { 36, 36, 36, 36, 30, 30, 30, 30 },
			new byte[] { 34, 16, 34, 16, 34, 16, 34, 16, 27, 22, 27, 22, 27, 22, 27, 22 },
			new byte[] { 44 },
			new byte[] { 37, 37, 37, 37, 37, 37, 37, 37, 31, 31, 31, 31, 31, 31, 31, 31 },
			new byte[] { 40 },
			new byte[] { 35, 24, 17, 18 },
			new byte[] { 39, 39, 29, 29 },
			new byte[] { 45 },
			new byte[] { 43 },
			new byte[] { 38, 28 },
			new byte[] { 42 },
			new byte[] { 41 },
			new byte[] { 46 },
		};
		
		Sprite ChooseShroud(int i, int j)
		{
			if( !IsExplored( i, j ) ) return shadowBits[ 0xf ];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if( !IsExplored( i, j - 1 ) ) { v |= 1; u |= 3; }
			if( !IsExplored( i + 1, j ) ) { v |= 2; u |= 6; }
			if( !IsExplored( i, j + 1 ) ) { v |= 4; u |= 12; }
			if( !IsExplored( i - 1, j ) ) { v |= 8; u |= 9; }

			var uSides = u;

			if( !IsExplored( i - 1, j - 1 ) ) u |= 1;
			if( !IsExplored( i + 1, j - 1 ) ) u |= 2;
			if( !IsExplored( i + 1, j + 1 ) ) u |= 4;
			if( !IsExplored( i - 1, j + 1 ) ) u |= 8;

			return shadowBits[ SpecialShroudTiles[ u ^ uSides ][ v ] ];
		}

		Sprite ChooseFog(int i, int j)
		{
			if (shroud.visibleCells[i, j] == 0) return shadowBits[0xf];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if (shroud.visibleCells[i, j - 1] == 0) { v |= 1; u |= 3; }
			if (shroud.visibleCells[i + 1, j] == 0) { v |= 2; u |= 6; }
			if (shroud.visibleCells[i, j + 1] == 0) { v |= 4; u |= 12; }
			if (shroud.visibleCells[i - 1, j] == 0) { v |= 8; u |= 9; }

			var uSides = u;

			if (shroud.visibleCells[i - 1, j - 1] == 0) u |= 1;
			if (shroud.visibleCells[i + 1, j - 1] == 0) u |= 2;
			if (shroud.visibleCells[i + 1, j + 1] == 0) u |= 4;
			if (shroud.visibleCells[i - 1, j + 1] == 0) u |= 8;

			return shadowBits[SpecialShroudTiles[u ^ uSides][v]];
		}

		internal void Draw(SpriteRenderer r)
		{
			if (dirty)
			{
				dirty = false;
				for (int j = map.YOffset; j < map.YOffset + map.Height; j++)
					for (int i = map.XOffset; i < map.XOffset + map.Width; i++)
						sprites[i, j] = ChooseShroud(i, j);
				for (int j = map.YOffset; j < map.YOffset + map.Height; j++)
					for (int i = map.XOffset; i < map.XOffset + map.Width; i++)
						fogSprites[i, j] = ChooseFog(i, j);
			}

			var miny = bounds.HasValue ? Math.Max(map.YOffset, bounds.Value.Top) : map.YOffset;
			var maxy = bounds.HasValue ? Math.Min(map.YOffset + map.Height, bounds.Value.Bottom) : map.YOffset + map.Height;

			var minx = bounds.HasValue ? Math.Max(map.XOffset, bounds.Value.Left) : map.XOffset;
			var maxx = bounds.HasValue ? Math.Min(map.XOffset + map.Width, bounds.Value.Right) : map.XOffset + map.Width;

			var shroudPalette = "fog";

			for (var j = miny; j < maxy; j++)
			{
				var starti = minx;
				for (var i = minx; i < maxx; i++)
				{
					if (fogSprites[i, j] == shadowBits[0x0f])
						continue;

					if (starti != i)
					{
						r.DrawSprite(fogSprites[starti, j],
						    Game.CellSize * new float2(starti, j),
						    shroudPalette,
						    new float2(Game.CellSize * (i - starti), Game.CellSize));
						starti = i+1;
					}

					r.DrawSprite(fogSprites[i, j],
						Game.CellSize * new float2(i, j),
						shroudPalette);
					starti = i+1;
				}

				if (starti < maxx)
					r.DrawSprite(fogSprites[starti, j],
						Game.CellSize * new float2(starti, j),
						shroudPalette,
						new float2(Game.CellSize * (maxx - starti), Game.CellSize));
			}

			shroudPalette = "shroud";

			for (var j = miny; j < maxy; j++)
			{
				var starti = minx;
				for (var i = minx; i < maxx; i++)
				{
					if (sprites[i, j] == shadowBits[0x0f])
						continue;

					if (starti != i)
					{
						r.DrawSprite(sprites[starti, j],
							Game.CellSize * new float2(starti, j),
							shroudPalette,
							new float2(Game.CellSize * (i - starti), Game.CellSize));
						starti = i + 1;
					}

					r.DrawSprite(sprites[i, j],
						Game.CellSize * new float2(i, j),
						shroudPalette);
					starti = i + 1;
				}

				if (starti < maxx)
					r.DrawSprite(sprites[starti, j],
						Game.CellSize * new float2(starti, j),
						shroudPalette,
						new float2(Game.CellSize * (maxx - starti), Game.CellSize));
			}
		}
	}
}
