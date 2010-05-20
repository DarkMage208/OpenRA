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

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackLeapInfo : AttackBaseInfo
	{
		public override object Create(Actor self) { return new AttackLeap(self); }
	}

	class AttackLeap : AttackBase
	{
		public AttackLeap(Actor self)
			: base(self) {}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (target == null || !target.IsInWorld) return;
			if (self.GetCurrentActivity() is Leap) return;

			var weapon = self.GetPrimaryWeapon();
			if (weapon.Range * weapon.Range < (target.Location - self.Location).LengthSquared) return;

			self.CancelActivity();
			self.QueueActivity(new Leap(self, target));
		}
	}
}
