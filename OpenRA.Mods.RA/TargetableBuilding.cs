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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using System.Linq;

namespace OpenRA.Mods.RA
{
	class TargetableBuildingInfo : ITraitInfo, ITraitPrerequisite<BuildingInfo>
	{
		public readonly string[] TargetTypes = { };
		public object Create( ActorInitializer init ) { return new TargetableBuilding( this ); }
	}

	class TargetableBuilding : ITargetable
	{
		readonly TargetableBuildingInfo info;
		public TargetableBuilding( TargetableBuildingInfo info )
		{
			this.info = info;
		}

		public string[] TargetTypes { get { return info.TargetTypes; } }
		public bool TargetableBy(Actor self, Actor byActor) { return true; }
		public IEnumerable<int2> TargetableCells( Actor self )
		{
			return self.Trait<Building>().OccupiedCells().Select(c => c.First);
		}
	}
}
