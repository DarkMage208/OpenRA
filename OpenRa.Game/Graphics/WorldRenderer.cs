using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IjwFramework.Types;
using System.Collections.Generic;

namespace OpenRa.Game.Graphics
{
	class WorldRenderer
	{
		public readonly SpriteRenderer spriteRenderer;
        public readonly LineRenderer lineRenderer;
		public readonly Game game;
		public readonly Region region;
		public readonly UiOverlay uiOverlay;

		public WorldRenderer(Renderer renderer, Game game)
		{
			// TODO: this is layout policy. it belongs at a higher level than this.
			this.game = game;
			region = Region.Create(game.viewport, DockStyle.Left,
				game.viewport.Width - 128, Draw, 
                game.controller.HandleMouseInput);		

			game.viewport.AddRegion(region);

			spriteRenderer = new SpriteRenderer(renderer, true);
            lineRenderer = new LineRenderer(renderer);
			uiOverlay = new UiOverlay(spriteRenderer, game);
		}

		void DrawSpriteList(Player owner, RectangleF rect, 
			IEnumerable<Pair<Sprite, float2>> images)
		{
			foreach (var image in images)
			{
				var loc = image.Second;

				if (loc.X > rect.Right || loc.X < rect.Left 
					- image.First.bounds.Width)
					continue;
				if (loc.Y > rect.Bottom || loc.Y < rect.Top 
					- image.First.bounds.Height)
					continue;

				spriteRenderer.DrawSprite(image.First, loc, 
					(owner != null) ? owner.Palette : 0);
			}
		}

		public void Draw()
		{
			var rect = new RectangleF((region.Position + game.viewport.Location).ToPointF(), 
                region.Size.ToSizeF());

			foreach (Actor a in game.world.Actors)
				DrawSpriteList(a.Owner, rect, a.Render());

			foreach (IEffect e in game.world.Effects)
				DrawSpriteList(e.Owner, rect, e.Render());

            uiOverlay.Draw();

			spriteRenderer.Flush();

            var selbox = game.controller.SelectionBox;
            if (selbox != null)
            {
                var a = selbox.Value.First;
                var b = new float2(selbox.Value.Second.X - a.X, 0);
                var c = new float2(0, selbox.Value.Second.Y - a.Y);

                lineRenderer.DrawLine(a, a + b, Color.White, Color.White);
                lineRenderer.DrawLine(a + b, a + b + c, Color.White, Color.White);
                lineRenderer.DrawLine(a + b + c, a + c, Color.White, Color.White);
                lineRenderer.DrawLine(a, a + c, Color.White, Color.White);

                foreach (var u in game.SelectUnitsInBox(selbox.Value.First, selbox.Value.Second))
                    DrawSelectionBox(u, Color.Yellow, false);
            }

            var selection = game.controller.orderGenerator as UnitOrderGenerator;
            if (selection != null)
				foreach( var a in game.world.Actors.Intersect(selection.selection) )		/* make sure we don't grab actors that are dead */
	                DrawSelectionBox(a, Color.White, true);
            
            lineRenderer.Flush();
		}

		const float conditionYellow = 0.5f;		/* todo: get these from gamerules */
		const float conditionRed = 0.25f;

        void DrawSelectionBox(Actor selectedUnit, Color c, bool drawHealthBar)
        {
            var center = selectedUnit.CenterLocation;
            var size = selectedUnit.SelectedSize;

            var xy = center - 0.5f * size;
            var XY = center + 0.5f * size;
            var Xy = new float2(XY.X, xy.Y);
            var xY = new float2(xy.X, XY.Y);

            lineRenderer.DrawLine(xy, xy + new float2(4, 0), c, c);
            lineRenderer.DrawLine(xy, xy + new float2(0, 4), c, c);
            lineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
            lineRenderer.DrawLine(Xy, Xy + new float2(0, 4), c, c);

            lineRenderer.DrawLine(xY, xY + new float2(4, 0), c, c);
            lineRenderer.DrawLine(xY, xY + new float2(0, -4), c, c);
            lineRenderer.DrawLine(XY, XY + new float2(-4, 0), c, c);
            lineRenderer.DrawLine(XY, XY + new float2(0, -4), c, c);

			if (drawHealthBar)
			{
				c = Color.Gray;
				lineRenderer.DrawLine(xy + new float2(0, -2), xy + new float2(0, -4), c, c);
				lineRenderer.DrawLine(Xy + new float2(0, -2), Xy + new float2(0, -4), c, c);

				var healthAmount = 0.6f;
				var healthColor = (healthAmount < conditionRed) ? Color.Red
					: (healthAmount < conditionYellow) ? Color.Yellow
					: Color.LimeGreen;

				var healthColor2 = Color.FromArgb(
					255,
					healthColor.R / 2,
					healthColor.G / 2,
					healthColor.B / 2);

				var z = float2.Lerp(xy, Xy, healthAmount);

				lineRenderer.DrawLine(z + new float2(0, -4), Xy + new float2(0,-4), c, c);
				lineRenderer.DrawLine(z + new float2(0, -2), Xy + new float2(0, -2), c, c);

				lineRenderer.DrawLine(xy + new float2(0, -3), 
					z + new float2(0, -3), 
					healthColor, healthColor);

				lineRenderer.DrawLine(xy + new float2(0, -2),
					z + new float2(0, -2),
					healthColor2, healthColor2);

				lineRenderer.DrawLine(xy + new float2(0, -4),
					z + new float2(0, -4),
					healthColor2, healthColor2);
			}
        }
	}
}
