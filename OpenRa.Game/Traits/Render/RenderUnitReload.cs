﻿using System.Linq;

namespace OpenRa.Traits
{
	class RenderUnitReloadInfo : RenderUnitInfo
	{
		public override object Create(Actor self) { return new RenderUnitReload(self); }
	}

	class RenderUnitReload : RenderUnit
	{
		public RenderUnitReload(Actor self)
			: base(self) { }

		public override void Tick(Actor self)
		{
			var isAttacking = self.GetCurrentActivity() is Activities.Attack;

			var attack = self.traits.GetOrDefault<AttackBase>();

			if (attack != null)
				anim.ReplaceAnim((attack.IsReloading() ? "empty-" : "")
					+ (isAttacking ? "aim" : "idle"));
			base.Tick(self);
		}
	}
}
