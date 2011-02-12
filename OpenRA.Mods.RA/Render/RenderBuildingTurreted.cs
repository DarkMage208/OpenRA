#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingTurretedInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingTurreted( init ); }
	}

	class RenderBuildingTurreted : RenderBuilding, INotifyBuildComplete
	{
		public RenderBuildingTurreted( ActorInitializer init )
			: base(init, () => init.self.Trait<Turreted>().turretFacing)
		{
		}

		public void BuildingComplete( Actor self )
		{
			anim.Play( "idle" );
		}

		public override void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;

			if (e.DamageState >= DamageState.Heavy && e.PreviousDamageState < DamageState.Heavy)
				anim.ReplaceAnim("damaged-idle");
			else if (e.DamageState < DamageState.Heavy)
				anim.ReplaceAnim("idle");
		}
	}
}
