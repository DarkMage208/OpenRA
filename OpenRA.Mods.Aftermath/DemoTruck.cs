﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Aftermath
{
	class DemoTruckInfo : TraitInfo<DemoTruck> { }

	class DemoTruck : Chronoshiftable, INotifyDamage
	{
		// Explode on chronoshift
		public override bool Activate(Actor self, int2 targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			Detonate(self, chronosphere);
			return false;
		}

		// Fire primary on death
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Detonate(self, e.Attacker);
		}

		public void Detonate(Actor self, Actor detonatedBy)
		{
			var unit = self.traits.GetOrDefault<Unit>();
			var info = self.Info.Traits.Get<AttackBaseInfo>();
			var altitude = unit != null ? unit.Altitude : 0;

			self.World.AddFrameEndTask( w =>
			{
				Combat.DoExplosion(self, info.PrimaryWeapon, Target.FromActor(self), altitude);
				var report = self.GetPrimaryWeapon().Report;
				if (report != null)
					Sound.Play(report + ".aud", self.CenterLocation);
				
				// Remove from world
				self.Health = 0;
				detonatedBy.Owner.Kills++;
				self.Owner.Deaths++;
				w.Remove(self);
			} );
		}
	}
}
