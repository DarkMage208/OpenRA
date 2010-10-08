﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackPlaneInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackPlane(init.self); }
	}

	class AttackPlane : AttackFrontal
	{
		public AttackPlane(Actor self) : base(self, 20) { }

		protected override void QueueAttack(Actor self, Order order)
		{
			if (self.Trait<Aircraft>().Altitude == 0)
				return;	// dont fire while landed

			target = Target.FromOrder(order);
			self.QueueActivity(new FlyAttack(target));
		}
	}
}
