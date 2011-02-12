#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA
{
	public class NullLoadScreen : ILoadScreen
	{
		public void Init() {}
		public void Display()
		{
			if (Game.Renderer == null)
				return;
			
			// Draw a black screen
			Game.Renderer.BeginFrame(float2.Zero);
			Game.Renderer.EndFrame( new NullInputHandler() );
		}
	}
}

