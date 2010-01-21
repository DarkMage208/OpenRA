﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.GameRules;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class SellOrderGenerator : IOrderGenerator
	{
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
				var loc = mi.Location + Game.viewport.Location;
				var underCursor = world.FindUnits(loc, loc)
					.Where(a => a.Owner == world.LocalPlayer
						&& a.traits.Contains<Building>()
						&& a.traits.Contains<Selectable>()).FirstOrDefault();

				var building = underCursor != null ? underCursor.Info.Traits.Get<BuildingInfo>() : null;

				if (building != null && !building.Unsellable)
					yield return new Order("Sell", underCursor, null, int2.Zero, null);
			}
		}

		public void Tick( World world ) {}
		public void Render( World world ) {}

		public Cursor GetCursor(World world, int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, xy, mi).Any()
				? Cursor.Sell : Cursor.SellBlocked;
		}
	}
}
