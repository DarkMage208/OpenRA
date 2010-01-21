﻿using System;
using System.Collections.Generic;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	public class DelayedAction : IEffect
	{
		Action a;
		int delay;

		public DelayedAction(int delay, Action a)
		{
			this.a = a;
			this.delay = delay;
		}

		public void Tick( World world )
		{
			if (--delay <= 0)
				world.AddFrameEndTask(w => { w.Remove(this); a(); });
		}

		public IEnumerable<Renderable> Render() { yield break; }
	}
}
