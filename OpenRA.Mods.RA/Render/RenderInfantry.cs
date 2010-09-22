﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA.Render
{
	public class RenderInfantryInfo : RenderSimpleInfo
	{
		public override object Create(ActorInitializer init) { return new RenderInfantry(init.self); }
	}

	public class RenderInfantry : RenderSimple, INotifyAttack, INotifyDamage
	{
		public RenderInfantry(Actor self)
			: base(self, () => self.Trait<IFacing>().Facing)
		{
			anim.Play("stand");
		}

		bool ChooseMoveAnim(Actor self)
		{
			var mobile = self.Trait<Mobile>();
			if( !mobile.IsMoving ) return false;

			if (float2.WithinEpsilon(self.CenterLocation, Util.CenterOfCell(mobile.toCell), 2)) return false;

			var seq = IsProne(self) ? "crawl" : "run";

			if (anim.CurrentSequence.Name != seq)
				anim.PlayRepeating(seq);

			return true;
		}

		bool inAttack = false;
		bool IsProne(Actor self)
		{
			var takeCover = self.TraitOrDefault<TakeCover>();
			return takeCover != null && takeCover.IsProne;
		}

		public void Attacking(Actor self)
		{
			inAttack = true;

			var seq = IsProne(self) ? "prone-shoot" : "shoot";

			if (anim.HasSequence(seq))
				anim.PlayThen(seq, () => inAttack = false);
			else if (anim.HasSequence("heal"))
				anim.PlayThen("heal", () => inAttack = false);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (inAttack) return;
			if (self.GetCurrentActivity() is Activities.IdleAnimation) return;
			if (ChooseMoveAnim(self)) return;

			if (IsProne(self))
				anim.PlayFetchIndex("crawl", () => 0);			/* what a hack. */
			else
				anim.Play("stand");
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				var death = e.Warhead != null ? e.Warhead.InfDeath : 0;
				Sound.PlayVoice("Die", self, self.Owner.Country.Race);
				self.World.AddFrameEndTask(w => w.Add(new Corpse(self, death)));
			}
		}
	}
}
