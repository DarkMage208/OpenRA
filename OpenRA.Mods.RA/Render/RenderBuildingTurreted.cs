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

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingTurretedInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingTurreted(init.self); }
	}

	class RenderBuildingTurreted : RenderBuilding, INotifyBuildComplete
	{
		public RenderBuildingTurreted(Actor self)
			: base(self, () => self.traits.Get<Turreted>().turretFacing)
		{
		}

		public void BuildingComplete( Actor self )
		{
			anim.Play( "idle" );
		}

		public override void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;

			switch( e.DamageState )
			{
				case DamageState.Medium: case DamageState.Light: case DamageState.Undamaged:
					anim.ReplaceAnim("idle");
					break;
				case DamageState.Heavy: case DamageState.Critical:
					anim.ReplaceAnim("damaged-idle");
					Sound.Play(self.Info.Traits.Get<BuildingInfo>().DamagedSound, self.CenterLocation);
					break;
			}
		}
	}
}
