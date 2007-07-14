using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;
using System.Runtime.InteropServices;
using OpenRa.TechTree;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		readonly Renderer renderer;
		readonly Map map;
		
		Package TileMix;

		World world;
		TreeCache treeCache;
		TerrainRenderer terrain;
		Sidebar sidebar;
		Viewport viewport;

		static Size GetResolution(Settings settings)
		{
			Size desktopResolution = Screen.PrimaryScreen.Bounds.Size;

			return new Size(settings.GetValue("width", desktopResolution.Width),
				settings.GetValue("height", desktopResolution.Height));
		}

		public MainWindow( Settings settings )
		{
			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.Black;
			StartPosition = FormStartPosition.Manual;
			Location = new Point();
			Visible = true;

			renderer = new Renderer(this, GetResolution(settings), false);
			viewport = new Viewport(ClientSize);

			SheetBuilder.Initialize(renderer.Device);

			map = new Map(new IniFile(File.OpenRead("../../../" + settings.GetValue("map", "scm12ea.ini"))));

			TileMix = new Package("../../../" + map.Theater + ".mix");

			renderer.SetPalette(new HardwarePalette(renderer.Device, map));
			terrain = new TerrainRenderer(renderer, map, TileMix);

			world = new World(renderer);
			treeCache = new TreeCache(renderer.Device, map, TileMix);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map));

			world.Add(new Mcv(new PointF(24 * 5, 24 * 5), 3));
			world.Add(new Mcv(new PointF(24 * 7, 24 * 5), 2));
			world.Add(new Mcv(new PointF(24 * 9, 24 * 5), 1));

			world.Add(new Refinery(new PointF(24 * 5, 24 * 7), 1));

			sidebar = new Sidebar(Race.Soviet, renderer);
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Frame();
				Application.DoEvents();
			}
		}

		float2 lastPos;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			lastPos = new float2(e.Location);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button == 0)
				return;

			float2 scrollPos = new float2(viewport.ScrollPosition) + lastPos - new float2(e.Location);
			float2 mapSize = 24 * new float2(map.Size) - viewport.Size + new float2(128, 0);

			scrollPos = scrollPos.Constrain(new Range<float2>(float2.Zero, mapSize));

			lastPos = new float2(e.Location);

			viewport.ScrollPosition = scrollPos.ToPointF();
		}

		void Frame()
		{
			PointF r1 = new PointF(2.0f / viewport.ClientSize.Width, -2.0f / viewport.ClientSize.Height);
			PointF r2 = new PointF(-1, 1);

			renderer.BeginFrame(r1, r2, viewport.ScrollPosition);

			renderer.Device.EnableScissor(0, 0, viewport.ClientSize.Width - 128, viewport.ClientSize.Height);
			terrain.Draw(viewport);

			world.Draw(renderer, viewport);

			renderer.Device.DisableScissor();
			sidebar.Paint(viewport);

			renderer.EndFrame();
		}
	}
}
