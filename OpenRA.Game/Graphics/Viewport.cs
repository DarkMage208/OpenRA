#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Support;
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
			cursorRenderer = renderer.SpriteRenderer;

			this.scrollPosition = Game.CellSize* mapStart;
		}
		
		public void DrawRegions( World world )
		{
			Timer.Time( "DrawRegions start" );

			world.WorldRenderer.palette.Update(
				world.WorldActor.traits.WithInterface<IPaletteModifier>());

			float2 r1 = new float2(2, -2) / screenSize;
			float2 r2 = new float2(-1, 1);

			renderer.BeginFrame(r1, r2, scrollPosition.ToInt2());
			world.WorldRenderer.Draw();
			Timer.Time( "worldRenderer: {0}" );

			Game.chrome.Draw(world);
			Timer.Time( "widgets: {0}" );

			var cursorName = Game.chrome.HitTest(mousePos) ? "default" : Game.controller.ChooseCursor( world );
			var c = new Cursor(cursorName);
			cursorRenderer.DrawSprite(c.GetSprite((int)cursorFrame), mousePos + Location - c.GetHotspot(), "cursor");
			Timer.Time( "cursors: {0}" );

			renderer.RgbaSpriteRenderer.Flush();
			renderer.SpriteRenderer.Flush();
			renderer.WorldSpriteRenderer.Flush();

			renderer.EndFrame();
			Timer.Time( "endFrame: {0}" );
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
		
		public void Center(int2 loc)
		{
			scrollPosition = (Game.CellSize*loc - .5f * new float2(Width, Height)).ToInt2();
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

		public Rectangle? ShroudBounds()
		{
			var localPlayer = Game.world.LocalPlayer;
			if (localPlayer == null) return null;
			if (localPlayer.Shroud.Disabled) return null;
			return localPlayer.Shroud.Bounds;
		}
	}
}
