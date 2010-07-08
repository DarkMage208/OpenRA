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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Orders
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order( World world, int2 xy, MouseInput mi )
		{
			var orders = Game.controller.selection.Actors
				.Select(a => a.Order(xy, mi))
				.Where(o => o != null)
				.ToArray();

			var actorsInvolved = orders.Select(o => o.Subject).Distinct();
			if (actorsInvolved.Any())
				yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor,
					string.Join(",", actorsInvolved.Select(a => a.ActorID.ToString()).ToArray()));

			foreach (var o in orders)
				yield return o;
		}

		public void Tick( World world ) {}

		public void Render( World world )
		{
			foreach (var a in Game.controller.selection.Actors)
			{
				world.WorldRenderer.DrawSelectionBox(a, Color.White, true);
				if (a.Owner == world.LocalPlayer)
				{
					//if (a.traits.Contains<RenderRangeCircle>())
					//    world.WorldRenderer.DrawRangeCircle(Color.FromArgb(128, Color.Yellow),
					//        a.CenterLocation, (int)a.GetPrimaryWeapon().Range);

					if (a.traits.Contains<DetectCloaked>())
						world.WorldRenderer.DrawRangeCircle(Color.FromArgb(128, Color.LimeGreen),
							a.CenterLocation, a.Info.Traits.Get<DetectCloakedInfo>().Range);
				}
			}
		}

		public string GetCursor( World world, int2 xy, MouseInput mi )
		{
			return ChooseCursor(world, mi);
		}

		string ChooseCursor(World world, MouseInput mi)
		{
			//using (new PerfSample("cursor"))
			{
				var p = Game.controller.MousePosition;
				var c = Order(world, p.ToInt2(), mi)
					.Select(o => o.Subject.traits.WithInterface<IProvideCursor>()
						.Select(pc => pc.CursorForOrderString(o.OrderString, o.Subject, o.TargetLocation)).FirstOrDefault(a => a != null))
					.FirstOrDefault(a => a != null);

				return c ??
					(world.SelectActorsInBox(Game.CellSize * p,
					Game.CellSize * p).Any()
						? "select" : "default");
			}
		}
	}
}
