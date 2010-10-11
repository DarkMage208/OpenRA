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

namespace OpenRA.Mods.RA
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
		public int2 Location;
		[Sync]
		public int Facing { get; set; }
		[Sync]
		public int Altitude { get; set; }

		public float2 center;

		AircraftInfo Info;

		public Aircraft( ActorInitializer init , AircraftInfo info)
		{
			this.self = init.self;
			if( init.Contains<LocationInit>() )
			{
				this.Location = init.Get<LocationInit, int2>();
				this.center = Util.CenterOfCell( Location );
			}
			
			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : info.InitialFacing;
			this.Altitude = init.Contains<AltitudeInit>() ? init.Get<AltitudeInit,int>() : 0;
			Info = info;
		}

		public int2 TopLeft
		{
			get { return Location; }
		}
		
		public int ROT { get { return Info.ROT; } }
		
		public int InitialFacing { get { return Info.InitialFacing; } }

		public void SetPosition(Actor self, int2 cell)
		{
			Location = cell;
			center = Util.CenterOfCell(cell);
		}

		public void SetPxPosition( Actor self, int2 px )
		{
			Location = Util.CellContaining( px );
			center = px;
		}

		public bool AircraftCanEnter(Actor a)
		{
			if( self.Owner != a.Owner ) return false;
			return Info.RearmBuildings.Contains( a.Info.Name )
				|| Info.RepairBuildings.Contains( a.Info.Name );
		}

		public bool CanEnterCell(int2 location) { return true; }
		
		public float MovementSpeedForCell(Actor self, int2 cell)
		{		
			var modifier = self
				.TraitsImplementing<ISpeedModifier>()
				.Select(t => t.GetSpeedModifier())
				.Product();
			return Info.Speed * modifier;
		}
		
		int2[] noCells = new int2[] { };
		public IEnumerable<int2> OccupiedCells() { return noCells; }
		public int2 PxPosition { get { return center.ToInt2(); } }
	}
}
