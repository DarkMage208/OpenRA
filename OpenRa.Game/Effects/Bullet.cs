﻿using System;
using System.Collections.Generic;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Effects
{
	class Bullet : IEffect
	{
		readonly Player Owner;
		readonly Actor FiredBy;
		readonly WeaponInfo Weapon;
		readonly ProjectileInfo Projectile;
		readonly WarheadInfo Warhead;
		readonly int2 Src;
		readonly int2 Dest;
		readonly int2 VisualDest;

		int t = 0;
		Animation anim;

		const int BaseBulletSpeed = 100;		/* pixels / 40ms frame */

		/* src, dest are *pixel* coords */
		public Bullet(string weapon, Player owner, Actor firedBy, 
			int2 src, int2 dest)
		{
			Owner = owner;
			FiredBy = firedBy;
			Src = src;
			Dest = dest;
			VisualDest = Dest + new int2(
						Game.CosmeticRandom.Next(-10, 10),
						Game.CosmeticRandom.Next(-10, 10));
			Weapon = Rules.WeaponInfo[weapon];
			Projectile = Rules.ProjectileInfo[Weapon.Projectile];
			Warhead = Rules.WarheadInfo[Weapon.Warhead];

			if (Projectile.Image != null && Projectile.Image != "none")
			{
				anim = new Animation(Projectile.Image);
				if (Projectile.Rotates)
					Traits.Util.PlayFacing(anim, "idle", () => Traits.Util.GetFacing((dest - src).ToFloat2(), 0));
				else
					anim.PlayRepeating("idle");
			}
		}

		int TotalTime() { return (Dest - Src).Length * BaseBulletSpeed / Weapon.Speed; }

		public void Tick()
		{
			if (t == 0)
				Sound.Play(Weapon.Report + ".aud");

			t += 40;

			if (t > TotalTime())		/* remove finished bullets */
			{
				Game.world.AddFrameEndTask(w => w.Remove(this));
				Combat.DoImpact(Dest, VisualDest, Weapon, Projectile, Warhead, FiredBy);
			}
		}

		const float height = .1f;

		public IEnumerable<Tuple<Sprite, float2, int>> Render()
		{
			if (anim != null)
			{
				var pos = float2.Lerp(
						Src.ToFloat2(),
						VisualDest.ToFloat2(),
						(float)t / TotalTime()) - 0.5f * anim.Image.size;

				if (Projectile.High || Projectile.Arcing)
				{
					if (Projectile.Shadow)
						yield return Tuple.New(anim.Image, pos, 8);

					var at = (float)t / TotalTime();
					var highPos = pos - new float2(0, (VisualDest - Src).Length * height * 4 * at * (1 - at));

					yield return Tuple.New(anim.Image, highPos, Owner.Palette);
				}
				else
					yield return Tuple.New(anim.Image, pos, Owner.Palette);
			}
		}
	}
}
