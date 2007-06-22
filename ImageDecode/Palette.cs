using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace ImageDecode
{
	public class Palette
	{
		List<Color> colors = new List<Color>();

		public Color GetColor(int index)
		{
			return colors[index];
		}

		public Palette(Stream s)
		{
			using (BinaryReader reader = new BinaryReader(s))
			{
				for (int i = 0; i < 256; i++)
				{
					byte r = (byte)(reader.ReadByte() << 2);
					byte g = (byte)(reader.ReadByte() << 2);
					byte b = (byte)(reader.ReadByte() << 2);

					colors.Add(Color.FromArgb(r, g, b));
				}
			}
		}
	}
}
