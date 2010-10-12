#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class TimerWidget : Widget
	{
		public Stopwatch Stopwatch;
		
		public TimerWidget ()
		{
			IsVisible = () => Game.Settings.Game.MatchTimer;
		}

		public override void DrawInner( WorldRenderer wr )
		{
			var s = WorldUtils.FormatTime(Game.LocalTick);
			var size = Game.Renderer.TitleFont.Measure(s);
			Game.Renderer.TitleFont.DrawText(s, new float2(RenderBounds.Left - size.X / 2, RenderBounds.Top - 20), Color.White);
		}
	}
}

