#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PaletteFromRGBAInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theatre = null;
		public readonly int R = 0;
		public readonly int G = 0;
		public readonly int B = 0;
		public readonly int A = 255;

		public object Create(ActorInitializer init) { return new PaletteFromRGBA(init.world, this); }
	}

	class PaletteFromRGBA
	{
		public PaletteFromRGBA(World world, PaletteFromRGBAInfo info)
		{
			if (info.Theatre == null ||
				info.Theatre.ToLowerInvariant() == world.Map.Theater.ToLowerInvariant())
			{
				// TODO: This shouldn't rely on a base palette
				var wr = world.WorldRenderer;
				var pal = wr.GetPalette("terrain");
				wr.AddPalette(info.Name, new Palette(pal, new SingleColorRemap(Color.FromArgb(info.A, info.R, info.G, info.B))));
			}
		}
	}
}
