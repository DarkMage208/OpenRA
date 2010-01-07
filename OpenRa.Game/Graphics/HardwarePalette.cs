using System.Drawing;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	public enum PaletteType
	{
		Gold, Blue, Red, Orange, Teal, Salmon, Green, Gray, 
		Shadow, Invuln, Chrome, Shroud, Highlight,
	};

	class HardwarePalette : Sheet
	{
		const int maxEntries = 16;
		int allocated = 0;
		
		public HardwarePalette(Renderer renderer, Map map)
			: base(renderer,new Size(256, maxEntries))
		{
			Palette pal = new Palette(FileSystem.Open(map.Theater + ".pal"));
			AddPalette(pal);

			foreach (string remap in new string[] { "blue", "red", "orange", "teal", "salmon", "green", "gray" })
				AddPalette(new Palette(pal, new PlayerColorRemap(FileSystem.Open(remap + ".rem"))));

			AddPalette(new Palette(pal, new SingleColorRemap(Color.FromArgb(140, 0, 0, 0))));
			AddPalette(pal);	// iron curtain. todo: remap!
			AddPalette(pal);	// chrome (it's like gold, but we're not going to hax it in palettemods)
			AddPalette(new Palette(pal, new ShroudPaletteRemap()));
			AddPalette(new Palette(pal, new SingleColorRemap(Color.FromArgb(128, 255, 255, 255))));
		}

		int AddPalette(Palette p)
		{
			for (int i = 0; i < 256; i++)
				this[new Point(i, allocated)] = p.GetColor(i);

			return allocated++;
		}
	}
}
