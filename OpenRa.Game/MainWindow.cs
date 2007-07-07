using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		readonly GraphicsDevice device;
		readonly Map map;
		readonly TileSet tileSet;
		
		Palette pal;
		Package TileMix;
		string TileSuffix;

		const string mapName = "scm12ea.ini";

		Dictionary<TileReference, SheetRectangle<Sheet>> tileMapping =
			new Dictionary<TileReference, SheetRectangle<Sheet>>();

		FvfVertexBuffer<Vertex> vertexBuffer;

		void LoadTextures()
		{
			List<Sheet> tempSheets = new List<Sheet>();

			Provider<Sheet> sheetProvider = delegate
			{
				Sheet t = new Sheet( new Bitmap(256, 256));
				tempSheets.Add(t);
				return t;
			};

			TileSheetBuilder<Sheet> builder = new TileSheetBuilder<Sheet>( new Size(256,256), sheetProvider );

			for( int i = 0; i < 128; i++ )
				for (int j = 0; j < 128; j++)
				{
					TileReference tileRef = map.MapTiles[i, j];

					if (!tileMapping.ContainsKey(tileRef))
					{
						SheetRectangle<Sheet> rect = builder.AddImage(new Size(24, 24));
						Bitmap srcImage = tileSet.tiles[ tileRef.tile ].GetTile( tileRef.image );
						using (Graphics g = Graphics.FromImage(rect.sheet.bitmap))
							g.DrawImage(srcImage, rect.origin);

						tileMapping.Add(tileRef, rect);
					}
				}

			foreach (Sheet s in tempSheets)
				s.LoadTexture(device);
		}

		void LoadVertexBuffer()
		{
			Dictionary<Sheet, List<ushort>> indexMap = new Dictionary<Sheet, List<ushort>>();

			Vertex[] vertices = new Vertex[4 * 128 * 128];//map.Width * map.Height];

			for( int i = 0; i < 128; i++ )
				for (int j = 0; j < 128; j++)
				{
					SheetRectangle<Sheet> tile = tileMapping[map.MapTiles[i, j]];

					ushort offset = (ushort)(4 * (i * 128 + j));

					vertices[offset] = new Vertex(24 * i, 24 * j, 0, 0, 0);
					vertices[offset + 1] = new Vertex(24 + 24 * i, 24 * j, 0, 1, 0);
					vertices[offset + 2] = new Vertex(24 * i, 24 + 24 * j, 0, 0, 1);
					vertices[offset + 3] = new Vertex(24 + 24 * i, 24 + 24 * j, 0, 1, 1);

					List<ushort> indexList;
					if (!indexMap.TryGetValue(tile.sheet, out indexList))
						indexMap.Add(tile.sheet, indexList = new List<ushort>());

					indexList.Add(offset);
					indexList.Add((ushort)(offset + 1));
					indexList.Add((ushort)(offset + 2));

					indexList.Add((ushort)(offset + 1));
					indexList.Add((ushort)(offset + 3));
					indexList.Add((ushort)(offset + 2));
				}

			vertexBuffer = new FvfVertexBuffer<Vertex>(device, vertices.Length, Vertex.Format);
			vertexBuffer.SetData(vertices);

			Dictionary<Sheet, IndexBuffer> indexBuffers = new Dictionary<Sheet, IndexBuffer>();

		}

		public MainWindow()
		{
			ClientSize = new Size(640, 480);

			Visible = true;

			device = GraphicsDevice.Create(this, ClientSize.Width, ClientSize.Height, true, false);

			IniFile mapFile = new IniFile(File.OpenRead("../../../" + mapName));
			map = new Map(mapFile);

			Text = string.Format("OpenRA - {0} - {1}", map.Title, mapName);

			tileSet = LoadTileSet(map);

			LoadTextures();
			LoadVertexBuffer();
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Frame();
				Application.DoEvents();
			}
		}

		void Frame()
		{
			device.Begin();
			device.Clear(0);

			// render something :)

			//vertexBuffer.Bind(0);
			//indexBuffer.Bind();

			//device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 2 * map.Width * map.Height);

			device.End();
			device.Present();
		}

		TileSet LoadTileSet(Map currentMap)
		{
			switch (currentMap.Theater.ToLowerInvariant())
			{
				case "temperate":
					pal = new Palette(File.OpenRead("../../../temperat.pal"));
					TileMix = new Package("../../../temperat.mix");
					TileSuffix = ".tem";
					break;
				case "snow":
					pal = new Palette(File.OpenRead("../../../snow.pal"));
					TileMix = new Package("../../../snow.mix");
					TileSuffix = ".sno";
					break;
				case "interior":
					pal = new Palette(File.OpenRead("../../../interior.pal"));
					TileMix = new Package("../../../interior.mix");
					TileSuffix = ".int";
					break;
				default:
					throw new NotImplementedException();
			}
			return new TileSet(TileMix, TileSuffix, pal);
		}

		
	}
}
