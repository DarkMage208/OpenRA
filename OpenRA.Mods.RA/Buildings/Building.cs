#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Buildings
{
	public class BuildingInfo : ITraitInfo
	{
		public readonly int Power = 0;
		public readonly bool BaseNormal = true;
		public readonly bool WaterBound = false;
		public readonly int Adjacent = 2;
		public readonly string Footprint = "x";
		public readonly int2 Dimensions = new int2(1, 1);
		
		public readonly string[] BuildSounds = {"placbldg.aud", "build5.aud"};
		public readonly string[] SellSounds = {"cashturn.aud"};


		public object Create(ActorInitializer init) { return new Building(init, this); }

		public bool IsCloseEnoughToBase(World world, Player p, string buildingName, int2 topLeft)
		{
            if (p.PlayerActor.Trait<DeveloperMode>().BuildAnywhere)
                return true;

			var buildingMaxBounds = Dimensions;
			if( Rules.Info[ buildingName ].Traits.Contains<BibInfo>() )
				buildingMaxBounds.Y += 1;

			var scanStart = world.ClampToWorld( topLeft - new int2( Adjacent, Adjacent ) );
			var scanEnd = world.ClampToWorld( topLeft + buildingMaxBounds + new int2( Adjacent, Adjacent ) );

			var nearnessCandidates = new List<int2>();

			for( int y = scanStart.Y ; y < scanEnd.Y ; y++ )
			{
				for( int x = scanStart.X ; x < scanEnd.X ; x++ )
				{
					var at = world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt( new int2( x, y ) );
					if( at != null && at.Owner.Stances[ p ] == Stance.Ally && at.Info.Traits.Get<BuildingInfo>().BaseNormal )
						nearnessCandidates.Add( new int2( x, y ) );
				}
			}
			var buildingTiles = FootprintUtils.Tiles( buildingName, this, topLeft ).ToList();
			return nearnessCandidates
				.Any( a => buildingTiles
					.Any( b => Math.Abs( a.X - b.X ) <= Adjacent
							&& Math.Abs( a.Y - b.Y ) <= Adjacent ) );
		}
	}

	public class Building : INotifyDamage, IOccupySpace, INotifyCapture, ISync, ITechTreePrerequisite
	{
		readonly Actor self;
		public readonly BuildingInfo Info;
		[Sync]
		readonly int2 topLeft;

		PowerManager PlayerPower;

		public int2 PxPosition { get { return ( 2 * topLeft + Info.Dimensions ) * Game.CellSize / 2; } }
		
		public IEnumerable<string> ProvidesPrerequisites { get { yield return self.Info.Name; } }
		
		public Building(ActorInitializer init, BuildingInfo info)
		{
			this.self = init.self;
			this.topLeft = init.Get<LocationInit,int2>();
			this.Info = info;
			this.PlayerPower = init.self.Owner.PlayerActor.Trait<PowerManager>();
		}
		
		public int GetPowerUsage()
		{
			if (Info.Power <= 0)
				return Info.Power;
			
			var health = self.TraitOrDefault<Health>();
			return health != null ? (Info.Power * health.HP / health.MaxHP) : Info.Power;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// Power plants lose power with damage
			if (Info.Power > 0)
				PlayerPower.UpdateActor(self, GetPowerUsage());
		}

		public int2 TopLeft
		{
			get { return topLeft; }
		}

		public IEnumerable<Pair<int2, SubCell>> OccupiedCells()
		{
			return FootprintUtils.UnpathableTiles( self.Info.Name, Info, TopLeft ).Select(c => Pair.New(c, SubCell.FullCell));
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			PlayerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
