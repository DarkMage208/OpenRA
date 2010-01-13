﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Traits;

namespace OpenRa.Game.GameRules
{
	static class Footprint
	{
		public static IEnumerable<int2> Tiles( string name, BuildingInfo buildingInfo, int2 position )
		{
			return Tiles(name, buildingInfo, position, true);
		}

		public static IEnumerable<int2> Tiles( string name, BuildingInfo buildingInfo, int2 position, bool adjustForPlacement )
		{
			var dim = buildingInfo.Dimensions;

			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));
			if (buildingInfo.Bib)
			{
				dim.Y += 1;
				footprint = footprint.Concat(new char[dim.X]);
			}

			var adjustment = adjustForPlacement ? AdjustForBuildingSize(buildingInfo) : int2.Zero;

			var tiles = TilesWhere(name, dim, footprint.ToArray(), a => a != '_');
			return tiles.Select(t => t + position - adjustment);
		}

		public static IEnumerable<int2> Tiles(Actor a, Traits.Building building)
		{
			return Tiles( a.Info.Name, a.Info.Traits.Get<BuildingInfo>(), a.Location, false );
		}

		public static IEnumerable<int2> UnpathableTiles( string name, BuildingInfo buildingInfo, int2 position )
		{
			var footprint = buildingInfo.Footprint.Where( x => !char.IsWhiteSpace( x ) ).ToArray();
			foreach( var tile in TilesWhere( name, buildingInfo.Dimensions, footprint, a => a == 'x' ) )
				yield return tile + position;
		}

		static IEnumerable<int2> TilesWhere( string name, int2 dim, char[] footprint, Func<char, bool> cond )
		{
			if( footprint.Length != dim.X * dim.Y )
				throw new InvalidOperationException( "Invalid footprint for " + name );
			int index = 0;

			for( int y = 0 ; y < dim.Y ; y++ )
				for( int x = 0 ; x < dim.X ; x++ )
					if( cond( footprint[ index++ ] ) )
						yield return new int2( x, y );
		}

		public static int2 AdjustForBuildingSize( BuildingInfo buildingInfo )
		{
			var dim = buildingInfo.Dimensions;
			return new int2( dim.X / 2, dim.Y > 1 ? ( dim.Y + 1 ) / 2 : 0 );
		}
	}
}
