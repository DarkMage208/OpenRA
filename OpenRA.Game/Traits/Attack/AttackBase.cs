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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class AttackBaseInfo : ITraitInfo
	{
		public readonly string PrimaryWeapon = null;
		public readonly string SecondaryWeapon = null;
		public readonly int Recoil = 0;
		public readonly int[] PrimaryLocalOffset = { };
		public readonly int[] SecondaryLocalOffset = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly bool MuzzleFlash = false;
		public readonly int FireDelay = 0;

		public virtual object Create(Actor self) { return new AttackBase(self); }
	}

	class AttackBase : IIssueOrder, IResolveOrder, ITick
	{
		[Sync] public Actor target;

		// time (in frames) until each weapon can fire again.
		[Sync]
		protected int primaryFireDelay = 0;
		[Sync]
		protected int secondaryFireDelay = 0;

		int primaryBurst;
		int secondaryBurst;

		public float primaryRecoil = 0.0f, secondaryRecoil = 0.0f;

		public AttackBase(Actor self)
		{
			var primaryWeapon = self.GetPrimaryWeapon();
			var secondaryWeapon = self.GetSecondaryWeapon();

			primaryBurst = primaryWeapon != null ? primaryWeapon.Burst : 1;
			secondaryBurst = secondaryWeapon != null ? secondaryWeapon.Burst : 1;
		}

		protected bool CanAttack(Actor self)
		{
			return target != null;
		}

		public bool IsReloading()
		{
			return (primaryFireDelay > 0) || (secondaryFireDelay > 0);
		}

		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public virtual void Tick(Actor self)
		{
			if (primaryFireDelay > 0) --primaryFireDelay;
			if (secondaryFireDelay > 0) --secondaryFireDelay;

			primaryRecoil = Math.Max(0f, primaryRecoil - .2f);
			secondaryRecoil = Math.Max(0f, secondaryRecoil - .2f);

			if (target != null && target.IsDead) target = null;		/* he's dead, jim. */

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
			var unit = self.traits.GetOrDefault<Unit>();
			var info = self.Info.Traits.Get<AttackBaseInfo>();

			if (info.PrimaryWeapon != null && CheckFire(self, unit, info.PrimaryWeapon, ref primaryFireDelay,
				info.PrimaryOffset, ref primaryBurst, info.PrimaryLocalOffset))
			{
				secondaryFireDelay = Math.Max(4, secondaryFireDelay);
				primaryRecoil = 1;
				return;
			}

			if (info.SecondaryWeapon != null && CheckFire(self, unit, info.SecondaryWeapon, ref secondaryFireDelay,
				info.SecondaryOffset ?? info.PrimaryOffset, ref secondaryBurst, info.SecondaryLocalOffset))
			{
				if (info.SecondaryOffset != null) secondaryRecoil = 1;
				else primaryRecoil = 1;
				return;
			}
		}

		bool CheckFire(Actor self, Unit unit, string weaponName, ref int fireDelay, int[] offset, ref int burst, int[] localOffset)
		{
			if (fireDelay > 0) return false;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return false;

			var weapon = Rules.WeaponInfo[weaponName];
			if (weapon.Range * weapon.Range < (target.Location - self.Location).LengthSquared) return false;

			if (!Combat.WeaponValidForTarget(weapon, target)) return false;

			var numOffsets = (localOffset.Length + 2) / 3;
			if (numOffsets == 0) numOffsets = 1;
			var localOffsetForShot = burst % numOffsets;
			var thisLocalOffset = localOffset.Skip(3 * localOffsetForShot).Take(3).ToArray();

			var fireOffset = new[] { 
				offset.ElementAtOrDefault(0) + thisLocalOffset.ElementAtOrDefault(0), 
				offset.ElementAtOrDefault(1) + thisLocalOffset.ElementAtOrDefault(1), 
				offset.ElementAtOrDefault(2),
				offset.ElementAtOrDefault(3) };

			if (--burst > 0)
				fireDelay = 5;
			else
			{
				fireDelay = weapon.ROF;
				burst = weapon.Burst;
			}

			var firePos = self.CenterLocation.ToInt2() + Util.GetTurretPosition(self, unit, fireOffset, 0f).ToInt2();
			var thisTarget = target;	// closure.
			var destUnit = thisTarget.traits.GetOrDefault<Unit>();
			var info = self.Info.Traits.Get<AttackBaseInfo>();

			ScheduleDelayedAction(info.FireDelay, () =>
			{
				var srcAltitude = unit != null ? unit.Altitude : 0;
				var destAltitude = destUnit != null ? destUnit.Altitude : 0;
				
				if ( weapon.RenderAsLaser )
				{
					// TODO: This is a hack; should probably use a particular palette index
					Color bc = (weapon.UsePlayerColor) ? Player.PlayerColors[self.Owner.PaletteIndex].c : Color.Red;
					self.World.Add(new LaserZap(firePos, thisTarget.CenterLocation.ToInt2(), weapon.BeamRadius, bc));
				}	
				if( weapon.RenderAsTesla )
					self.World.Add( new TeslaZap( firePos, thisTarget.CenterLocation.ToInt2() ) );

				if (Rules.ProjectileInfo[weapon.Projectile].ROT != 0)
				{
					var fireFacing = thisLocalOffset.ElementAtOrDefault(2) + 
						(self.traits.Contains<Turreted>() ? self.traits.Get<Turreted>().turretFacing : 
						unit != null ? unit.Facing : Util.GetFacing( thisTarget.CenterLocation - self.CenterLocation, 0 ));
	
					self.World.Add(new Missile(weapon, self.Owner, self,
						firePos, thisTarget, srcAltitude, fireFacing));
				}
				else
					self.World.Add(new Bullet(weapon, self.Owner, self,
						firePos, thisTarget.CenterLocation.ToInt2(), srcAltitude, destAltitude));

				if (!string.IsNullOrEmpty(weapon.Report))
					Sound.Play(weapon.Report + ".aud");
			});

			foreach (var na in self.traits.WithInterface<INotifyAttack>())
				na.Attacking(self);

			return true;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left || underCursor == null) return null;
			if (self == underCursor) return null;

			var isHeal = self.GetPrimaryWeapon().Damage < 0;
			var forceFire = mi.Modifiers.HasModifier(Modifiers.Ctrl);

			if (isHeal)
			{
				if (underCursor.Owner == null)
					return null;
				if (underCursor.Owner != self.Owner && !forceFire)
					return null;
				if (underCursor.Health >= underCursor.GetMaxHP())
					return null;	// don't allow healing of fully-healed stuff!
			}
			else
				if ((underCursor.Owner == self.Owner || underCursor.Owner == null) && !forceFire)
					return null;
			
			if (!Combat.HasAnyValidWeapons(self, underCursor)) return null;

			return new Order(isHeal ? "Heal" : "Attack", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack" || order.OrderString == "Heal")
			{
				self.CancelActivity();
				QueueAttack(self, order);

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w => w.Add(new FlashTarget(order.TargetActor)));
			}
			else
				target = null;
		}

		protected virtual void QueueAttack(Actor self, Order order)
		{
			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = self.GetPrimaryWeapon() ?? self.GetSecondaryWeapon();

			self.QueueActivity(new Activities.Attack(order.TargetActor,
					Math.Max(0, (int)weapon.Range - RangeTolerance)));
		}
	}
}
