#region Copyright & License Information
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

using OpenRA.Graphics;

namespace OpenRA
{
	public enum UnitMovementType : byte
	{
		Foot = 0,
		Track = 1,
		Wheel = 2,
		Float = 3,
		Fly = 4,
	}

	enum TerrainMovementType : byte
	{
		Clear = 0,
		Water = 1,
		Road = 2,
		Rock = 3,
		//Tree = 4,
		River = 5,
		Rough = 6,
		Wall = 7,
		Beach = 8,
		Ore = 9,
		Special = 10,
	}

	static class TerrainCosts
	{
		static float[][] costs = Util.MakeArray<float[]>(4,
			a => Util.MakeArray<float>(11, b => float.PositiveInfinity));

		static TerrainCosts()
		{
			for( int i = 0 ; i < 11 ; i++ )
			{
				if( i == 4 ) continue;
				var section = Rules.AllRules.GetSection( ( (TerrainMovementType)i ).ToString() );
				for( int j = 0 ; j < 4 ; j++ )
				{
					string val = section.GetValue( ( (UnitMovementType)j ).ToString(), "0%" );
					costs[j][i] = 100f / float.Parse(val.Substring(0, val.Length - 1));
				}
			}
		}

		public static float Cost( UnitMovementType unitMovementType, int r )
		{
			return costs[ (byte)unitMovementType ][ r ];
		}
	}
}
