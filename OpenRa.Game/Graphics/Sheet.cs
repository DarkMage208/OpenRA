using System.Drawing;
using System.IO;
using Ijw.DirectX;
using OpenRa.FileFormats;
using System.Drawing.Imaging;

namespace OpenRa.Game.Graphics
{
	class Sheet
	{
		readonly Renderer renderer;
		protected readonly Bitmap bitmap;

		Texture texture;
		static int suffix = 0;

		public Sheet(Renderer renderer, Size size)
		{
			this.renderer = renderer;
			this.bitmap = new Bitmap(size.Width, size.Height);
		}

		public Sheet(Renderer renderer, string filename)
		{
			this.renderer = renderer;
			this.bitmap = (Bitmap)Image.FromStream(FileSystem.Open(filename));
		}

		void Resolve()
		{
			texture = Texture.CreateFromBitmap(bitmap, renderer.Device);
		}

		public Texture Texture
		{
			get
			{
				if (texture == null)
					Resolve();

				return texture;
			}
		}

		public Size Size { get { return bitmap.Size; } }

		public Color this[Point p]
		{
			get { return bitmap.GetPixel(p.X, p.Y); }
			set { bitmap.SetPixel(p.X, p.Y, value); }
		}

		public Bitmap Bitmap { get { return bitmap; } }	// for perf
	}
}
