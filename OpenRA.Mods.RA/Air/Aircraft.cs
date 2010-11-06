﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using System;

namespace OpenRA.Mods.RA.Air
{
	public class AircraftInfo : ITraitInfo
	{
		public readonly int CruiseAltitude = 30;
		[ActorReference]
		public readonly string[] RepairBuildings = { "fix" };
		[ActorReference]
		public readonly string[] RearmBuildings = { "hpad", "afld" };
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;

		public virtual object Create( ActorInitializer init ) { return new Aircraft( init , this ); }
	}

	public class Aircraft : IMove, IFacing, IOccupySpace
	{
		protected readonly Actor self;
		[Sync]
		public int Facing { get; set; }
		[Sync]
		public int Altitude { get; set; }
		[Sync]
		public int2 SubPxPosition;
		public int2 PxPosition { get { return new int2( SubPxPosition.X / 1024, SubPxPosition.Y / 1024 ); } }
		public int2 TopLeft { get { return Util.CellContaining( PxPosition ); } }

		readonly AircraftInfo Info;

		public Aircraft( ActorInitializer init , AircraftInfo info)
		{
			this.self = init.self;
			if( init.Contains<LocationInit>() )
				this.SubPxPosition = 1024 * Util.CenterOfCell( init.Get<LocationInit, int2>() );
			
			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : info.InitialFacing;
			this.Altitude = init.Contains<AltitudeInit>() ? init.Get<AltitudeInit,int>() : 0;
			Info = info;
		}

		public int ROT { get { return Info.ROT; } }
		
		public int InitialFacing { get { return Info.InitialFacing; } }

		public void SetPosition(Actor self, int2 cell)
		{
			SetPxPosition( self, Util.CenterOfCell( cell ) );
		}

		public void SetPxPosition( Actor self, int2 px )
		{
			SubPxPosition = px * 1024;
		}

		public bool AircraftCanEnter(Actor a)
		{
			if( self.Owner != a.Owner ) return false;
			return Info.RearmBuildings.Contains( a.Info.Name )
				|| Info.RepairBuildings.Contains( a.Info.Name );
		}

		public bool CanEnterCell(int2 location) { return true; }

		public int MovementSpeed
		{
			get
			{
				decimal ret = Info.Speed;
				foreach( var t in self.TraitsImplementing<ISpeedModifier>() )
					ret *= t.GetSpeedModifier();
				return (int)ret;
			}
		}
		
		int2[] noCells = new int2[] { };
		public IEnumerable<int2> OccupiedCells() { return noCells; }

		public void TickMove( int speed, int facing )
		{
			var rawspeed = speed * 7 / (32 * 1024);
			SubPxPosition += rawspeed * -SubPxVector( facing );
		}

		int2 SubPxVector( int facing )
		{
			var angle = facing * Math.PI / 128.0;
			return new int2( (int)Math.Truncate( 1024 * Math.Sin( angle ) ), (int)Math.Truncate( 1024 * Math.Cos( angle ) ) );
		}
	}
}
