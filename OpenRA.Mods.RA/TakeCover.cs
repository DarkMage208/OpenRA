﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TakeCoverInfo : TraitInfo<TakeCover> { }

	// infantry prone behavior
	class TakeCover : ITick, INotifyDamage, IDamageModifier, ISpeedModifier, INotifyIdle
	{
		const int defaultProneTime = 100;	/* ticks, =4s */
		const float proneDamage = .5f;
		const decimal proneSpeed = .5m;

		[Sync]
		int remainingProneTime = 0;

		public bool IsProne { get { return remainingProneTime > 0; } }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0)		/* fix to allow healing via `damage` */
			{
				if (e.Warhead == null || !e.Warhead.PreventProne)
					remainingProneTime = defaultProneTime;
				
			}
		}

		public void Tick(Actor self)
		{
			if (IsProne)
				--remainingProneTime;
		}
		
		public void TickIdle(Actor self)
		{
			System.Console.WriteLine("TakeCover:TickIdle");
			if (remainingProneTime > 0)
			{
				System.Console.WriteLine("TakeCover: set anim to crawl");
				self.Trait<RenderSimple>().anim.PlayFetchIndex("crawl", () => 0);
			}
		}

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead )
		{
			return IsProne ? proneDamage : 1f;
		}

		public decimal GetSpeedModifier()
		{
			return IsProne ? proneSpeed : 1m;
		}
	}
}
