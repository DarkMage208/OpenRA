#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;

namespace OpenRA.Widgets
{
	[Flags]
	public enum ScrollDirection
	{
		None = 0,
		Up = 1,
		Left = 2,
		Down = 4,
		Right = 8
	}
	
	class ViewportScrollControllerWidget : Widget
	{
		public int EdgeScrollThreshold = 15;

		ScrollDirection Keyboard;
		ScrollDirection Edge;

		public ViewportScrollControllerWidget() : base() { }
		protected ViewportScrollControllerWidget(ViewportScrollControllerWidget widget) : base(widget) {}
		public override void DrawInner( World world ) {}
		
		public override bool HandleInputInner(MouseInput mi)
		{									
			if (mi.Event == MouseInputEvent.Move &&
				(mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right)))
			{
				Game.viewport.Scroll(Widget.LastMousePos - mi.Location);
				return true;
			}
			return false;
		}
		
		public override string GetCursor(int2 pos)
		{
			if (!Game.Settings.ViewportEdgeScroll)
				return null;
			
			if (Edge.Includes(ScrollDirection.Up) && Edge.Includes(ScrollDirection.Left))
				return "scroll-tl";
			if (Edge.Includes(ScrollDirection.Up) && Edge.Includes(ScrollDirection.Right))
				return "scroll-tr";
			if (Edge.Includes(ScrollDirection.Down) && Edge.Includes(ScrollDirection.Left))
				return "scroll-bl";
			if (Edge.Includes(ScrollDirection.Down) && Edge.Includes(ScrollDirection.Right))
				return "scroll-br";
			
			if (Edge.Includes(ScrollDirection.Up))
				return "scroll-t";
			if (Edge.Includes(ScrollDirection.Down))
				return "scroll-b";
			if (Edge.Includes(ScrollDirection.Left))
				return "scroll-l";
			if (Edge.Includes(ScrollDirection.Right))
				return "scroll-r";
			
			return null;
		}

		public override bool LoseFocus (MouseInput mi)
		{
			Keyboard = ScrollDirection.None;
			return base.LoseFocus(mi);
		}
		
		public override bool HandleKeyPressInner(KeyInput e)
		{			
			switch (e.KeyName)
			{
				case "up": Keyboard = Keyboard.Set(ScrollDirection.Up, (e.Event == KeyInputEvent.Down)); return true;
				case "down": Keyboard = Keyboard.Set(ScrollDirection.Down, (e.Event == KeyInputEvent.Down)); return true;
				case "left": Keyboard = Keyboard.Set(ScrollDirection.Left, (e.Event == KeyInputEvent.Down)); return true;
				case "right": Keyboard = Keyboard.Set(ScrollDirection.Right, (e.Event == KeyInputEvent.Down)); return true;
			}
			return false;
		}
		
		public override void Tick(World world)
		{
			Edge = ScrollDirection.None;
			if (Game.Settings.ViewportEdgeScroll)
			{
				// Check for edge-scroll
				if (Widget.LastMousePos.X < EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Left, true);
				if (Widget.LastMousePos.Y < EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Up, true);
				if (Widget.LastMousePos.X >= Game.viewport.Width - EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Right, true);
				if (Widget.LastMousePos.Y >= Game.viewport.Height - EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Down, true);
			}
			var scroll = new float2(0,0);
			if (Keyboard.Includes(ScrollDirection.Up) || Edge.Includes(ScrollDirection.Up))
				scroll += new float2(0, -10);
			if (Keyboard.Includes(ScrollDirection.Right) || Edge.Includes(ScrollDirection.Right))
				scroll += new float2(10, 0);
			if (Keyboard.Includes(ScrollDirection.Down) || Edge.Includes(ScrollDirection.Down))
				scroll += new float2(0, 10);
			if (Keyboard.Includes(ScrollDirection.Left) || Edge.Includes(ScrollDirection.Left))
				scroll += new float2(-10, 0);
			
			Game.viewport.Scroll(scroll);
		}
		
		public override Widget Clone() { return new ViewportScrollControllerWidget(this); }
	}
	
	public static class ViewportExts
	{	
		public static bool Includes(this ScrollDirection d, ScrollDirection s)
		{
			return (d & s) == s;
		}
		
		public static ScrollDirection Set(this ScrollDirection d, ScrollDirection s, bool val)
		{
			return (d.Includes(s) != val) ? d ^ s : d;
		}
	}
}