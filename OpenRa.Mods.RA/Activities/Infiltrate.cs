﻿using OpenRa.Traits.Activities;
using OpenRa.Traits;

namespace OpenRa.Mods.RA.Activities
{
	class Infiltrate : IActivity
	{
		Actor target;
		public Infiltrate(Actor target) { this.target = target; }
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;
			if (target.Owner == self.Owner) return NextActivity;

			foreach (var t in target.traits.WithInterface<IAcceptSpy>())
				t.OnInfiltrate(target, self);

			self.World.AddFrameEndTask(w => w.Remove(self));

			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
