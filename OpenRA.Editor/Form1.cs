﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;
			LocateGameRoot();

			LoadMap("ra", "mjolnir");
		}

		string loadedMapName;
		string colors;
		TileSet tileset;

		void LoadMap(string mod, string mapname)
		{
			tilePalette.Controls.Clear();
			actorPalette.Controls.Clear();
			resourcePalette.Controls.Clear();

			var mods = new[] { mod };

			var manifest = new Manifest(mods);

			FileSystem.UnmountAll();
			foreach (var folder in manifest.Folders) FileSystem.Mount(folder);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);

			// load the map
			var map = new Map(new Folder("mods/{0}/maps/{1}".F(mod, mapname)));

			Game.LoadModAssemblies(manifest);
			Rules.LoadRules(manifest, map);

			// we're also going to need a tileset...
			var tsinfo = fileMapping[Pair.New(mods[0], map.Theater)];
			tileset = new TileSet("tileset.til", "templates.ini", tsinfo.First);

			colors = tsinfo.Second;

			var palette = new Palette(FileSystem.Open(map.Theater.ToLowerInvariant() + ".pal"), true);

			surface1.Bind(map, tileset, palette);

			// construct the palette of tiles

			foreach (var n in tileset.tiles.Keys)
			{
				try
				{
					var bitmap = RenderTemplate(tileset, (ushort)n, palette);
					var ibox = new PictureBox
					{
						Image = bitmap,
						Width = bitmap.Width / 2,
						Height = bitmap.Height / 2,
						SizeMode = PictureBoxSizeMode.StretchImage
					};

					var brushTemplate = new BrushTemplate { Bitmap = bitmap, N = n };
					ibox.Click += (_, e) => surface1.SetBrush(brushTemplate);

					var template = tileset.walk[n];
					tilePalette.Controls.Add(ibox);

					tt.SetToolTip(ibox,
						"{1}:{0} ({3}x{4} {2})".F(
						template.Name,
						template.Index,
						template.Bridge,
						template.Size.X,
						template.Size.Y));
				}
				catch { }
			}

			var actorTemplates = new List<ActorTemplate>();

			foreach (var a in Rules.Info.Keys)
			{
				try
				{
					var info = Rules.Info[a];
					var template = RenderActor(info, tsinfo.First, palette);
					var ibox = new PictureBox
					{
						Image = template.Bitmap,
						Width = template.Bitmap.Width / 2,
						Height = template.Bitmap.Height / 2,
						SizeMode = PictureBoxSizeMode.StretchImage
					};

					ibox.Click += (_, e) => surface1.SetActor(template);

					actorPalette.Controls.Add(ibox);

					tt.SetToolTip(ibox,
						"{0}:{1}".F(
						info.Name,
						info.Category));

					actorTemplates.Add( template);
				}
				catch { }
			}

			surface1.BindActorTemplates(actorTemplates);

			var resourceTemplates = new List<ResourceTemplate>();

			foreach (var a in Rules.Info["world"].Traits.WithInterface<ResourceTypeInfo>())
			{
				try
				{
					var template = RenderResourceType(a, tsinfo.First, palette);
					var ibox = new PictureBox
					{
						Image = template.Bitmap,
						Width = template.Bitmap.Width,
						Height = template.Bitmap.Height,
						SizeMode = PictureBoxSizeMode.StretchImage
					};

					ibox.Click += (_, e) => surface1.SetResource(template);

					resourcePalette.Controls.Add(ibox);

					tt.SetToolTip(ibox,
						"{0}:{1}cr".F(
						template.Info.Name,
						template.Info.ValuePerUnit));

					resourceTemplates.Add(template);
				}
				catch { }
			}

			surface1.BindResourceTemplates(resourceTemplates);
			loadedMapName = mapname;
		}

		void LocateGameRoot()
		{
			while (!Directory.Exists("mods"))
			{
				var current = Directory.GetCurrentDirectory();
				if (Directory.GetDirectoryRoot(current) == current)
					throw new InvalidOperationException("Unable to find game root.");
				Directory.SetCurrentDirectory("..");
			}
		}

		static Dictionary<Pair<string, string>, Pair<string, string>> fileMapping = new Dictionary<Pair<string, string>, Pair<string, string>>()
		{
			{Pair.New("ra","TEMPERAT"),Pair.New("tem","temperat.col")},
			{Pair.New("ra","SNOW"),Pair.New("sno","snow.col")},
			{Pair.New("ra","INTERIOR"),Pair.New("int","temperat.col")},
			{Pair.New("cnc","DESERT"),Pair.New("des","desert.col")},
			{Pair.New("cnc","TEMPERAT"),Pair.New("tem","temperat.col")},
			{Pair.New("cnc","WINTER"),Pair.New("win","winter.col")},
		};

		static Bitmap RenderTemplate(TileSet ts, ushort n, Palette p)
		{
			var template = ts.walk[n];
			var tile = ts.tiles[n];

			var bitmap = new Bitmap(24 * template.Size.X, 24 * template.Size.Y);
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* q = (int*)data.Scan0.ToPointer();
				var stride = data.Stride >> 2;

				for (var u = 0; u < template.Size.X; u++)
					for (var v = 0; v < template.Size.Y; v++)
						if (tile.TileBitmapBytes[u + v * template.Size.X] != null)
						{
							var rawImage = tile.TileBitmapBytes[u + v * template.Size.X];
							for (var i = 0; i < 24; i++)
								for (var j = 0; j < 24; j++)
									q[(v * 24 + j) * stride + u * 24 + i] = p.GetColor(rawImage[i + 24 * j]).ToArgb();
						}
						else
						{
							for (var i = 0; i < 24; i++)
								for (var j = 0; j < 24; j++)
									q[(v * 24 + j) * stride + u * 24 + i] = Color.Transparent.ToArgb();
						}
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		static ActorTemplate RenderActor(ActorInfo info, string ext, Palette p)
		{
			var image = info.Traits.Get<RenderSimpleInfo>().Image ?? info.Name;
			using (var s = FileSystem.OpenWithExts(image, "." + ext, ".shp"))
			{
				var shp = new ShpReader(s);
				var frame = shp[0];

				var bitmap = new Bitmap(shp.Width, shp.Height);
				var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				unsafe
				{
					int* q = (int*)data.Scan0.ToPointer();
					var stride = data.Stride >> 2;

					for (var i = 0; i < shp.Width; i++)
						for (var j = 0; j < shp.Height; j++)
							q[j * stride + i] = p.GetColor(frame.Image[i + shp.Width * j]).ToArgb();
				}

				bitmap.UnlockBits(data);
				return new ActorTemplate { Bitmap = bitmap, Info = info, Centered = !info.Traits.Contains<BuildingInfo>() };
			}
		}

		static ResourceTemplate RenderResourceType(ResourceTypeInfo info, string ext, Palette p)
		{
			var image = info.SpriteNames[0];
			using (var s = FileSystem.OpenWithExts(image, "." + ext, ".shp"))
			{
				var shp = new ShpReader(s);
				var frame = shp[shp.ImageCount - 1];

				var bitmap = new Bitmap(shp.Width, shp.Height);
				var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				unsafe
				{
					int* q = (int*)data.Scan0.ToPointer();
					var stride = data.Stride >> 2;

					for (var i = 0; i < shp.Width; i++)
						for (var j = 0; j < shp.Height; j++)
							q[j * stride + i] = p.GetColor(frame.Image[i + shp.Width * j]).ToArgb();
				}

				bitmap.UnlockBits(data);
				return new ResourceTemplate { Bitmap = bitmap, Info = info, Value = shp.ImageCount - 1 };
			}
		}

		void ResizeClicked(object sender, EventArgs e)
		{
			using (var rd = new ResizeDialog())
			{
				rd.width.Value = surface1.Map.MapSize.X;
				rd.height.Value = surface1.Map.MapSize.Y;
				rd.cordonLeft.Value = surface1.Map.TopLeft.X;
				rd.cordonTop.Value = surface1.Map.TopLeft.Y;
				rd.cordonRight.Value = surface1.Map.BottomRight.X;
				rd.cordonBottom.Value = surface1.Map.BottomRight.Y;

				if (DialogResult.OK != rd.ShowDialog())
					return;

				surface1.Map.TopLeft = new int2((int)rd.cordonLeft.Value, (int)rd.cordonTop.Value);
				surface1.Map.BottomRight = new int2((int)rd.cordonRight.Value, (int)rd.cordonBottom.Value);

				if ((int)rd.width.Value != surface1.Map.MapSize.X || (int)rd.height.Value != surface1.Map.MapSize.Y)
				{
					surface1.Map.Resize((int)rd.width.Value, (int)rd.height.Value);
					surface1.Bind(surface1.Map, surface1.TileSet, surface1.Palette);	// rebind it to invalidate all caches
				}

				surface1.Invalidate();
			}
		}

		void SavePreviewImage(string filepath)
		{
			var Map = surface1.Map;
			
			var xs = Map.TopLeft.X;
			var ys = Map.TopLeft.Y;

			var terrainTypeColors = new TerrainColorSet(colors);

			var bitmap = new Bitmap(Map.Width, Map.Height);
			for (var x = 0; x < Map.Width; x++)
				for (var y = 0; y < Map.Height; y++)
					bitmap.SetPixel(x, y, terrainTypeColors.ColorForTerrainType(
						tileset.GetTerrainType(Map.MapTiles[x + xs, y + ys])));

			for (var x = 0; x < Map.Width; x++)
				for (var y = 0; y < Map.Height; y++)
					if (Map.MapResources[x + xs, y + ys].type > 0)
						bitmap.SetPixel(x, y, terrainTypeColors.ColorForTerrainType(TerrainType.Ore));

			bitmap.Save(filepath, ImageFormat.Png);
		}

		void SaveClicked(object sender, EventArgs e)
		{
			if (loadedMapName == null)
				SaveAsClicked(sender, e);
			else
			{
				surface1.Map.Package = new Folder(loadedMapName);
				SavePreviewImage(Path.Combine(loadedMapName, "preview.png"));
				surface1.Map.Save(loadedMapName);
			}
		}

		void SaveAsClicked(object sender, EventArgs e)
		{
			if (DialogResult.OK == folderBrowser.ShowDialog())
			{
				loadedMapName = folderBrowser.SelectedPath;
				SaveClicked(sender, e);
			}
		}
	}
}