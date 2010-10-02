﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OpenRA.Widgets
{
	class ScrollingTextWidget : Widget
	{
		public string Text = "";
		public string Background = null;

		public bool Bold = false;

		public int ScrollLength = 200;

		// ticks per single letter scroll
		public int ScrollRate = 4;

		private string ScrollBuffer = "";

		private int ScrollLocation = 0;
		private int ScrollTick = 0;

		public Func<string> GetText;
		public Func<string> GetBackground;

		public ScrollingTextWidget()
			: base()
		{
			GetText = () => Text;
			GetBackground = () => Background;
		}

		protected ScrollingTextWidget(ScrollingTextWidget other)
			: base(other)
		{
			Text = other.Text;
			GetText = other.GetText;
			Bold = other.Bold;
			GetBackground = other.GetBackground;
		}

		public override void Tick(World world)
		{
			UpdateScrollBuffer();
		}

		private void UpdateScrollBuffer()
		{
			ScrollTick++;

			if (ScrollTick < ScrollRate)
			{
				return;
			}

			ScrollTick = 0;
			ScrollBuffer = "";

			if (Text.Substring(Text.Length - 4, 3) != "   ")
			{
				Text += "   ";
			}

			int tempScrollLocation = ScrollLocation;
			for (int i = 0; i < ScrollLength; ++i)
			{
				ScrollBuffer += Text.Substring(tempScrollLocation, 1);
				tempScrollLocation = (tempScrollLocation + 1) % Text.Length;
			}

			ScrollLocation = (ScrollLocation + 1) % Text.Length;
		}
		
		public override void DrawInner(World world)
		{
			var bg = GetBackground();

			if (bg != null)
				WidgetUtils.DrawPanel(bg, RenderBounds);

			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var text = GetText();
			if (text == null)
				return;

			int2 textSize = font.Measure(text);
			int2 position = RenderOrigin + new int2(0, (Bounds.Height - textSize.Y) / 2);

			Game.Renderer.EnableScissor(position.X, position.Y, Bounds.Width, Bounds.Height);
			font.DrawText(ScrollBuffer, position, Color.White);
			Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new ScrollingTextWidget(this); }
	}
}
