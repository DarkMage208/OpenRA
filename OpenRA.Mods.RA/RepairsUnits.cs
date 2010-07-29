#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class RepairsUnitsInfo : TraitInfo<RepairsUnits>
	{
		public readonly float URepairPercent = 0.2f;
		public readonly int URepairStep = 10;
		public readonly float RepairRate = 0.016f;
	}

	public class RepairsUnits { }
}
