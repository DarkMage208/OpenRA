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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	interface IHandleInput
	{
		bool HandleInput(World world, MouseInput mi);
	}

	class Viewport
	{
		readonly float2 screenSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public float2 Location { get { return scrollPosition; } }

		public int Width { get { return (int)screenSize.X; } }
		public int Height { get { return (int)screenSize.Y; } }

		SpriteRenderer cursorRenderer;
		int2 mousePos;
		float cursorFrame = 0f;

		public void Scroll(float2 delta)
		{
			scrollPosition = scrollPosition + delta;
		}

		public IEnumerable<IHandleInput> regions { get { return new IHandleInput[] { Game.chrome, Game.controller }; } }

		public Viewport(float2 screenSize, int2 mapStart, int2 mapEnd, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.renderer = renderer;
			cursorRenderer = new SpriteRenderer(renderer, true);

			this.scrollPosition = Game.CellSize* mapStart;
		}

		public void DrawRegions( World world )
		{
			world.WorldRenderer.palette.Update(world.Queries.WithTraitMultiple<IPaletteModifier>().Select(t=>t.Trait));

			float2 r1 = new float2(2, -2) / screenSize;
			float2 r2 = new float2(-1, 1);

			renderer.BeginFrame(r1, r2, scrollPosition);

			if( Game.orderManager.GameStarted )
			{
				world.WorldRenderer.Draw();
				Game.chrome.Draw( world );
				

				if( Game.orderManager.Connection.ConnectionState == ConnectionState.NotConnected )
					Game.chrome.DrawDialog("Connection lost.");
			}
			else
			{
				// what a hack. as soon as we have some real chrome stuff...

				Game.chrome.DrawWidgets(world);
				switch( Game.orderManager.Connection.ConnectionState )
				{
					case ConnectionState.Connecting:
						Game.chrome.DrawDialog("Connecting to {0}:{1}...".F( Game.Settings.NetworkHost, Game.Settings.NetworkPort ));
						break;
					case ConnectionState.NotConnected:
						// Todo: Hook these up
						Game.chrome.DrawDialog("Connection failed.", "Retry", _ => {}, "Cancel",_ => {});
						break;
					case ConnectionState.Connected:
						Game.chrome.DrawLobby( world );
						break;
				}
			}

			var cursorName = Game.chrome.HitTest(mousePos) ? "default" : Game.controller.ChooseCursor( world );
			var c = new Cursor(cursorName);
			cursorRenderer.DrawSprite(c.GetSprite((int)cursorFrame), mousePos + Location - c.GetHotspot(), "cursor");
			cursorRenderer.Flush();

			renderer.EndFrame();
		}

		public void Tick()
		{
			cursorFrame += 0.5f;
		}

		IHandleInput dragRegion = null;
		public void DispatchMouseInput(World world, MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				mousePos = mi.Location;

			if (dragRegion != null) {
				dragRegion.HandleInput( world, mi );
				if (mi.Event == MouseInputEvent.Up) dragRegion = null;
				return;
			}

			dragRegion = regions.FirstOrDefault(r => r.HandleInput(world, mi));
			if (mi.Event != MouseInputEvent.Down)
				dragRegion = null;
		}

		public float2 ViewToWorld(MouseInput mi)
		{
			return (1 / 24.0f) * (new float2(mi.Location.X, mi.Location.Y) + Location);
		}

		public void Center(IEnumerable<Actor> actors)
		{
			if (!actors.Any()) return;

			var avgPos = (1f / actors.Count()) * actors
				.Select(a => a.CenterLocation)
				.Aggregate((a, b) => a + b);

			scrollPosition = (avgPos - .5f * new float2(Width, Height)).ToInt2();
		}

		public void GoToStartLocation( Player player )
		{
			Center( player.World.Queries.OwnedBy[ player ].WithTrait<Selectable>().Select( a => a.Actor ) );
		}
	}
}
