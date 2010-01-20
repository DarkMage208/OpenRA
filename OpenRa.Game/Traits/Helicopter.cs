﻿using System;
using System.Linq;
using OpenRa.GameRules;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class HelicopterInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Helicopter(self); }
	}

	class Helicopter : IIssueOrder, IResolveOrder, IMovement
	{
		public IDisposable reservation;
		public Helicopter(Actor self) {}

		static bool HeliCanEnter(Actor a)
		{
			if (a.Info.Name == "hpad") return true;
			if (a.Info.Name == "fix") return true;
			return false;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == null)
				return new Order("Move", self, null, xy, null);

			if (HeliCanEnter(underCursor)
				&& underCursor.Owner == self.Owner
				&& !Reservable.IsReserved(underCursor))
				return new Order("Enter", self, underCursor, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}

			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new HeliFly(Util.CenterOfCell(order.TargetLocation)));
				self.QueueActivity(new Turn(self.Info.Traits.GetOrDefault<UnitInfo>().InitialFacing));
				self.QueueActivity(new HeliLand(true));
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;
				var res = order.TargetActor.traits.GetOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(self);

				var productionInfo = order.TargetActor.Info.Traits.GetOrDefault<ProductionInfo>();
				var offset = productionInfo != null ? productionInfo.SpawnOffset : null;
				var offsetVec = offset != null ? new float2(offset[0], offset[1]) : float2.Zero;

				self.CancelActivity();
				self.QueueActivity(new HeliFly(order.TargetActor.CenterLocation + offsetVec));
				self.QueueActivity(new Turn(self.Info.Traits.GetOrDefault<UnitInfo>().InitialFacing));
				self.QueueActivity(new HeliLand(false));
				self.QueueActivity(order.TargetActor.Info.Name == "hpad"
					? (IActivity)new Rearm() : new Repair(true));
			}
		}

		public UnitMovementType GetMovementType() { return UnitMovementType.Fly; }
		public bool CanEnterCell(int2 location) { return true; }
	}
}
