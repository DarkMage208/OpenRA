﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class AttackBase : IOrder, ITick
	{
		public Actor target;

		// time (in frames) until each weapon can fire again.
		protected int primaryFireDelay = 0;
		protected int secondaryFireDelay = 0;

		public float primaryRecoil = 0.0f, secondaryRecoil = 0.0f;

		public AttackBase(Actor self) { }

		protected bool CanAttack( Actor self )
		{
			return target != null;
		}

		public virtual void Tick(Actor self)
		{
			if (primaryFireDelay > 0) --primaryFireDelay;
			if (secondaryFireDelay > 0) --secondaryFireDelay;

			primaryRecoil = Math.Max(0f, primaryRecoil - .2f);
			secondaryRecoil = Math.Max(0f, secondaryRecoil - .2f);

			if (target != null && target.IsDead) target = null;		/* he's dead, jim. */
		}

		public void DoAttack( Actor self )
		{
			var unit = self.traits.Get<Unit>();

			if( self.unitInfo.Primary != null && CheckFire( self, unit, self.unitInfo.Primary, ref primaryFireDelay, 
				self.unitInfo.PrimaryOffset ) )
			{
				secondaryFireDelay = Math.Max( 4, secondaryFireDelay );
				primaryRecoil = 1;
				return;
			}

			if (self.unitInfo.Secondary != null && CheckFire(self, unit, self.unitInfo.Secondary, ref secondaryFireDelay,
				self.unitInfo.SecondaryOffset ?? self.unitInfo.PrimaryOffset))
			{
				if (self.unitInfo.SecondaryOffset != null) secondaryRecoil = 1;
				else primaryRecoil = 1;
				return;
			}
		}

		bool CheckFire( Actor self, Unit unit, string weaponName, ref int fireDelay, int[] offset )
		{
			if( fireDelay > 0 ) return false;
			var weapon = Rules.WeaponInfo[ weaponName ];
			if( weapon.Range * weapon.Range < ( target.Location - self.Location ).LengthSquared ) return false;

			fireDelay = weapon.ROF;

			Game.world.Add( new Bullet( weaponName, self.Owner, self,
				self.CenterLocation.ToInt2() + Util.GetTurretPosition( self, unit, offset, 0f ).ToInt2(),
				target.CenterLocation.ToInt2() ) );

			return true;
		}

		public Order Order( Actor self, int2 xy, bool lmb, Actor underCursor )
		{
			if( lmb || underCursor == null ) return null;
			if( underCursor.Owner == self.Owner ) return null;
			return OpenRa.Game.Order.Attack( self, underCursor );
		}
	}

	class AttackTurreted : AttackBase
	{
		public AttackTurreted( Actor self ) : base(self) { self.traits.Get<Turreted>(); }

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if( !CanAttack( self ) ) return;

			var turreted = self.traits.Get<Turreted>();
			turreted.desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turreted.turretFacing );
			if( turreted.desiredFacing != turreted.turretFacing )
				return;

			DoAttack( self );
		}
	}
}
