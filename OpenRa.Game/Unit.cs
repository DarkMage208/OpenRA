using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	abstract class Unit : Actor, ISelectable
	{
		protected Animation animation;

		protected int facing = 0;
		protected int2 fromCell, toCell;
		protected int moveFraction, moveFractionTotal;

		protected delegate void TickFunc( World world, double t );
		protected TickFunc currentOrder = null;
		protected TickFunc nextOrder = null;

		protected readonly float2 renderOffset;

		public Unit( string name, int2 cell, int palette, float2 renderOffset )
		{
			fromCell = toCell = cell;
			this.renderOffset = renderOffset;
			this.palette = palette;

			animation = new Animation( name );
			animation.PlayFetchIndex( "idle", delegate { return facing; } );
		}

		static float2[] fvecs = Util.MakeArray<float2>( 32,
			delegate( int i ) { return -float2.FromAngle( i / 16.0f * (float)Math.PI ); } );

		int GetFacing( float2 d )
		{
			if( float2.WithinEpsilon( d, float2.Zero, 0.001f ) )
				return facing;

			int highest = -1;
			float highestDot = -1.0f;

			for( int i = 0 ; i < fvecs.Length ; i++ )
			{
				float dot = float2.Dot( fvecs[ i ], d );
				if( dot > highestDot )
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest;
		}

		const int Speed = 6;

		public override void Tick( World world, double t )
		{
			animation.Tick( t );
			if( currentOrder == null && nextOrder != null )
			{
				currentOrder = nextOrder;
				nextOrder = null;
			}

			if( currentOrder != null )
				currentOrder( world, t );
		}

		public void AcceptMoveOrder( int2 destination )
		{
			nextOrder = delegate( World world, double t )
			{
				int speed = (int)( t * ( Speed * 100 ) );

				if( nextOrder != null )
					destination = toCell;

				int desiredFacing = GetFacing( ( toCell - fromCell ).ToFloat2() );
				if( facing != desiredFacing )
					Turn( desiredFacing );
				else
				{
					moveFraction += speed;
					if( moveFraction >= moveFractionTotal )
					{
						moveFraction = 0;
						moveFractionTotal = 0;
						fromCell = toCell;

						if( toCell == destination )
							currentOrder = null;
						else
						{
							List<int2> res = PathFinder.Instance.FindUnitPath( world, this, destination );
							if( res.Count != 0 )
							{
								toCell = res[ res.Count - 1 ];

								int2 dir = toCell - fromCell;
								moveFractionTotal = ( dir.X != 0 && dir.Y != 0 ) ? 250 : 200;
							}
							else
								destination = toCell;
						}
					}
				}
			};
		}

		protected void Turn( int desiredFacing )
		{
			int df = ( desiredFacing - facing + 32 ) % 32;
			facing = ( facing + ( df > 16 ? 31 : 1 ) ) % 32;
		}

		public virtual IOrder Order( int2 xy )
		{
			return new MoveOrder( this, xy );
		}

		public int2 Location
		{
			get { return toCell; }
		}

		public override float2 RenderLocation
		{
			get
			{
				float fraction = (moveFraction > 0) ? (float)moveFraction / moveFractionTotal : 0f;

				float2 location = 24 * float2.Lerp( fromCell.ToFloat2(), toCell.ToFloat2(), fraction );
				return ( location - renderOffset ).Round(); ;
			}
		}

		public override Sprite[] CurrentImages
		{
			get { return animation.Images; }
		}
	}
}
