﻿#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;

namespace OpenRA.FileFormats
{
	public class ShroudPaletteRemap : IPaletteRemap
	{
		bool isFog;

		public ShroudPaletteRemap(bool isFog) { this.isFog = isFog; }
		public Color GetRemappedColor(Color original, int index)
		{
			// false-color version for debug

			//return new[] { 
			//    Color.FromArgb(64,0,0,0), Color.Green, 
			//    Color.Blue, Color.Yellow, 
			//    Color.Green, 
			//    Color.Red, 
			//    Color.Purple, 
			//    Color.Cyan}[index % 8];

			if (isFog)
				return new[] { 
					Color.Transparent, Color.Green, 
					Color.Blue, Color.Yellow, 
					Color.FromArgb(128,0,0,0), 
					Color.FromArgb(128,0,0,0), 
					Color.FromArgb(128,0,0,0), 
					Color.FromArgb(64,0,0,0)}[index % 8];
			else
				return new[] { 
					Color.Transparent, Color.Green, 
					Color.Blue, Color.Yellow, 
					Color.Black, 
					Color.FromArgb(128,0,0,0), 
					Color.Transparent, 
					Color.Transparent}[index % 8];
		}
	}
}
