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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class WorldRenderer
	{
		public readonly World world;
		internal readonly TerrainRenderer terrainRenderer;
		internal readonly ShroudRenderer shroudRenderer;

		public readonly UiOverlay uiOverlay;
		internal readonly HardwarePalette palette;

		internal WorldRenderer(World world)
		{
			this.world = world;
			this.palette = Game.modData.Palette;
			foreach( var pal in world.traitDict.ActorsWithTraitMultiple<IPalette>( world ) )
				pal.Trait.InitPalette( this );
			
			terrainRenderer = new TerrainRenderer(world, this);
			shroudRenderer = new ShroudRenderer(world);
			uiOverlay = new UiOverlay();
		}
		
		public int GetPaletteIndex(string name) { return palette.GetPaletteIndex(name); }
		public Palette GetPalette(string name) { return palette.GetPalette(name); }
		public void AddPalette(string name, Palette pal) { palette.AddPalette(name, pal); }
		
		class SpriteComparer : IComparer<Renderable>
		{
			public int Compare(Renderable x, Renderable y)
			{
				return (x.Z + x.ZOffset).CompareTo(y.Z + y.ZOffset);
			}
		}

		IEnumerable<Renderable> SpritesToRender()
		{
			var bounds = Game.viewport.ViewBounds(world);
			var comparer = new SpriteComparer();

			bounds.Offset((int)Game.viewport.Location.X, (int)Game.viewport.Location.Y);

			var actors = world.FindUnits(
				new float2(bounds.Left, bounds.Top),
				new float2(bounds.Right, bounds.Bottom));

			var renderables = actors.SelectMany(a => a.Render())
				.OrderBy(r => r, comparer);

			var effects = world.Effects.SelectMany(e => e.Render());

			return renderables.Concat(effects);
		}

		public void Draw()
		{
			RefreshPalette();
			var bounds = Game.viewport.ViewBounds(world);
			Game.Renderer.EnableScissor(bounds.Left, bounds.Top, bounds.Width, bounds.Height);

			terrainRenderer.Draw(this, Game.viewport);
			foreach (var a in world.traitDict.ActorsWithTraitMultiple<IRenderAsTerrain>(world))
				foreach (var r in a.Trait.RenderAsTerrain(a.Actor))
					r.Sprite.DrawAt(r.Pos, this.GetPaletteIndex(r.Palette), r.Scale);

			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPreRenderSelection>())
						t.RenderBeforeWorld(this, a);
			
			Game.Renderer.Flush();
			
			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderBeforeWorld(this, world);

            foreach (var image in SpritesToRender() )
                image.Sprite.DrawAt(image.Pos, this.GetPaletteIndex(image.Palette), image.Scale);
		    uiOverlay.Draw(this, world);

			// added for contrails
			foreach (var a in world.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPostRender>())
						t.RenderAfterWorld(this, a);

			if (world.OrderGenerator != null)
				world.OrderGenerator.RenderAfterWorld(this, world);

			shroudRenderer.Draw( this );
			Game.Renderer.DisableScissor();
			
			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPostRenderSelection>())
						t.RenderAfterWorld(this, a);

			Game.Renderer.Flush();
		}

		void DrawBox(RectangleF r, Color color)
		{
			var a = new float2(r.Left, r.Top);
			var b = new float2(r.Right - a.X, 0);
			var c = new float2(0, r.Bottom - a.Y);
			Game.Renderer.LineRenderer.DrawLine(a, a + b, color, color);
			Game.Renderer.LineRenderer.DrawLine(a + b, a + b + c, color, color);
			Game.Renderer.LineRenderer.DrawLine(a + b + c, a + c, color, color);
			Game.Renderer.LineRenderer.DrawLine(a, a + c, color, color);
		}

		public void DrawSelectionBox(Actor selectedUnit, Color c)
		{
			var bounds = selectedUnit.GetBounds(false);

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(0, 4), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(0, 4), c, c);

			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(0, -4), c, c);
				}

		public void DrawLocus(Color c, int2[] cells)
		{
			var dict = cells.ToDictionary(a => a, a => 0);
			foreach (var t in dict.Keys)
			{
				if (!dict.ContainsKey(t + new int2(-1, 0)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(0, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(1, 0)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * (t + new int2(1, 0)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, -1)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(1, 0)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, 1)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * (t + new int2(0, 1)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
			}
		}

		public void DrawRangeCircle(Color c, float2 location, float range)
		{
			for (var i = 0; i < 32; i++)
			{
				var start = location + Game.CellSize * range * float2.FromAngle((float)(Math.PI * i) / 16);
				var end = location + Game.CellSize * range * float2.FromAngle((float)(Math.PI * (i + 0.7)) / 16);
				
				Game.Renderer.LineRenderer.DrawLine(start, end, c, c);
			}
		}

		public void RefreshPalette()
		{
			palette.Update( world.WorldActor.TraitsImplementing<IPaletteModifier>() );
		}
	}
}
