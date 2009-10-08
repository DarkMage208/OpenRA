﻿using System.Drawing;
using System.Windows.Forms;

namespace OpenRa.Game.Graphics
{
	class WorldRenderer
	{
		public readonly SpriteRenderer spriteRenderer;
        public readonly LineRenderer lineRenderer;
		public readonly World world;
		public readonly Region region;
		public readonly UiOverlay uiOverlay;

		public WorldRenderer(Renderer renderer, World world)
		{
			// TODO: this is layout policy. it belongs at a higher level than this.

			region = Region.Create(world.game.viewport, DockStyle.Left,
				world.game.viewport.Width - 128, Draw, 
                world.game.controller.HandleMouseInput);		

			world.game.viewport.AddRegion(region);

			spriteRenderer = new SpriteRenderer(renderer, true);
            lineRenderer = new LineRenderer(renderer);
			uiOverlay = new UiOverlay(spriteRenderer, world.game);
			this.world = world;
		}

		public void Draw()
		{
			var rect = new RectangleF((region.Position + world.game.viewport.Location).ToPointF(), 
                region.Size.ToSizeF());

			foreach (Actor a in world.Actors)
			{
				var images = a.CurrentImages;

				foreach( var image in images )
				{
					var loc = image.Second;

					if( loc.X > rect.Right || loc.X < rect.Left - image.First.bounds.Width )
						continue;
					if( loc.Y > rect.Bottom || loc.Y < rect.Top - image.First.bounds.Height )
						continue;

					spriteRenderer.DrawSprite( image.First, loc, ( a.owner != null ) ? a.owner.Palette : 0 );
				}
			}

            uiOverlay.Draw();

			spriteRenderer.Flush();

            var selectedUnit = world.game.controller.orderGenerator as Unit;
            if (selectedUnit != null)
            {
                var center = selectedUnit.CenterLocation;
                var size = selectedUnit.SelectedSize;

                var xy = center - 0.5f * size;
                var XY = center + 0.5f * size;
                var Xy = new float2( XY.X, xy.Y );
                var xY = new float2( xy.X, XY.Y );

                lineRenderer.DrawLine(xy, xy + new float2(4, 0), Color.White, Color.White);
                lineRenderer.DrawLine(xy, xy + new float2(0, 4), Color.White, Color.White);
                lineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), Color.White, Color.White);
                lineRenderer.DrawLine(Xy, Xy + new float2(0, 4), Color.White, Color.White);

                lineRenderer.DrawLine(xY, xY + new float2(4, 0), Color.White, Color.White);
                lineRenderer.DrawLine(xY, xY + new float2(0, -4), Color.White, Color.White);
                lineRenderer.DrawLine(XY, XY + new float2(-4, 0), Color.White, Color.White);
                lineRenderer.DrawLine(XY, XY + new float2(0, -4), Color.White, Color.White);
            }
            
            lineRenderer.Flush();
		}
	}
}
