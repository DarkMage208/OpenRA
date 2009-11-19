﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;
using System.Diagnostics;

namespace OpenRa.Game.Traits.Activities
{
	class Move : IActivity
	{
		public IActivity NextActivity { get; set; }

		int2? destination;
		int nearEnough;
		public List<int2> path;
		Func<Actor, Mobile, List<int2>> getPath;

		MovePart move;

		public Move( int2 destination, int nearEnough )
		{
			this.getPath = ( self, mobile ) => Game.PathFinder.FindUnitPath(
				self.Location, destination,
				mobile.GetMovementType() );
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move( Actor target, int range )
		{
			this.getPath = ( self, mobile ) => Game.PathFinder.FindUnitPathToRange(
				self.Location, target.Location,
				mobile.GetMovementType(), range );
			this.destination = null;
			this.nearEnough = range;
		}

		public Move(Func<List<int2>> getPath)
		{
			this.getPath = (_, _2) => getPath();
			this.destination = null;
			this.nearEnough = 0;
		}

		static bool CanEnterCell( int2 c, Actor self )
		{
			if (!Game.BuildingInfluence.CanMoveHere(c)) return false;
			var u = Game.UnitInfluence.GetUnitAt( c );
			return (u == null || u == self);
		}

		public IActivity Tick( Actor self, Mobile mobile )
		{
			if( move != null )
			{
				move.TickMove( self, mobile, this );
				return null;
			}

			if( destination == self.Location )
				return NextActivity;

			if( path == null )
			{
				path = getPath( self, mobile ).TakeWhile( a => a != self.Location ).ToList();
				SanityCheckPath( mobile );
			}

			if( path.Count == 0 )
			{
				destination = mobile.toCell;
				return null;
			}

			destination = path[ 0 ];

			var nextCell = PopPath( self, mobile );
			if( nextCell == null )
				return null;

			int2 dir = nextCell.Value - mobile.fromCell;
			var firstFacing = Util.GetFacing( dir, mobile.facing );
			if( firstFacing != mobile.facing )
			{
				path.Add( nextCell.Value );

				return new Turn( firstFacing ) { NextActivity = this };
			}
			else
			{
				mobile.toCell = nextCell.Value;
				move = new MoveFirstHalf(
					CenterOfCell( mobile.fromCell ),
					BetweenCells( mobile.fromCell, mobile.toCell ),
					mobile.facing,
					mobile.facing,
					0 );

				Game.UnitInfluence.Update( mobile );

				move.TickMove( self, mobile, this );

				return null;
			}
		}

		[Conditional( "SANITY_CHECKS")]
		void SanityCheckPath( Mobile mobile )
		{
			if( path.Count == 0 )
				return;
			var d = path[path.Count-1] - mobile.toCell;
			if( d.LengthSquared > 2 )
				throw new InvalidOperationException( "(Move) Sanity check failed" );
		}

		int2? PopPath( Actor self, Mobile mobile )
		{
			if( path.Count == 0 ) return null;
			var nextCell = path[ path.Count - 1 ];
			if( !CanEnterCell( nextCell, self ) )
			{
				if( ( mobile.toCell - destination.Value ).LengthSquared <= nearEnough )
				{
					path.Clear();
					return null;
				}

				Game.UnitInfluence.Remove( mobile );
				var newPath = getPath(self, mobile).TakeWhile(a => a != self.Location).ToList();

				Game.UnitInfluence.Add( mobile );
				if (newPath.Count != 0)
					path = newPath;

				return null;
			}
			path.RemoveAt( path.Count - 1 );
			return nextCell;
		}

		static float2 CenterOfCell( int2 loc )
		{
			return new float2( 12, 12 ) + Game.CellSize * (float2)loc;
		}

		static float2 BetweenCells( int2 from, int2 to )
		{
			return 0.5f * ( CenterOfCell( from ) + CenterOfCell( to ) );
		}

		abstract class MovePart
		{
			public readonly float2 from, to;
			public readonly int fromFacing, toFacing;
			public int moveFraction;
			public readonly int moveFractionTotal;

			public MovePart( float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
			{
				this.from = from;
				this.to = to;
				this.fromFacing = fromFacing;
				this.toFacing = toFacing;
				this.moveFraction = startingFraction;
				this.moveFractionTotal = (int)( to - from ).Length * ( 25 / 6 );
			}

			public void TickMove( Actor self, Mobile mobile, Move parent )
			{
				var oldFraction = moveFraction;
				var oldTotal = moveFractionTotal;

				moveFraction += ( self.unitInfo as UnitInfo.MobileInfo ).Speed;
				UpdateCenterLocation( self, mobile );
				if( moveFraction >= moveFractionTotal )
				{
					parent.move = OnComplete( self, mobile, parent );
					if( parent.move == null )
						UpdateCenterLocation( self, mobile );
				}
			}

			void UpdateCenterLocation( Actor self, Mobile mobile )
			{
				var frac = (float)moveFraction / moveFractionTotal;

				self.CenterLocation = float2.Lerp( from, to, frac );
				if( moveFraction >= moveFractionTotal )
					mobile.facing = toFacing & 0xFF;
				else
					mobile.facing = ( fromFacing + ( toFacing - fromFacing ) * moveFraction / moveFractionTotal ) & 0xFF;
			}

			protected abstract MovePart OnComplete( Actor self, Mobile mobile, Move parent );
		}

		class MoveFirstHalf : MovePart
		{
			public MoveFirstHalf( float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
				: base( from, to, fromFacing, toFacing, startingFraction )
			{
			}

			protected override MovePart OnComplete( Actor self, Mobile mobile, Move parent )
			{
				var nextCell = parent.PopPath( self, mobile );
				if( nextCell != null )
				{
					if( ( nextCell - mobile.toCell ) != ( mobile.toCell - mobile.fromCell ) )
					{
						var ret = new MoveFirstHalf(
							BetweenCells( mobile.fromCell, mobile.toCell ),
							BetweenCells( mobile.toCell, nextCell.Value ),
							mobile.facing,
							Util.GetNearestFacing( mobile.facing, Util.GetFacing( nextCell.Value - mobile.toCell, mobile.facing ) ),
							moveFraction - moveFractionTotal );
						mobile.fromCell = mobile.toCell;
						mobile.toCell = nextCell.Value;
						Game.UnitInfluence.Update( mobile );
						return ret;
					}
					else
						parent.path.Add( nextCell.Value );
				}
				var ret2 = new MoveSecondHalf(
					BetweenCells( mobile.fromCell, mobile.toCell ),
					CenterOfCell( mobile.toCell ),
					mobile.facing,
					mobile.facing,
					moveFraction - moveFractionTotal );
				mobile.fromCell = mobile.toCell;
				Game.UnitInfluence.Update( mobile );
				return ret2;
			}
		}

		class MoveSecondHalf : MovePart
		{
			public MoveSecondHalf( float2 from, float2 to, int fromFacing, int toFacing, int startingFraction )
				: base( from, to, fromFacing, toFacing, startingFraction )
			{
			}

			protected override MovePart OnComplete( Actor self, Mobile mobile, Move parent )
			{
				self.CenterLocation = CenterOfCell( mobile.toCell );
				mobile.fromCell = mobile.toCell;
				return null;
			}
		}

		public void Cancel( Actor self, Mobile mobile )
		{
			path = new List<int2>();
			NextActivity = null;
		}
	}
}
