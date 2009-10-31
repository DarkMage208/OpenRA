﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IjwFramework.Types;
using System.Drawing;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class Controller
	{
		public IOrderGenerator orderGenerator;

		List<Order> recentOrders = new List<Order>();

		void ApplyOrders(float2 xy, bool left)
		{
			var doVoice = null as Actor;
			if (orderGenerator != null)
				foreach (var order in orderGenerator.Order(xy.ToInt2(), left))
				{
					recentOrders.Add(order);
					if (order.Subject != null && order.Player == Game.LocalPlayer)
						doVoice = order.Subject;
				}
			if (doVoice != null)
				Game.PlaySound(Game.SovietVoices.First.GetNext() + GetVoiceSuffix(doVoice), false);
		}

		public void AddOrder(Order o) { recentOrders.Add(o); }

		public List<Order> GetRecentOrders()
		{
			var ret = recentOrders;
			recentOrders = new List<Order>();
			return ret;
		}

		static string GetVoiceSuffix(Actor unit)
		{
			var suffixes = new[] { ".r01", ".r03" };
			return suffixes[unit.traits.Get<Traits.Mobile>().Voice];
		}

		float2 dragStart, dragEnd;
		public void HandleMouseInput(MouseInput mi)
		{
			var xy = Game.viewport.ViewToWorld(mi);

			if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!(orderGenerator is PlaceBuilding))
					dragStart = dragEnd = xy;
				ApplyOrders(xy, true);
			}

			if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Up)
			{
				if (!(orderGenerator is PlaceBuilding))
				{
					if (dragStart != xy)
						orderGenerator = new UnitOrderGenerator(
							Game.SelectUnitsInBox(Game.CellSize * dragStart, Game.CellSize * xy));
					else
						orderGenerator = new UnitOrderGenerator(
							Game.SelectUnitOrBuilding(Game.CellSize * xy));
				}

				dragStart = dragEnd = xy;
			}

			if (mi.Button == MouseButtons.None && mi.Event == MouseInputEvent.Move)
			{
				/* update the cursor to reflect the thing under us - note this 
				 * needs to also happen when the *thing* changes, so per-frame hook */
				dragStart = dragEnd = xy;
			}

			if (mi.Button == MouseButtons.Right && mi.Event == MouseInputEvent.Down)
				ApplyOrders(xy, false);
		}

		public Pair<float2, float2>? SelectionBox
		{
			get
			{
				if (dragStart == dragEnd) return null;
				return Pair.New(Game.CellSize * dragStart, Game.CellSize * dragEnd);
			}
		}

		public float2 MousePosition { get { return dragEnd; } }

		public Cursor ChooseCursor()
		{
			var c = (orderGenerator is UnitOrderGenerator) ? orderGenerator.Order(dragEnd.ToInt2(), false)
				.Select(a => a.Cursor)
				.FirstOrDefault(a => a != null) : null;

			return c ?? (Game.SelectUnitOrBuilding(Game.CellSize * dragEnd).Any() ? Cursor.Select : Cursor.Default);
		}
	}
}
