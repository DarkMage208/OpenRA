﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	public class AttackBaseInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string PrimaryWeapon = null;
		[WeaponReference]
		public readonly string SecondaryWeapon = null;
		public readonly int Recoil = 0;
		public readonly int[] PrimaryLocalOffset = { };
		public readonly int[] SecondaryLocalOffset = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly bool MuzzleFlash = false;
		public readonly int FireDelay = 0;

		public virtual object Create(ActorInitializer init) { return new AttackBase(init.self); }
	}

	public class Barrel { public int2 Position; public int Facing; /* relative to turret */ }

	public class Weapon
	{
		public WeaponInfo Info;
		public int FireDelay = 0;			// time (in frames) until the weapon can fire again
		public int Burst = 0;				// burst counter
		public float Recoil = 0.0f;			// remaining recoil fraction

		public int[] Offset;
		public Barrel[] Barrels;

		public Weapon(string weaponName, int[] offset, int[] localOffset)
		{
			Info = Rules.Weapons[weaponName.ToLowerInvariant()];
			Burst = Info.Burst;
			Offset = offset;

			var barrels = new List<Barrel>();
			for (var i = 0; i < localOffset.Length / 3; i++)
				barrels.Add(new Barrel
				{
					Position = new int2(localOffset[3 * i], localOffset[3 * i + 1]),
					Facing = localOffset[3 * i + 2]
				});

			// if no barrels specified, the default is "turret position; turret facing".
			if (barrels.Count == 0)
				barrels.Add(new Barrel { Position = int2.Zero, Facing = 0 });

			Barrels = barrels.ToArray();
		}

		public bool IsReloading { get { return FireDelay > 0; } }

		public void Tick()
		{
			if (FireDelay > 0) --FireDelay;
			Recoil = Math.Max(0f, Recoil - .2f);
		}

		public bool IsValidAgainst(Target target)
		{
			return Combat.WeaponValidForTarget(Info, target);
		}
	}

	public class AttackBase : IIssueOrder, IResolveOrder, ITick, IExplodeModifier, IOrderCursor, IOrderVoice
	{
		public Target target;

		public List<Weapon> Weapons = new List<Weapon>();

		public AttackBase(Actor self)
		{
			var info = self.Info.Traits.Get<AttackBaseInfo>();

			if (info.PrimaryWeapon != null)
				Weapons.Add(new Weapon(info.PrimaryWeapon, 
					info.PrimaryOffset, info.PrimaryLocalOffset));

			if (self.GetSecondaryWeapon() != null)
				Weapons.Add(new Weapon(info.SecondaryWeapon, 
					info.SecondaryOffset ?? info.PrimaryOffset, info.SecondaryLocalOffset));
		}

		protected virtual bool CanAttack(Actor self)
		{
			if (!target.IsValid) return false;
			if (Weapons.All(w => w.IsReloading)) return false;
			if (self.traits.WithInterface<IDisable>().Any(d => d.Disabled)) return false;

			return true;
		}

		public bool ShouldExplode(Actor self) { return !IsReloading(); }

		public bool IsReloading() { return Weapons.Any(w => w.IsReloading); }

		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public virtual void Tick(Actor self)
		{
			foreach (var w in Weapons)
				w.Tick();

			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.First <= 0)
					x.Second();
				delayedActions[i] = x;
			}
			delayedActions.RemoveAll(a => a.First <= 0);
		}

		void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add(Pair.New(t, a));
			else
				a();
		}

		public void DoAttack(Actor self)
		{
			if( !CanAttack( self ) ) return;

			var unit = self.traits.GetOrDefault<Unit>();
			var info = self.Info.Traits.Get<AttackBaseInfo>();

			foreach (var w in Weapons)
				if (CheckFire(self, unit, w))
					w.Recoil = 1;
		}

		bool CheckFire(Actor self, Unit unit, Weapon w)
		{
			if (w.FireDelay > 0) return false;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return false;

			if (w.Info.Range * w.Info.Range * Game.CellSize * Game.CellSize
			    < (target.CenterLocation - self.CenterLocation).LengthSquared) return false;
			
			if (!w.IsValidAgainst(target)) return false;

			var barrel = w.Barrels[w.Burst % w.Barrels.Length];
		
			var fireOffset = new[] { 
				w.Offset.ElementAtOrDefault(0) + barrel.Position.X,
				w.Offset.ElementAtOrDefault(1) + barrel.Position.Y,
				w.Offset.ElementAtOrDefault(2),
				w.Offset.ElementAtOrDefault(3) };

			if (--w.Burst > 0)
				w.FireDelay = w.Info.BurstDelay;
			else
			{
				w.FireDelay = w.Info.ROF;
				w.Burst = w.Info.Burst;
			}

			var destUnit = target.IsActor ? target.Actor.traits.GetOrDefault<Unit>() : null;

			var args = new ProjectileArgs
			{
				weapon = w.Info,

				firedBy = self,
				target = this.target,

				src = self.CenterLocation.ToInt2() + Combat.GetTurretPosition(self, unit, fireOffset, 0f).ToInt2(),
				srcAltitude = unit != null ? unit.Altitude : 0,
				dest = target.CenterLocation.ToInt2(),
				destAltitude = destUnit != null ? destUnit.Altitude : 0,
				
				facing = barrel.Facing + 
					(self.traits.Contains<Turreted>() ? self.traits.Get<Turreted>().turretFacing :
					unit != null ? unit.Facing : Util.GetFacing(target.CenterLocation - self.CenterLocation, 0)),
			};
			
			ScheduleDelayedAction( FireDelay( self, self.Info.Traits.Get<AttackBaseInfo>() ), () =>
			{
				if (args.weapon.Projectile != null)
				{
					var projectile = args.weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (!string.IsNullOrEmpty(args.weapon.Report))
						Sound.Play(args.weapon.Report + ".aud", self.CenterLocation);
				}
			});

			foreach (var na in self.traits.WithInterface<INotifyAttack>())
				na.Attacking(self);

			return true;
		}

		public virtual int FireDelay( Actor self, AttackBaseInfo info )
		{
			return info.FireDelay;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (self == underCursor) return null;

			var target = underCursor == null ? Target.FromCell(xy) : Target.FromActor(underCursor);

			var isHeal = Weapons[0].Info.Warheads[0].Damage < 0;
			var forceFire = mi.Modifiers.HasModifier(Modifiers.Ctrl);

			if (isHeal)
			{
				// we can never "heal ground"; that makes no sense.
				if (!target.IsActor) return null;
				
				// unless forced, only heal allies.
				if (self.Owner.Stances[underCursor.Owner] != Stance.Ally && !forceFire) return null;
				
				// don't allow healing of fully-healed stuff!
				if (underCursor.GetDamageState() == DamageState.Undamaged) return null;
			}
			else
			{
				if (!target.IsActor)
				{
					if (!forceFire) return null;
					return new Order("Attack", self, xy);
				}

				if ((self.Owner.Stances[underCursor.Owner] != Stance.Enemy) && !forceFire)
					return null;
			}
			
			if (!HasAnyValidWeapons(target)) return null;

			return new Order(isHeal ? "Heal" : "Attack", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack" || order.OrderString == "Heal")
			{
				self.CancelActivity();
				QueueAttack(self, order);

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (order.TargetActor != null)
							w.Add(new FlashTarget(order.TargetActor));
						
						var line = self.traits.GetOrDefault<DrawLineToTarget>();
						if (line != null)
							if (order.TargetActor != null) line.SetTarget(self, Target.FromOrder(order), Color.Red);
							else line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});
			}
			else
				target = Target.None;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Attack": return "attack";
				case "Heal": return "heal";
				default: return null;
			}
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Attack" || order.OrderString == "Heal") ? "Attack" : null;
		}
		
		protected virtual void QueueAttack(Actor self, Order order)
		{
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = self.GetPrimaryWeapon() ?? self.GetSecondaryWeapon();
			self.QueueActivity(
				new Activities.Attack(
					Target.FromOrder(order), 
					Math.Max(0, (int)weapon.Range)));
		}

		/* temp hack */
		public float GetPrimaryRecoil() { return Weapons.Count > 0 ? Weapons[0].Recoil : 0; }
		public float GetSecondaryRecoil() { return Weapons.Count > 1 ? Weapons[1].Recoil : 0; }

		public bool HasAnyValidWeapons(Target t) { return Weapons.Any(w => w.IsValidAgainst(t)); }
		public float GetMaximumRange() { return Weapons.Max(w => w.Info.Range); }
	}
}
