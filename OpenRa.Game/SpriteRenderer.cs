using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class SpriteRenderer
	{
		FvfVertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;
		Renderer renderer;

		const int spritesPerBatch = 1024;

		List<Vertex> vertices = new List<Vertex>();
		List<ushort> indicies = new List<ushort>();
		Sheet currentSheet = null;
		int sprites = 0;

		public SpriteRenderer(Renderer renderer)
		{
			this.renderer = renderer;

			vertexBuffer = new FvfVertexBuffer<Vertex>(renderer.Device, 4 * spritesPerBatch, Vertex.Format);
			indexBuffer = new IndexBuffer(renderer.Device, 6 * spritesPerBatch);
		}

		public void Flush()
		{
			if (sprites > 0)
			{
				renderer.DrawWithShader(ShaderQuality.Low, delegate
				{
					vertexBuffer.SetData(vertices.ToArray());
					indexBuffer.SetData(indicies.ToArray());
					renderer.DrawBatch(vertexBuffer, indexBuffer,
						new Range<int>(0, vertices.Count),
						new Range<int>(0, indicies.Count),
						currentSheet.Texture);
				});

				vertices = new List<Vertex>();
				indicies = new List<ushort>();
				currentSheet = null;
				sprites = 0;
			}
		}

		public void DrawSprite(Sprite s, PointF location)
		{
			if (s.sheet != currentSheet)
				Flush();

			currentSheet = s.sheet;
			Util.CreateQuad(vertices, indicies, location, s, 0);

			if (++sprites >= spritesPerBatch)
				Flush();
		}
	}
}
