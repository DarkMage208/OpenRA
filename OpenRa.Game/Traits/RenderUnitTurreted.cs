﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	class RenderUnitTurreted : RenderUnit
	{
		public Animation turretAnim;
		public Animation muzzleFlash;

		public RenderUnitTurreted(Actor self)
			: base(self)
		{
			self.traits.Get<Turreted>();
			turretAnim = new Animation(self.unitInfo.Name);

			if (self.unitInfo.MuzzleFlash)
			{
				var attack = self.traits.WithInterface<AttackBase>().First();
				muzzleFlash = new Animation(self.unitInfo.Name);
				muzzleFlash.PlayFetchIndex("muzzle",
					() => (Util.QuantizeFacing(self.traits.Get<Turreted>().turretFacing,8)) * 6 + (int)(attack.primaryRecoil * 5.9f));	
						/* hack: recoil can be 1.0f, but don't overflow into next anim */
			}

			turretAnim.PlayFetchIndex("turret",
				() => self.traits.Get<Turreted>().turretFacing / 8);
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var unit = self.traits.Get<Unit>();
			var attack = self.traits.WithInterface<AttackBase>().FirstOrDefault();

			yield return Util.Centered(self, anim.Image, self.CenterLocation);
			yield return Util.Centered(self, turretAnim.Image, self.CenterLocation 
				+ Util.GetTurretPosition(self, unit, self.unitInfo.PrimaryOffset, attack.primaryRecoil));
			if (self.unitInfo.SecondaryOffset != null)
				yield return Util.Centered(self, turretAnim.Image, self.CenterLocation
					+ Util.GetTurretPosition(self, unit, self.unitInfo.SecondaryOffset, attack.secondaryRecoil));

			if (muzzleFlash != null && attack.primaryRecoil > 0)
				yield return Util.Centered(self, muzzleFlash.Image, self.CenterLocation
				+ Util.GetTurretPosition(self, unit, self.unitInfo.PrimaryOffset, attack.primaryRecoil));
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			turretAnim.Tick();
			if (muzzleFlash != null)
				muzzleFlash.Tick();
		}
	}
}
