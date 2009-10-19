﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class AttackTurreted : ITick
	{
		public Actor target;

		// time (in frames) until each weapon can fire again.
		int primaryFireDelay = 0;
		int secondaryFireDelay = 0;

		public AttackTurreted( Actor self )
		{
			self.traits.Get<Turreted>();
		}

		public void Tick( Actor self, Game game, int dt )
		{
			if( primaryFireDelay > 0 )
				--primaryFireDelay;
			if( secondaryFireDelay > 0 )
				--secondaryFireDelay;

			if( target == null )
				return;

			var mobile = self.traits.Get<Mobile>();
			var turreted = self.traits.Get<Turreted>();
			turreted.desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turreted.turretFacing );
			if( turreted.desiredFacing != turreted.turretFacing )
				return;

			if( self.unitInfo.Primary != null && CheckFire( self, game, self.unitInfo.Primary, ref primaryFireDelay ) )
			{
				secondaryFireDelay = Math.Max( 4, secondaryFireDelay );
				return;
			}
			if( self.unitInfo.Secondary != null && CheckFire( self, game, self.unitInfo.Secondary, ref secondaryFireDelay ) )
				return;
		}

		bool CheckFire( Actor self, Game game, string weaponName, ref int fireDelay )
		{
			if( fireDelay > 0 )
				return false;
			var weapon = Rules.WeaponInfo[ weaponName ];
			var d = target.Location - self.Location;
			if( weapon.Range * weapon.Range < d.X * d.X + d.Y * d.Y )
				return false;

			// FIXME: rules specifies ROF in 1/15 sec units; ticks are 1/25 sec
			fireDelay = weapon.ROF;

			game.world.Add( new Bullet( weaponName, self.Owner, self, self.CenterLocation.ToInt2(), target.CenterLocation.ToInt2(), game ) );
			return true;
		}
	}
}
