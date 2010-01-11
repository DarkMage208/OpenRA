﻿using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitTurretedInfo : RenderUnitInfo
	{
		public override object Create(Actor self) { return new RenderUnitTurreted(self); }
	}

	class RenderUnitTurreted : RenderUnit
	{
		public RenderUnitTurreted(Actor self)
			: base(self)
		{
			var unit = self.traits.Get<Unit>();
			var turreted = self.traits.Get<Turreted>();
			var attack = self.traits.WithInterface<AttackBase>().FirstOrDefault();
			var attackInfo = self.Info.Traits.WithInterface<AttackBaseInfo>().First();

			var turretAnim = new Animation(GetImage(self));
			turretAnim.PlayFacing( "turret", () => turreted.turretFacing );

			if( attackInfo.PrimaryOffset != null )
				anims.Add("turret_1", new AnimationWithOffset(
					turretAnim,
					() => Util.GetTurretPosition(self, unit, attackInfo.PrimaryOffset, attack.primaryRecoil),
					null) { ZOffset = 1 });

			if (attackInfo.SecondaryOffset != null)
				anims.Add("turret_2", new AnimationWithOffset(
					turretAnim,
					() => Util.GetTurretPosition(self, unit, attackInfo.SecondaryOffset, attack.secondaryRecoil),
					null) { ZOffset = 1 });

			if( attackInfo.MuzzleFlash )
			{
				var muzzleFlash = new Animation( GetImage(self) );
				muzzleFlash.PlayFetchIndex( "muzzle",
					() => ( Util.QuantizeFacing( self.traits.Get<Turreted>().turretFacing, 8 ) ) * 6
						+ (int)( attack.primaryRecoil * 5.9f ) ); /* hack: recoil can be 1.0f, but don't overflow into next anim */
				anims.Add( "muzzle_flash", new AnimationWithOffset(
					muzzleFlash,
					() => Util.GetTurretPosition(self, unit, attackInfo.PrimaryOffset, attack.primaryRecoil),
					() => attack.primaryRecoil <= 0 ) );
			}
		}
	}
}
