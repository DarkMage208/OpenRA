#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Widgets;
using System;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class ProductionTab
	{
		public string Name;
		public ProductionQueue Queue;
	}

	public class ProductionTabGroup
	{
		public List<ProductionTab> Tabs = new List<ProductionTab>();
		public string Group;
		public int CumulativeCount;

		public void Update(IEnumerable<ProductionQueue> allQueues)
		{
			var queues = allQueues.Where(q => q.Info.Group == Group).ToList();
			List<ProductionTab> tabs = new List<ProductionTab>();

			// Remove stale queues
			foreach (var t in Tabs)
			{
				if (!queues.Contains(t.Queue))
					continue;

				tabs.Add(t);
				queues.Remove(t.Queue);
			}

			// Add new queues
			foreach (var queue in queues)
				tabs.Add(new ProductionTab()
				{
					Name = (++CumulativeCount).ToString(),
					Queue = queue
				});
			Tabs = tabs;
		}
	}

	class ProductionTabsWidget : Widget
	{
		string queueType;
		public string QueueType
		{
			get
			{
				return queueType;
			}
			set
			{
				queueType = value;
				ListOffset = 0;
				Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget)
					.CurrentQueue = Groups[queueType].Tabs[0].Queue;
			}
		}

		public string PaletteWidget = null;
		public float ScrollVelocity = 4f;
		public int TabWidth = 30;
		public int ArrowWidth = 20;
		public Dictionary<string, ProductionTabGroup> Groups;

		int ContentWidth = 0;
		float ListOffset = 0;
		bool leftPressed = false;
		bool rightPressed = false;
		Rectangle leftButtonRect;
		Rectangle rightButtonRect;
		readonly World world;

		[ObjectCreator.UseCtor]
		public ProductionTabsWidget( [ObjectCreator.Param] World world )
		{
			this.world = world;
			Groups = Rules.Info.Values.SelectMany(a => a.Traits.WithInterface<ProductionQueueInfo>())
				.Select(q => q.Group).Distinct().ToDictionary(g => g, g => new ProductionTabGroup() { Group = g });
		}
		
		public override void DrawInner()
		{
			var rb = RenderBounds;
			leftButtonRect = new Rectangle(rb.X, rb.Y, ArrowWidth, rb.Height);
			rightButtonRect = new Rectangle(rb.Right - ArrowWidth, rb.Y, ArrowWidth, rb.Height);

			var leftDisabled = ListOffset >= 0;
			var rightDisabled = ListOffset <= Bounds.Width - rightButtonRect.Width - leftButtonRect.Width - ContentWidth;

			WidgetUtils.DrawPanel("panel-black", rb);
			ButtonWidget.DrawBackground("button", leftButtonRect, leftDisabled,
			                            leftPressed, leftButtonRect.Contains(Viewport.LastMousePos));
			ButtonWidget.DrawBackground("button", rightButtonRect, rightDisabled,
			                            rightPressed, rightButtonRect.Contains(Viewport.LastMousePos));

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", leftPressed || leftDisabled ? "up_pressed" : "up_arrow"),
				new float2(leftButtonRect.Left + 2, leftButtonRect.Top + 2));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", rightPressed || rightDisabled ? "down_pressed" : "down_arrow"),
				new float2(rightButtonRect.Left + 2, rightButtonRect.Top + 2));

			if (QueueType == null)
				return;

			// Draw tab buttons
			Game.Renderer.EnableScissor(leftButtonRect.Right, rb.Y + 1, rightButtonRect.Left - leftButtonRect.Right - 1, rb.Height);
			var palette = Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget);
			var origin = new int2(leftButtonRect.Right - 1 + (int)ListOffset, leftButtonRect.Y);
			SpriteFont font = Game.Renderer.Fonts["TinyBold"];
			ContentWidth = 0;
			foreach (var tab in Groups[QueueType].Tabs)
			{
				var rect = new Rectangle(origin.X + ContentWidth, origin.Y, TabWidth, rb.Height);
				ButtonWidget.DrawBackground("button", rect, false, tab.Queue == palette.CurrentQueue, rect.Contains(Viewport.LastMousePos));
				ContentWidth += TabWidth - 1;

				int2 textSize = font.Measure(tab.Name);
				int2 position = new int2(rect.X + (rect.Width - textSize.X)/2, rect.Y + (rect.Height - textSize.Y)/2);
				font.DrawTextWithContrast(tab.Name, position, Color.White, Color.Black, 1);
			}

			Game.Renderer.DisableScissor();
		}

		void Scroll(int direction)
		{
			ListOffset += direction*ScrollVelocity;
			ListOffset = Math.Min(0,Math.Max(Bounds.Width - rightButtonRect.Width - leftButtonRect.Width - ContentWidth, ListOffset));
		}

		// Is added to world.ActorAdded by the SidebarLogic handler
		public void ActorChanged(Actor a)
		{
			if (a.HasTrait<ProductionQueue>())
			{
				var allQueues = world.ActorsWithTrait<ProductionQueue>()
					.Where(p => p.Actor.Owner == world.LocalPlayer && p.Actor.IsInWorld)
					.Select(p => p.Trait).ToArray();
				foreach (var g in Groups.Values)
					g.Update(allQueues);
			}
		}

		public override void Tick()
		{
			if (leftPressed) Scroll(1);
			if (rightPressed) Scroll(-1);
			base.Tick();
		}

		public override bool LoseFocus(MouseInput mi)
		{
			leftPressed = rightPressed = false;
			return base.LoseFocus(mi);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.WheelDown)
			{
				Scroll(-1);
				return true;
			}

			if (mi.Button == MouseButton.WheelUp)
			{
				Scroll(1);
				return true;
			}

			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			if (!Focused)
				return false;

			if (Focused && mi.Event == MouseInputEvent.Up)
				return LoseFocus(mi);

			leftPressed = leftButtonRect.Contains(mi.Location.X, mi.Location.Y);
			rightPressed = rightButtonRect.Contains(mi.Location.X, mi.Location.Y);

			// Check production tabs
			var offsetloc = mi.Location - new int2(leftButtonRect.Right - 1 + (int)ListOffset, leftButtonRect.Y);
			if (offsetloc.X > 0 && offsetloc.X <= ContentWidth)
			{
				var palette = Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget);
				palette.CurrentQueue = Groups[QueueType].Tabs[offsetloc.X/(TabWidth - 1)].Queue;
				return true;
			}

			return (leftPressed || rightPressed);
		}
	}
}
