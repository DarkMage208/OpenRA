﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PrimaryBuildingInfo : TraitInfo<PrimaryBuilding> { }

	class PrimaryBuilding : IIssueOrder, IResolveOrder, IOrderCursor, ITags
	{
		bool isPrimary = false;
		public bool IsPrimary { get { return isPrimary; } }

		public IEnumerable<TagType> GetTags()
		{
			yield return (isPrimary) ? TagType.Primary : TagType.None;
		}
		
		public int OrderPriority(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return 0;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && underCursor == self)
				return new Order("PrimaryProducer", self);
			return null;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "PrimaryProducer") ? "deploy" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PrimaryProducer")
				SetPrimaryProducer(self, !isPrimary);
		}

		public void SetPrimaryProducer(Actor self, bool state)
		{
			if (state == false)
			{
				isPrimary = false;
				return;
			}

			// Cancel existing primaries
			foreach (var p in self.Info.Traits.Get<ProductionInfo>().Produces)
				foreach (var b in self.World.Queries.OwnedBy[self.Owner]
					.WithTrait<PrimaryBuilding>()
					.Where(x => x.Trait.IsPrimary
						&& (x.Actor.Info.Traits.Get<ProductionInfo>().Produces.Contains(p))))
					b.Trait.SetPrimaryProducer(b.Actor, false);

			isPrimary = true;

			var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			Sound.PlayToPlayer(self.Owner, eva.PrimaryBuildingSelected);
		}
	}

	static class PrimaryExts
	{
		public static bool IsPrimaryBuilding(this Actor a)
		{
			var pb = a.TraitOrDefault<PrimaryBuilding>();
			return pb != null && pb.IsPrimary;
		}
	}
}
