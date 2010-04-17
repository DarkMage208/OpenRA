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
using OpenRA.GameRules;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class ResourceTypeInfo : ITraitInfo
	{
		public readonly string[] SpriteNames = { };
		public readonly string Palette = "terrain";
		public readonly int ResourceType = 1;

		public readonly int ValuePerUnit = 0;
		public readonly string Name = null;

		public readonly float GrowthInterval = 0;
		public readonly float SpreadInterval = 0;
		public readonly string MovementTerrainType = null;
		public readonly string PathingTerrainType = null;

		public Sprite[][] Sprites;
		
		public object Create(Actor self) { return new ResourceType(this); }
	}

	public class ResourceType : ITick
	{
		int growthTicks;
		int spreadTicks;
		public ResourceTypeInfo info;
		float[] movementSpeed = new float[4];
		float[] pathCost = new float[4];

		public ResourceType(ResourceTypeInfo info)
		{
			for (var umt = UnitMovementType.Foot; umt <= UnitMovementType.Float; umt++ )
			{
				// HACK: hardcode "ore" terraintype for now
				movementSpeed[(int)umt] = (info.MovementTerrainType != null) ? (float)Rules.TerrainTypes[TerrainType.Ore].GetSpeedMultiplier(umt) : 1.0f;
				pathCost[(int)umt] = (info.PathingTerrainType != null) ? (float)Rules.TerrainTypes[TerrainType.Ore].GetCost(umt) 
								  : (info.MovementTerrainType != null) ? (float)Rules.TerrainTypes[TerrainType.Ore].GetCost(umt) : 1.0f;
			}
			
			this.info = info;
		}
		
		public float GetSpeedMultiplier(UnitMovementType umt)
		{
			return movementSpeed[(int)umt];
		}
		
		public float GetCost(UnitMovementType umt)
		{
			return pathCost[(int)umt];
		}

		public void Tick(Actor self)
		{
			if (info.GrowthInterval != 0 && --growthTicks <= 0)
			{
				growthTicks = (int)(info.GrowthInterval * 25 * 60);
				self.World.WorldActor.traits.Get<ResourceLayer>().Grow(this);
				self.World.Minimap.InvalidateOre();
			}

			if (info.SpreadInterval != 0 && --spreadTicks <= 0)
			{
				spreadTicks = (int)(info.SpreadInterval * 25 * 60);
				self.World.WorldActor.traits.Get<ResourceLayer>().Spread(this);
				self.World.Minimap.InvalidateOre();
			}
		}
	}
}
