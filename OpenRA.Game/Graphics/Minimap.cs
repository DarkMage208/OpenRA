#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
	public class Minimap
	{		
		public static Bitmap TerrainBitmap(Map map)
		{
			var tileset = Rules.TileSets[map.Tileset];
			var size = Util.NextPowerOf2(Math.Max(map.Width, map.Height));
			Bitmap terrain = new Bitmap(size, size);
			
			var bitmapData = terrain.LockBits(new Rectangle(0, 0, terrain.Width, terrain.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;
						var type = tileset.GetTerrainType(map.MapTiles[mapX, mapY]);
						*(c + (y * bitmapData.Stride >> 2) + x) = tileset.Terrain[type].Color.ToArgb();
					}
			}
			terrain.UnlockBits(bitmapData);
			return terrain;
		}
		
		// Add the static resources defined in the map; if the map lives
		// in a world use AddCustomTerrain instead
		public static Bitmap AddStaticResources(Map map, Bitmap terrainBitmap)
		{
			Bitmap terrain = new Bitmap(terrainBitmap);
			var tileset = Rules.TileSets[map.Tileset];

			var bitmapData = terrain.LockBits(new Rectangle(0, 0, terrain.Width, terrain.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;	
						if (map.MapResources[mapX, mapY].type == 0)
							continue;
					
						var res = Rules.Info["world"].Traits.WithInterface<ResourceTypeInfo>()
								.Where(t => t.ResourceType == map.MapResources[mapX, mapY].type)
								.Select(t => t.TerrainType).FirstOrDefault();
						if (res == null)
							continue;
						
						*(c + (y * bitmapData.Stride >> 2) + x) = tileset.Terrain[res].Color.ToArgb();
					}
			}
			terrain.UnlockBits(bitmapData);

			return terrain;
		}
		
		public static Bitmap CustomTerrainBitmap(World world)
		{
			var map = world.Map;
			var size = Util.NextPowerOf2(Math.Max(map.Width, map.Height));
			Bitmap bitmap = new Bitmap(size, size);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;
						var custom = map.CustomTerrain[mapX,mapY];
						if (custom == null)
							continue;
						*(c + (y * bitmapData.Stride >> 2) + x) = world.TileSet.Terrain[custom].Color.ToArgb();
					}
			}
			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}
				
		public static Bitmap ActorsBitmap(World world)
		{	
			var map = world.Map;
			var size = Util.NextPowerOf2(Math.Max(map.Width, map.Height));
			Bitmap bitmap = new Bitmap(size, size);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
		
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;
				
				foreach (var t in world.Queries.WithTraitMultiple<IRadarSignature>())
				{
					if (!t.Actor.IsVisible())
						continue;
					
					var color = t.Trait.RadarSignatureColor(t.Actor);
					foreach (var cell in t.Trait.RadarSignatureCells(t.Actor))
						if (world.Map.IsInMap(cell))
							*(c + ((cell.Y - world.Map.TopLeft.Y) * bitmapData.Stride >> 2) + cell.X - world.Map.TopLeft.X) = color.ToArgb();
				}
			}

			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}
		
		public static Bitmap ShroudBitmap(World world)
		{	
			var map = world.Map;
			var size = Util.NextPowerOf2(Math.Max(map.Width, map.Height));
			Bitmap bitmap = new Bitmap(size, size);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			var shroud = Color.Black.ToArgb();
			var fog = Color.FromArgb(128, Color.Black).ToArgb();
						
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;
				
				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;
						if (!world.LocalPlayer.Shroud.IsExplored(mapX, mapY))
							*(c + (y * bitmapData.Stride >> 2) + x) = shroud;
						else if (!world.LocalPlayer.Shroud.IsVisible(mapX,mapY))					
							*(c + (y * bitmapData.Stride >> 2) + x) = fog;
					}
			}

			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}
		
		public static Bitmap RenderMapPreview(Map map)
		{
			Bitmap terrain = TerrainBitmap(map);
			return AddStaticResources(map, terrain);
		}
	}
}
