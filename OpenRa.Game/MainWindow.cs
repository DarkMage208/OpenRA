using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;
using OpenRa.TechTree;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		readonly Renderer renderer;
		
		Game game;
		public readonly Sidebar sidebar;

		static Size GetResolution(Settings settings)
		{
			Size desktopResolution = Screen.PrimaryScreen.Bounds.Size;

			return new Size(settings.GetValue("width", desktopResolution.Width),
				settings.GetValue("height", desktopResolution.Height));
		}

		public MainWindow(Settings settings)
		{
			FileSystem.Mount(new Folder("../../../"));
			FileSystem.Mount(new Package("../../../conquer.mix"));
			FileSystem.Mount(new Package("../../../hires.mix"));

			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.Black;
			StartPosition = FormStartPosition.Manual;
			Location = Point.Empty;
			Visible = true;

			bool windowed = !settings.GetValue("fullscreeen", false);
			renderer = new Renderer(this, GetResolution(settings), windowed);
			SheetBuilder.Initialize( renderer );

			game = new Game( settings.GetValue( "map", "scm12ea.ini" ), renderer, new int2( ClientSize ) );

			SequenceProvider.ForcePrecache();

			game.world.Add( new Mcv( new int2( 5, 5 ), 3 ) );
			game.world.Add( new Mcv( new int2( 7, 5 ), 2 ) );
			Mcv mcv = new Mcv( new int2( 9, 5 ), 1 );
			game.world.myUnit = mcv;
			game.world.Add( mcv );
			game.world.Add( new Refinery( new int2( 7, 5 ), 2 ) );

			sidebar = new Sidebar(Race.Soviet, renderer, game.viewport);

			renderer.SetPalette( new HardwarePalette( renderer, game.map ) );
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				game.Tick();
				Application.DoEvents();
			}
		}

		float2 lastPos;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			lastPos = new float2(e.Location);

			if (e.Button == MouseButtons.Left)
			{
				float2 point = new float2(e.Location) + game.viewport.Location;
				RectangleF rect = new RectangleF(sidebar.Location.ToPointF(), new SizeF(sidebar.Width, game.viewport.Height));
				if (rect.Contains(point.ToPointF()))
				{
					sidebar.Build(sidebar.FindSpriteAtPoint(point));
					return;
				}
				float2 xy = (1 / 24.0f) * point;
				IOrder order = game.world.myUnit.Order( new int2( (int)xy.X, (int)xy.Y ) );
				game.Issue( order );
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button == MouseButtons.Right)
			{
				float2 p = new float2(e.Location);
				game.viewport.Scroll(lastPos - p);
				lastPos = p;
			}
		}
	}
}
