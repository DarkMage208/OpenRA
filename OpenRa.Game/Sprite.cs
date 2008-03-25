using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	class Sprite
	{
		public readonly Rectangle bounds;
		public readonly Sheet sheet;
		public readonly TextureChannel channel;
		public readonly RectangleF uv;
		public readonly float2 size;

        readonly float2[] uvhax;

		internal Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel)
		{
			this.bounds = bounds;
			this.sheet = sheet;
			this.channel = channel;

			uv = new RectangleF(
					(float)(bounds.Left + 0.5f) / sheet.Size.Width,
					(float)(bounds.Top + 0.5f) / sheet.Size.Height,
					(float)(bounds.Width) / sheet.Size.Width,
					(float)(bounds.Height) / sheet.Size.Height);

            uvhax = new float2[]
            {
                MapTextureCoords( new float2(0,0) ),
                MapTextureCoords( new float2(1,0) ),
                MapTextureCoords( new float2(0,1) ),
                MapTextureCoords( new float2(1,1) ),
            };

			this.size = new float2(bounds.Size);
		}

		public float2 MapTextureCoords(float2 p)
		{
			return new float2(
				p.X > 0 ? uv.Right : uv.Left,
				p.Y > 0 ? uv.Bottom : uv.Top);
		}

        public float2 FastMapTextureCoords(int k)
        {
            return uvhax[k];
        }
    }

	public enum TextureChannel
	{
		Red = 0,
		Green = 1,
		Blue = 2,
		Alpha = 3,
	}
}
