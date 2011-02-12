#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class MusicInfo
	{
		public readonly string Filename = null;
		public readonly string Title = null;
		public readonly int Length = 0; // seconds
		public readonly bool Exists = false;

		public MusicInfo( string key, MiniYaml value )
		{
			Filename = key+".aud";
			Title = value.Value;

			if (!FileSystem.Exists(Filename))
				return;
			
			Exists = true;
			Length = (int)AudLoader.SoundLength(FileSystem.Open(Filename));
		}
	}
}
