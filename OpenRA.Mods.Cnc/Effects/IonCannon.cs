#region Copyright & License Information
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

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	class IonCannon : IEffect
	{
		int2 Target;
		Animation anim;
		Actor firedBy;

		public IonCannon(Actor firedBy, World world, int2 location)
		{
			this.firedBy = firedBy;
			Target = location;
			anim = new Animation("ionsfx");
			anim.PlayThen("idle", () => Finish(world));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
				Traits.Util.CenterOfCell(Target) - new float2(.5f * anim.Image.size.X, anim.Image.size.Y - Game.CellSize),
				"effect");
		}

		void Finish( World world )
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(firedBy, "IonCannon", Target, 0);
		}
	}
}
