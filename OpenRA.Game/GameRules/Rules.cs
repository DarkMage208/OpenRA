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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA
{
	public static class Rules
	{
		public static TechTree TechTree;

		public static Dictionary<string, ActorInfo> Info;
		public static Dictionary<string, WeaponInfo> Weapons;
		public static Dictionary<string, VoiceInfo> Voices;
		public static Dictionary<string, MusicInfo> Music;
		public static Dictionary<string, string> Movies;
		public static Dictionary<string, TileSet> TileSets;

		public static void LoadRules(Manifest m, Map map)
		{
			Info = LoadYamlRules(m.Rules, map.Rules, (k, y) => new ActorInfo(k.Key.ToLowerInvariant(), k.Value, y));
			Weapons = LoadYamlRules(m.Weapons, map.Weapons, (k, _) => new WeaponInfo(k.Key.ToLowerInvariant(), k.Value));
			Voices = LoadYamlRules(m.Voices, map.Voices, (k, _) => new VoiceInfo(k.Value));
			Music = LoadYamlRules(m.Music, map.Music, (k, _) => new MusicInfo(k.Key, k.Value));
			Movies = LoadYamlRules(m.Movies, new Dictionary<string,MiniYaml>(), (k, v) => k.Value.Value);
			
			TileSets = new Dictionary<string, TileSet>();
			foreach (var file in m.TileSets)
			{
				var t = new TileSet(file);
				TileSets.Add(t.Id,t);
			}
			
			TechTree = new TechTree();
		}
		
		static Dictionary<string, T> LoadYamlRules<T>(string[] files, Dictionary<string,MiniYaml>dict, Func<KeyValuePair<string, MiniYaml>, Dictionary<string, MiniYaml>, T> f)
		{
			var y = files.Select(a => MiniYaml.FromFile(a)).Aggregate(dict,MiniYaml.Merge);
			return y.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => f(kv, y));
		}
	}
}
