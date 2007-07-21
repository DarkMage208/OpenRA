using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Harvester : Unit
	{
		public Harvester( int2 cell, int palette )
			: base( "harv", cell, palette, new float2( 12, 12 ) )
		{
		}

		public override IOrder Order( int2 xy )
		{
			if( ( fromCell == toCell || moveFraction == 0 ) && fromCell == xy )
				return new HarvestOrder( this );
			return base.Order( xy );
		}

		void AcceptHarvestOrder()
		{
			TickFunc order = null;
			order = nextOrder = delegate
			{
				// TODO: check that there's actually some ore in this cell :)

				// face in one of the 8 directions
				int desiredFacing = ( facing + 1 ) & 28;
				if( facing != desiredFacing )
				{
					Turn( desiredFacing );
					return;
				}

				currentOrder = delegate { };
				if( nextOrder == null )
					nextOrder = order;

				string sequenceName = string.Format( "harvest{0}", facing / 4 );
				animation.PlayThen( sequenceName, delegate
				{
					currentOrder = null;
					animation.PlayFetchIndex( "idle", delegate { return facing; } );
				} );
			};
		}

		public class HarvestOrder : IOrder
		{
			Harvester harvester;

			public HarvestOrder( Harvester harv )
			{
				harvester = harv;
			}

			public void Apply()
			{
				harvester.AcceptHarvestOrder();
			}
		}
	}
}
