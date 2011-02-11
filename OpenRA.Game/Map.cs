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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using OpenRA.FileFormats;
using System.Text;

namespace OpenRA
{
	public class Map : MapStub
	{
		// Yaml map data
		[FieldLoader.Load] public int MapFormat;

		public Dictionary<string, PlayerReference> Players = new Dictionary<string, PlayerReference>();
		public Dictionary<string, ActorReference> Actors = new Dictionary<string, ActorReference>();
		public List<SmudgeReference> Smudges = new List<SmudgeReference>();

		// Rules overrides
		public List<MiniYamlNode> Rules = new List<MiniYamlNode>();

		// Sequences overrides
		public List<MiniYamlNode> Sequences = new List<MiniYamlNode>();

		// Weapon overrides
		public List<MiniYamlNode> Weapons = new List<MiniYamlNode>();

		// Voices overrides
		public List<MiniYamlNode> Voices = new List<MiniYamlNode>();

		// Binary map data
		public byte TileFormat = 1;
		[FieldLoader.Load] public int2 MapSize;

		public TileReference<ushort, byte>[,] MapTiles;
		public TileReference<byte, byte>[,] MapResources;
		public string [,] CustomTerrain;

		public Map()
		{
			// Do nothing; not a valid map (editor hack)
		}
		
		public static Map FromTileset(string tileset)
		{
			var tile = OpenRA.Rules.TileSets[tileset].Templates.First();
			Map map = new Map()
			{
				Title = "Name your map here",
				Description = "Describe your map here",
				Author = "Your name here",
				MapSize = new int2(1, 1),
				PlayerCount = 0,
				Tileset = tileset,
				MapResources = new TileReference<byte, byte>[1, 1],
				MapTiles = new TileReference<ushort, byte>[1, 1] 
				{ { new TileReference<ushort, byte> { 
					type = tile.Key, 
					image = (byte)(tile.Value.PickAny ? 0xffu : 0), 
					index = (byte)(tile.Value.PickAny ? 0xffu : 0) }
				} },
			};
			
			return map;
		}

		class Format2ActorReference
		{
			public string Id = null;
			public string Type = null;
			public int2 Location = int2.Zero;
			public string Owner = null;
		}
		
		public Map(string path)
			: base(path)
		{
			var yaml = new MiniYaml( null, MiniYaml.FromStream( Container.GetContent( "map.yaml" ) ) );

			// 'Simple' metadata
			FieldLoader.Load( this, yaml );


			// Players & Actors -- this has changed several times.
			//	- Be backwards compatible wherever possible.
			//	- Loading a map then saving it out upgrades to latest.
			// Minimum criteria for dropping a format:
			//	- There are no maps of this format left in tree

			switch (MapFormat)
			{
				case 1:
					{
						Players.Add("Neutral", new PlayerReference("Neutral", "allies", true, true));

						int actors = 0;
						foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
						{
							string[] vals = kv.Value.Value.Split(' ');
							string[] loc = vals[2].Split(',');
							Actors.Add("Actor" + actors++, new ActorReference(vals[0])
							{
								new LocationInit( new int2( int.Parse( loc[ 0 ] ), int.Parse( loc[ 1 ] ) ) ),
								new OwnerInit( "Neutral" ),
							});
						}
					} break;

				case 2:
					{
						foreach (var kv in yaml.NodesDict["Players"].NodesDict)
						{
							var player = new PlayerReference(kv.Value);
							Players.Add(player.Name, player);
						}

						foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
						{
							var oldActorReference = FieldLoader.Load<Format2ActorReference>(kv.Value);
							Actors.Add(oldActorReference.Id, new ActorReference(oldActorReference.Type)
							{
								new LocationInit( oldActorReference.Location ),
								new OwnerInit( oldActorReference.Owner )
							});
						}
					} break;

				case 3:
                case 4:
				case 5:
					{
						foreach (var kv in yaml.NodesDict["Players"].NodesDict)
						{
							var player = new PlayerReference(kv.Value);
							Players.Add(player.Name, player);
						}

						foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
							Actors.Add(kv.Key, new ActorReference(kv.Value.Value, kv.Value.NodesDict));
					} break;

				default:
					throw new InvalidDataException("Map format {0} is not supported.".F(MapFormat));
			}

			/* hack: make some slots. */
			if (!Players.Any(p => p.Value.Playable))
			{
				for (int index = 0; index < Waypoints.Count; index++)
				{
					var p = new PlayerReference
					{
						Name = "Multi{0}".F(index),
						Race = "Random",
						Playable = true,
						DefaultStartingUnits = true,
						Enemies = new[]{"Creeps"}
					};
					Players.Add(p.Name, p);
				}
			}

            // Color1/Color2 -> ColorRamp
            if (MapFormat < 4)
                foreach (var mp in Players)
                    mp.Value.ColorRamp = new ColorRamp(
                        (byte)((mp.Value.Color.GetHue() / 360.0f) * 255),
                        (byte)(mp.Value.Color.GetSaturation() * 255),
                        (byte)(mp.Value.Color.GetBrightness() * 255),
                        (byte)(mp.Value.Color2.GetBrightness() * 255));
			
			
			// Creep player / Required Mod
			if (MapFormat < 5)
			{
				RequiresMod = Game.CurrentMods.Keys.First();
				
				foreach (var mp in Players.Where(p => !p.Value.NonCombatant && !p.Value.Enemies.Contains("Creeps")))
					mp.Value.Enemies = mp.Value.Enemies.Concat(new[] {"Creeps"}).ToArray();
				
				Players.Add("Creeps", new PlayerReference
				{
					Name = "Creeps",
					Race = "Random",
					NonCombatant = true,
					Enemies = Players.Keys.Where(k => k != "Neutral").ToArray()
				});
			}
			
			// Smudges
			foreach (var kv in yaml.NodesDict["Smudges"].NodesDict)
			{
				string[] vals = kv.Key.Split(' ');
				string[] loc = vals[1].Split(',');
				Smudges.Add(new SmudgeReference(vals[0], new int2(int.Parse(loc[0]), int.Parse(loc[1])), int.Parse(vals[2])));
			}

			// Rules
			Rules = yaml.NodesDict["Rules"].Nodes;

			// Sequences
			Sequences = (yaml.NodesDict.ContainsKey("Sequences")) ? yaml.NodesDict["Sequences"].Nodes : new List<MiniYamlNode>();

			// Weapons
			Weapons = (yaml.NodesDict.ContainsKey("Weapons")) ? yaml.NodesDict["Weapons"].Nodes : new List<MiniYamlNode>();
			
			// Voices
			Voices = (yaml.NodesDict.ContainsKey("Voices")) ? yaml.NodesDict["Voices"].Nodes : new List<MiniYamlNode>();

			CustomTerrain = new string[MapSize.X, MapSize.Y];			
			LoadBinaryData();
		}

		public void Save(string toPath)
		{			
			MapFormat = 5;
			
			var root = new List<MiniYamlNode>();
			foreach (var field in new string[] {"Selectable", "MapFormat", "RequiresMod", "Title", "Description", "Author", "PlayerCount", "Tileset", "MapSize", "TopLeft", "BottomRight", "UseAsShellmap", "Type"})
			{
				var f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				root.Add( new MiniYamlNode( field, FieldSaver.FormatValue( this, f ) ) );
			}

			root.Add( new MiniYamlNode( "Players", null,
				Players.Select( p => new MiniYamlNode(
					"PlayerReference@{0}".F( p.Key ),
					FieldSaver.Save( p.Value ) ) ).ToList() ) );

			root.Add( new MiniYamlNode( "Actors", null,
				Actors.Select( x => new MiniYamlNode(
					x.Key,
					x.Value.Save() ) ).ToList() ) );

			root.Add(new MiniYamlNode("Waypoints", MiniYaml.FromDictionary<string, int2>( Waypoints )));
			root.Add(new MiniYamlNode("Smudges", MiniYaml.FromList<SmudgeReference>( Smudges )));
			root.Add(new MiniYamlNode("Rules", null, Rules));
			root.Add(new MiniYamlNode("Sequences", null, Sequences));
			root.Add(new MiniYamlNode("Weapons", null, Weapons));
			root.Add(new MiniYamlNode("Voices", null, Voices));
			
			Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
			entries.Add("map.bin", SaveBinaryData());
			var s = root.WriteToString();
			entries.Add("map.yaml", Encoding.UTF8.GetBytes(s));
			
			// Saving the map to a new location
			if (toPath != Path)
			{
				Path = toPath;
				
				// Create a new map package
				// TODO: Add other files (resources, rules) to the entries list
				Container = FileSystem.CreatePackage(Path, int.MaxValue, entries);
			}
			
			// Update existing package
			Container.Write(entries);
		}

		static byte ReadByte(Stream s)
		{
			int ret = s.ReadByte();
			if (ret == -1)
				throw new NotImplementedException();
			return (byte)ret;
		}

		static ushort ReadWord(Stream s)
		{
			ushort ret = ReadByte(s);
			ret |= (ushort)(ReadByte(s) << 8);

			return ret;
		}

		public void LoadBinaryData()
		{
			using (var dataStream = Container.GetContent("map.bin"))
			{
				if (ReadByte(dataStream) != 1)
					throw new InvalidDataException("Unknown binary map format");

				// Load header info
				var width = ReadWord(dataStream);
				var height = ReadWord(dataStream);

				if (width != MapSize.X || height != MapSize.Y)
					throw new InvalidDataException("Invalid tile data");

				MapTiles = new TileReference<ushort, byte>[MapSize.X, MapSize.Y];
				MapResources = new TileReference<byte, byte>[MapSize.X, MapSize.Y];

				// Load tile data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						ushort tile = ReadWord(dataStream);
						byte index = ReadByte(dataStream);
						byte image = (index == byte.MaxValue) ? (byte)(i % 4 + (j % 4) * 4) : index;
						MapTiles[i, j] = new TileReference<ushort, byte>(tile, index, image);
					}

				// Load resource data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
						MapResources[i, j] = new TileReference<byte, byte>(ReadByte(dataStream), ReadByte(dataStream));
			}
		}

		public byte[] SaveBinaryData()
		{
			MemoryStream dataStream = new MemoryStream();
			using (var writer = new BinaryWriter(dataStream))
			{
				// File header consists of a version byte, followed by 2 ushorts for width and height
				writer.Write(TileFormat);
				writer.Write((ushort)MapSize.X);
				writer.Write((ushort)MapSize.Y);

				// Tile data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						writer.Write(MapTiles[i, j].type);
						writer.Write(MapTiles[i, j].index);
					}

				// Resource data	
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						writer.Write(MapResources[i, j].type);
						writer.Write(MapResources[i, j].index);
					}
			}
			return dataStream.ToArray();
		}

		public bool IsInMap(int2 xy)
		{
			return IsInMap(xy.X, xy.Y);
		}

		public bool IsInMap(int x, int y)
		{
			return Bounds.Contains(x,y);
		}

		static T[,] ResizeArray<T>(T[,] ts, T t, int width, int height)
		{
			var result = new T[width, height];
			for (var i = 0; i < width; i++)
				for (var j = 0; j < height; j++)
					result[i, j] = i <= ts.GetUpperBound(0) && j <= ts.GetUpperBound(1)
						? ts[i, j] : t;
			return result;
		}

		public void Resize(int width, int height)		// editor magic.
		{
			MapTiles = ResizeArray(MapTiles, MapTiles[0, 0], width, height);
			MapResources = ResizeArray(MapResources, MapResources[0, 0], width, height);
			MapSize = new int2(width, height);
		}
		
		public void ResizeCordon(int left, int top, int right, int bottom)
		{
			TopLeft = new int2(left, top);
			BottomRight = new int2(right, bottom);
			Bounds = Rectangle.FromLTRB(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y);
		}
	}
}
