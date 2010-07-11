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
using System.Drawing.Imaging;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	class Minimap
	{
		readonly World world;
		Sheet sheet;
		SpriteRenderer rgbaRenderer;
		Sprite sprite;
		Bitmap terrain, customLayer;
		Rectangle bounds;
		
		const int alpha = 230;

		public Minimap(World world, Renderer r)
		{
			this.world = world;
			sheet = new Sheet(r, new Size(world.Map.MapSize.X, world.Map.MapSize.Y));
			rgbaRenderer = r.RgbaSpriteRenderer;
			var size = Math.Max(world.Map.Width, world.Map.Height);
			var dw = (size - world.Map.Width) / 2;
			var dh = (size - world.Map.Height) / 2;

			bounds = new Rectangle(world.Map.TopLeft.X - dw, world.Map.TopLeft.Y - dh, size, size);

			sprite = new Sprite(sheet, bounds, TextureChannel.Alpha);
		
			shroudColor = Color.FromArgb(alpha, Color.Black);
		}

		public static Rectangle MakeMinimapBounds(Map m)
		{
			var size = Math.Max(m.Width, m.Height);
			var dw = (size - m.Width) / 2;
			var dh = (size - m.Height) / 2;

			return new Rectangle(m.TopLeft.X - dw, m.TopLeft.Y - dh, size, size);
		}
		
		static Color shroudColor;

		public void InvalidateCustom() { customLayer = null; }

		public static Bitmap RenderTerrainBitmap(Map map, TileSet tileset)
		{
			var terrain = new Bitmap(map.MapSize.X, map.MapSize.Y);
			
			for (var x = 0; x < map.MapSize.X; x++)
				for (var y = 0; y < map.MapSize.Y; y++)
				{
					var type = tileset.GetTerrainType(map.MapTiles[x, y]);
					terrain.SetPixel(x, y, map.IsInMap(x, y)
						? Color.FromArgb(alpha, tileset.Terrain[type].Color)
						: shroudColor);
				}
			return terrain;
		}

		public void Update()
		{			
			if (terrain == null)
				terrain = RenderTerrainBitmap(world.Map, world.TileSet);

			// Custom terrain layer
			if (customLayer == null)
			{
				customLayer = new Bitmap(terrain);
				for (var x = world.Map.TopLeft.X; x < world.Map.BottomRight.X; x++)
					for (var y = world.Map.TopLeft.Y; y < world.Map.BottomRight.Y; y++)
					{
						var customTerrain = world.WorldActor.traits.WithInterface<ITerrainTypeModifier>()
							.Select( t => t.GetTerrainType(new int2(x,y)) )
							.FirstOrDefault( t => t != null );
						if (customTerrain == null) continue;
						customLayer.SetPixel(x, y, Color.FromArgb(alpha, world.TileSet.Terrain[customTerrain].Color));
					}							
			}

			if (!world.GameHasStarted || !world.Queries.OwnedBy[world.LocalPlayer].WithTrait<ProvidesRadar>().Any())
				return;

			var bitmap = new Bitmap(customLayer);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				foreach (var a in world.Queries.WithTrait<Unit>().Where(a => a.Actor.Owner != null && a.Actor.IsVisible()))
					*(c + (a.Actor.Location.Y * bitmapData.Stride >> 2) + a.Actor.Location.X) =
						Color.FromArgb(alpha, a.Actor.Owner.Color).ToArgb();

				for (var x = world.Map.TopLeft.X; x < world.Map.BottomRight.X; x++)
					for (var y = world.Map.TopLeft.Y; y < world.Map.BottomRight.Y; y++)
					{
						if (!world.LocalPlayer.Shroud.DisplayOnRadar(x, y))
						{
							*(c + (y * bitmapData.Stride >> 2) + x) = shroudColor.ToArgb();
							continue;
						}
						var b = world.WorldActor.traits.Get<BuildingInfluence>().GetBuildingAt(new int2(x, y));
						
						if (b != null)
							*(c + (y * bitmapData.Stride >> 2) + x) = Color.FromArgb(alpha, b.Owner.Color).ToArgb();
					}
			}

			bitmap.UnlockBits(bitmapData);
			sheet.Texture.SetData(bitmap);
		}

		public void Draw(RectangleF rect)
		{
			rgbaRenderer.DrawSprite(sprite, 
				new float2(rect.X, rect.Y), "chrome", new float2(rect.Width, rect.Height));
			rgbaRenderer.Flush();
		}

		int2 CellToMinimapPixel(RectangleF viewRect, int2 p)
		{
			var fx = (float)(p.X - bounds.X) / bounds.Width;
			var fy = (float)(p.Y - bounds.Y) / bounds.Height;

			return new int2(
				(int)(viewRect.Width * fx + viewRect.Left),
				(int)(viewRect.Height * fy + viewRect.Top));
		}

		public int2 MinimapPixelToCell(RectangleF viewRect, int2 p)
		{
			var fx = (float)(p.X - viewRect.Left) / viewRect.Width;
			var fy = (float)(p.Y - viewRect.Top) / viewRect.Height;

			return new int2(
				(int)(bounds.Width * fx + bounds.Left),
				(int)(bounds.Height * fy + bounds.Top));
		}
	}
}
