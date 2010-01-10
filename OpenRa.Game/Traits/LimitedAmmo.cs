﻿using System.Collections.Generic;

namespace OpenRa.Game.Traits
{
	class LimitedAmmoInfo : ITraitInfo
	{
		public readonly int Ammo = 0;

		public object Create(Actor self) { return new LimitedAmmo(self); }
	}

	class LimitedAmmo : INotifyAttack, IPips
	{
		[Sync]
		int ammo;
		Actor self;

		public LimitedAmmo(Actor self)
		{
			ammo = self.Info.Traits.Get<LimitedAmmoInfo>().Ammo;
			this.self = self;
		}

		public bool HasAmmo() { return ammo > 0; }
		public bool GiveAmmo()
		{
			if (ammo >= self.Info.Traits.Get<LimitedAmmoInfo>().Ammo) return false;
			++ammo;
			return true;
		}

		public void Attacking(Actor self) { --ammo; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var maxAmmo = self.Info.Traits.Get<LimitedAmmoInfo>().Ammo;
			return Graphics.Util.MakeArray(maxAmmo, 
				i => ammo > i ? PipType.Green : PipType.Transparent);
		}
	}
}
