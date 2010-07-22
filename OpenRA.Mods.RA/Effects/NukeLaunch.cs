#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class NukeLaunch : IEffect
	{
		readonly Actor silo;
		Animation anim;
		float2 pos;
		int2 targetLocation;
		int altitude;
		bool goingUp = true;
		string weapon;

		public NukeLaunch(Actor silo, string weapon, int2 targetLocation)
		{
			this.silo = silo;
			this.targetLocation = targetLocation;
			this.weapon = weapon;
			anim = new Animation(weapon);
			anim.PlayRepeating("up");
			
			if (silo == null)
			{
				altitude = Game.world.Map.Height*Game.CellSize;
				StartDescent(Game.world);
			}
			else
				pos = silo.CenterLocation;
		}

		void StartDescent(World world)
		{
			pos = OpenRA.Traits.Util.CenterOfCell(targetLocation);
			anim.PlayRepeating("down");
			goingUp = false;
		}
		
		public void Tick(World world)
		{
			anim.Tick();

			if (goingUp)
			{
				altitude += 10;
				if (altitude >= world.Map.Height*Game.CellSize)
					StartDescent(world);
			}
			else
			{
				altitude -= 10;
				if (altitude <= 0)
				{
					// Trigger screen desaturate effect
					foreach (var a in Game.world.Queries.WithTrait<NukePaletteEffect>())
						a.Trait.Enable();
					
					Explode(world);
				}
			}
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(silo.Owner.PlayerActor, weapon, Target.FromPos(pos), 0);
			world.WorldActor.traits.Get<ScreenShaker>().AddEffect(20, pos, 5);
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - 0.5f * anim.Image.size - new float2(0, altitude), "effect");
		}
	}
}
