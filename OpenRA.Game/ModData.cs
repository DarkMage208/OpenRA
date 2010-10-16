﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA
{
	public class ModData
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;
		public readonly SheetBuilder SheetBuilder;
		public readonly CursorSheetBuilder CursorSheetBuilder;
		public readonly Dictionary<string, MapStub> AvailableMaps;
		public readonly WidgetLoader WidgetLoader;
		public ILoadScreen LoadScreen = null;
		
		public ModData( params string[] mods )
		{		
			Manifest = new Manifest( mods );
			ObjectCreator = new ObjectCreator( Manifest );
			LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen);
			LoadScreen.Init();
			LoadScreen.Display();
			
			FileSystem.LoadFromManifest( Manifest );
			ChromeProvider.Initialize( Manifest.Chrome );
			SheetBuilder = new SheetBuilder( TextureChannel.Red );
			CursorSheetBuilder = new CursorSheetBuilder( this );
			AvailableMaps = FindMaps( mods );
			WidgetLoader = new WidgetLoader( this );
		}
		
		// TODO: Do this nicer
		Dictionary<string, MapStub> FindMaps(string[] mods)
		{
			var paths = new[] { "maps/" }.Concat(mods.Select(m => "mods/" + m + "/maps/"))
				.Where(p => Directory.Exists(p))
				.SelectMany(p => Directory.GetDirectories(p)).ToList();

			return paths.Select(p => new MapStub(new Folder(p))).ToDictionary(m => m.Uid);
		}
		
		string cachedTheatre = null;
		public Map PrepareMap(string uid)
		{
			LoadScreen.Display();

			if (!AvailableMaps.ContainsKey(uid))
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));
			
			var map = new Map(AvailableMaps[uid].Package);
			
			Rules.LoadRules(Manifest, map);
			if (map.Theater != cachedTheatre)
			{
				SpriteSheetBuilder.Initialize( Rules.TileSets[map.Tileset] );
				SequenceProvider.Initialize(Manifest.Sequences);
				CursorProvider.Initialize(Manifest.Cursors);
				cachedTheatre = map.Theater;
			}
			return map;
		}
	}
	
	public interface ILoadScreen { void Display(); void Init(); }
}
