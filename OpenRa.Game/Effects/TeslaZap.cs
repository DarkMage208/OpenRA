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

using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class TeslaZap : IEffect
	{
		readonly int2 from, to;
		readonly Sequence tesla;
		int timeUntilRemove = 2; // # of frames

		public TeslaZap( int2 from, int2 to )
		{
			this.from = from;
			this.to = to;
			this.tesla = SequenceProvider.GetSequence( "litning", "bright" );
		}

		public void Tick( World world )
		{
			if( timeUntilRemove <= 0 )
				world.AddFrameEndTask( w => w.Remove( this ) );
			--timeUntilRemove;
		}

		public IEnumerable<Renderable> Render()
		{
			if( from.X < to.X )
				return DrawZap( from, to, tesla );
			else if( from.X > to.X || from.Y > to.Y )
				return DrawZap( to, from, tesla );
			else
				return DrawZap( from, to, tesla );
		}

		static IEnumerable<Renderable> DrawZap( int2 from, int2 to, Sequence tesla )
		{
			int2 d = to - from;
			if( d.X < 8 )
			{
				var prev = new int2( 0, 0 );
				var y = d.Y;
				while( y >= prev.Y + 8 )
				{
					yield return new Renderable( tesla.GetSprite( 2 ), (float2)( from + prev - new int2( 0, 8 ) ), "effect");
					prev.Y += 8;
				}
			}
			else
			{
				var prev = new int2( 0, 0 );
				for( int i = 1 ; i < d.X ; i += 8 )
				{
					var y = i * d.Y / d.X;
					if( y <= prev.Y - 8 )
					{
						yield return new Renderable(tesla.GetSprite(3), (float2)(from + prev - new int2(8, 16)), "effect");
						prev.Y -= 8;
						while( y <= prev.Y - 8 )
						{
							yield return new Renderable(tesla.GetSprite(2), (float2)(from + prev - new int2(0, 16)), "effect");
							prev.Y -= 8;
						}
					}
					else if( y >= prev.Y + 8 )
					{
						yield return new Renderable(tesla.GetSprite(0), (float2)(from + prev - new int2(8, 8)), "effect");
						prev.Y += 8;
						while( y >= prev.Y + 8 )
						{
							yield return new Renderable(tesla.GetSprite(2), (float2)(from + prev - new int2(0, 8)), "effect");
							prev.Y += 8;
						}
					}
					else
						yield return new Renderable(tesla.GetSprite(1), (float2)(from + prev - new int2(8, 8)), "effect");

					prev.X += 8;
				}
			}
		}
	}
}
