using System.Collections.Generic;
using System;
using System.Linq;

namespace OpenRa.Game.Graphics
{
	class Viewport
	{
		readonly float2 size;
		readonly float2 mapSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public float2 Location { get { return scrollPosition; } }
		public float2 Size { get { return size; } }

		public int Width { get { return (int)size.X; } }
		public int Height { get { return (int)size.Y; } }

		public Cursor cursor = Cursor.Move;
		SpriteRenderer cursorRenderer;
		int2 mousePos;
		float cursorFrame = 0f;

		public void Scroll(float2 delta)
		{
			scrollPosition = (scrollPosition + delta).Constrain(float2.Zero, mapSize);
		}

		public Viewport(float2 size, float2 mapSize, Renderer renderer)
		{
			this.size = size;
			this.mapSize = Game.CellSize * mapSize - size + new float2(128, 0);
			this.renderer = renderer;
			cursorRenderer = new SpriteRenderer(renderer, true);
		}

		List<Region> regions = new List<Region>();

		public void AddRegion(Region r) { regions.Add(r); }

		public void DrawRegions()
		{
			float2 r1 = new float2(2, -2) / Size;
			float2 r2 = new float2(-1, 1);
			
			renderer.BeginFrame(r1, r2, scrollPosition);

			foreach (Region region in regions)
				region.Draw(renderer);
			cursorFrame += 0.01f;
			cursorRenderer.DrawSprite(cursor.GetSprite((int)cursorFrame), mousePos + Location - cursor.GetHotspot(), 0);
			cursorRenderer.Flush();

			renderer.EndFrame();
		}

        Region dragRegion = null;
        public void DispatchMouseInput(MouseInput mi)
        {
			if (mi.Event == MouseInputEvent.Move)
				mousePos = mi.Location;

            if (dragRegion != null) {
                dragRegion.HandleMouseInput( mi );
                if (mi.Event == MouseInputEvent.Up) dragRegion = null;
                return;
            }

            dragRegion = regions.FirstOrDefault(r => r.Contains(mi.Location) && r.HandleMouseInput(mi));
            if (mi.Event != MouseInputEvent.Down)
                dragRegion = null;
        }

		public float2 ViewToWorld(MouseInput mi)
		{
			return (1 / 24.0f) * (new float2(mi.Location.X, mi.Location.Y) + Location);
		}
	}
}
