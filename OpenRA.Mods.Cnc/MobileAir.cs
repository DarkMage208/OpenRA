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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Traits
{
	public class MobileAirInfo : MobileInfo
	{
		public readonly int CruiseAltitude = 20;
		public readonly float InstabilityMagnitude = 2.0f;
		public readonly int InstabilityTicks = 5;	
		public readonly bool LandWhenIdle = true;
		
		public override object Create(ActorInitializer init) { return new MobileAir(init, this); }
	}
	
	public class MobileAir : Mobile, ITick, IOccupyAir
	{
		MobileAirInfo AirInfo;
		public MobileAir (ActorInitializer init, MobileAirInfo info)
			: base(init, info)
		{
			AirInfo = info;
		}

		public override void AddInfluence()
		{
			self.World.WorldActor.traits.Get<AircraftInfluence>().Add( self, this );
		}
		
		public override void RemoveInfluence()
		{
			self.World.WorldActor.traits.Get<AircraftInfluence>().Remove( self, this );
		}
		
		public override bool CanEnterCell(int2 p, Actor ignoreBuilding, bool checkTransientActors)
		{
			if (!checkTransientActors)
				return true;
			
			return self.World.WorldActor.traits.Get<AircraftInfluence>().GetUnitsAt(p).Count() == 0;
		}
		
		public override void FinishedMoving(Actor self) {}
		
		public override float MovementCostForCell(Actor self, int2 cell)
		{
			if (!self.World.Map.IsInMap(cell.X,cell.Y))
				return float.PositiveInfinity;
			
			var additionalCost = self.World.WorldActor.traits.WithInterface<ITerrainCost>()
				.Select( t => t.GetTerrainCost(cell, self) ).Sum();
			
			return additionalCost;
		}

		public override float MovementSpeedForCell(Actor self, int2 cell)
		{		
			var unitInfo = self.Info.Traits.GetOrDefault<UnitInfo>();
			if( unitInfo == null )
			   return 0f;
			
			var modifier = self.traits
				.WithInterface<ISpeedModifier>()
				.Select(t => t.GetSpeedModifier())
				.Product();
			return unitInfo.Speed * modifier;
		}
		
		public override IEnumerable<int2> OccupiedCells()
		{
			// Todo: do the right thing when landed
			return new int2[] {};
		}
		
		public IEnumerable<int2> OccupiedAirCells()
		{
			return (fromCell == toCell)
				? new[] { fromCell }
				: CanEnterCell(toCell)
					? new[] { toCell }
					: new[] { fromCell, toCell };
		}
		
		int offsetTicks = 0;
		public void Tick(Actor self)
		{
			var unit = self.traits.Get<Unit>();
			//if (unit.Altitude <= 0)
			//	return;
			
			if (unit.Altitude < AirInfo.CruiseAltitude)
				unit.Altitude++;
			
			if (--offsetTicks <= 0)
			{
				self.CenterLocation += AirInfo.InstabilityMagnitude * self.World.SharedRandom.Gauss2D(5);
				unit.Altitude += (int)(AirInfo.InstabilityMagnitude * self.World.SharedRandom.Gauss1D(5));
				offsetTicks = AirInfo.InstabilityTicks;
			}
		}
	}
}