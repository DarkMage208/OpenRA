﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	abstract class AttackFrontal : AttackBase
	{
		public AttackFrontal(Actor self, int facingTolerance)
			: base(self) { FacingTolerance = facingTolerance; }

		readonly int FacingTolerance;

		protected override bool CanAttack( Actor self )
		{
			if( !base.CanAttack( self ) )
				return false;

			var facing = self.Trait<IFacing>().Facing;
			var facingToTarget = Util.GetFacing(target.CenterLocation - self.CenterLocation, facing);

			if( Math.Abs( facingToTarget - facing ) % 256 >= FacingTolerance )
				return false;

			return true;
		}
	}
}
