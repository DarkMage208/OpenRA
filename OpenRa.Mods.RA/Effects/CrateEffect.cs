﻿#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class CrateEffect : IEffect
	{
		Actor a;
		Animation anim = new Animation("crate-effects");
		float2 doorOffset = new float2(-4,0);

		public CrateEffect(Actor a, string seq)
		{
			this.a = a;
			anim.PlayThen(seq,
				() => a.World.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world )
		{
			anim.Tick();
		}
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
				a.CenterLocation - .5f * anim.Image.size + doorOffset, "effect");
		}
	}
}
