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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA
{
	public static class Exts
	{
		public static bool HasModifier(this Modifiers k, Modifiers mod)
		{
			return (k & mod) == mod;
		}

		public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> xs, IEnumerable<T> ys)
		{
			// this is probably a shockingly-slow way to do this, but it's concise.
			return xs.Except(ys).Concat(ys.Except(xs));
		}

		public static float Product(this IEnumerable<float> xs)
		{
			return xs.Aggregate(1f, (a, x) => a * x);
		}

		public static NewWeaponInfo GetPrimaryWeapon(this Actor self)
		{
			var info = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (info == null) return null;
			
			var weapon = info.PrimaryWeapon;
			if (weapon == null) return null;

			return Rules.Weapons[weapon.ToLowerInvariant()];
		}

		public static NewWeaponInfo GetSecondaryWeapon(this Actor self)
		{
			var info = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (info == null) return null;

			var weapon = info.SecondaryWeapon;
			if (weapon == null) return null;

			return Rules.Weapons[weapon.ToLowerInvariant()];
		}

		public static int GetMaxHP(this Actor self)
		{
			var oai = self.Info.Traits.GetOrDefault<OwnedActorInfo>();
			if (oai == null) return 0;
			return oai.HP;
		}

		public static V GetOrAdd<K, V>( this Dictionary<K, V> d, K k )
			where V : new()
		{
			return d.GetOrAdd( k, _ => new V() );
		}

		public static V GetOrAdd<K, V>( this Dictionary<K, V> d, K k, Func<K, V> createFn )
		{
			V ret;
			if( !d.TryGetValue( k, out ret ) )
				d.Add( k, ret = createFn( k ) );
			return ret;
		}

		public static T Random<T>(this IEnumerable<T> ts, Thirdparty.Random r)
		{
			var xs = ts.ToArray();
			return xs[r.Next(xs.Length)];
		}
	}
}
