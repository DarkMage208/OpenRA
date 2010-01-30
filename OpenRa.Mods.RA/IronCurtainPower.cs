﻿using System.Collections.Generic;
using System.Linq;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public override object Create(Actor self) { return new IronCurtainPower(self, this); }
	}

	class IronCurtainPower : SupportPower, IResolveOrder
	{
		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, "ironchg1.aud"); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "ironrdy1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IronCurtain")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();

				var curtain = self.World.Queries.WithTrait<IronCurtain>()
					.Where(a => a.Actor.Owner != null)
					.FirstOrDefault().Actor;
				if (curtain != null)
					curtain.traits.Get<RenderBuilding>().PlayCustomAnim(curtain, "active");

				Sound.Play("ironcur9.aud");
				
				order.TargetActor.traits.Get<IronCurtainable>().Activate(order.TargetActor,
					(int)((Info as IronCurtainPowerInfo).Duration * 25 * 60));
				
				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public SelectTarget() {	}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					Game.controller.CancelInputMode();

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var underCursor = world.FindUnitsAtMouse(mi.Location)
						.Where(a => a.Owner != null
							&& a.traits.Contains<IronCurtainable>()
							&& a.traits.Contains<Selectable>()).FirstOrDefault();

					if (underCursor != null)
						yield return new Order("IronCurtain", underCursor.Owner.PlayerActor, underCursor);
				}

				yield break;
			}

			public void Tick(World world)
			{
				var hasStructure = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<IronCurtain>()
					.Any();

				if (!hasStructure)
					Game.controller.CancelInputMode();
			}

			public void Render(World world) { }

			public Cursor GetCursor(World world, int2 xy, MouseInput mi)
			{
				mi.Button = MouseButton.Left;
				return OrderInner(world, xy, mi).Any()
					? Cursor.Ability : Cursor.MoveBlocked;
			}
		}
	}

	// tag trait for the building
	class IronCurtainInfo : StatelessTraitInfo<IronCurtain> { }
	class IronCurtain { }
}
