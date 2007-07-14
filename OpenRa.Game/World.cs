using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class World
	{
		List<Actor> actors = new List<Actor>();
		SpriteRenderer spriteRenderer;

		public World(Renderer renderer)
		{
			spriteRenderer = new SpriteRenderer(renderer, true);
		}

		public void Add(Actor a) { actors.Add(a); }

		public void Draw(Renderer renderer, Range<float> xr, Range<float> yr)
		{
			foreach (Actor a in actors)
			{
				Sprite[] images = a.CurrentImages;

				if (images == null)
					continue;

				if (a.location.X > xr.End || a.location.X < xr.Start - images[0].size.Width)
					continue;

				if (a.location.Y > yr.End || a.location.Y < yr.Start - images[0].size.Height)
					continue;

				foreach (Sprite image in images)
					spriteRenderer.DrawSprite(image, a.location, a.palette);
			}

			spriteRenderer.Flush();
		}
	}
}
