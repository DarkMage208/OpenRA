#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenRA.FileFormats
{
	public class NewMap
	{
		// General info
		public byte MapFormat = 1;
		public string Title;
		public string Description;
		public string Author;
		public int PlayerCount;
		public string Preview;
		
		// 'Simple' map data
		public string Tiledata;
		public byte TileFormat = 1;
		public string Tileset;
		public int2 Size;
		public int[] Bounds;
		
		// 'Complex' map data	
		public TileReference[ , ] MapTiles;
		public Dictionary<string, ActorReference> Actors = new Dictionary<string, ActorReference>();
		public Dictionary<string, int2> Waypoints = new Dictionary<string, int2>();
		public Dictionary<string, MiniYaml> Rules = new Dictionary<string, MiniYaml>();
		
		List<string> SimpleFields = new List<string>() {
			"MapFormat", "Title", "Description", "Author", "PlayerCount", "Tileset", "Size", "Tiledata", "Preview", "Bounds"
		};
		
		public NewMap(string filename)
		{			
			var yaml = MiniYaml.FromFile(filename);
			
			// 'Simple' metadata
			foreach (var field in SimpleFields)
			{
				if (!yaml.ContainsKey(field)) continue;
				FieldLoader.LoadField(this,field,yaml[field].Value);
			}
			
			// Waypoints
			foreach (var wp in yaml["Waypoints"].Nodes)
			{
				string[] loc = wp.Value.Value.Split(',');
				Waypoints.Add(wp.Key, new int2(int.Parse(loc[0]),int.Parse(loc[1])));
			}
			
			// TODO: Players
			
			// Actors
			foreach (var kv in yaml["Actors"].Nodes.ToPairs())
			{
				string[] vals = kv.Second.Split(' ');
				string[] loc = vals[2].Split(',');
				var a = new ActorReference(vals[0], new int2(int.Parse(loc[0]),int.Parse(loc[1])) ,vals[1]);
				Actors.Add(kv.First,a);
			}
			
			// Rules
			Rules = yaml["Rules"].Nodes;
		}
		
		
		public void SaveBinaryData(string filepath)
		{
			
			FileStream dataStream = new FileStream(filepath+".tmp", FileMode.Create, FileAccess.Write);
			BinaryWriter writer = new BinaryWriter( dataStream );
			writer.BaseStream.Seek(0, SeekOrigin.Begin);
			
			// File header consists of a version byte, followed by 2 ushorts for width and height
			writer.Write(TileFormat);
			writer.Write((ushort)Size.X);
			writer.Write((ushort)Size.Y);
			
			// Tile data is stored as a base-64 encoded stream of
			// {(2-byte) tile index, (1-byte) image index} pairs
			for( int i = 0 ; i < Size.X ; i++ )
				for( int j = 0 ; j < Size.Y ; j++ )
				{			
					writer.Write( MapTiles[j,i].tile );
					// Semi-hack: Convert clear and water tiles to "pick an image for me" magic number
					byte image = (MapTiles[ j, i ].tile == 0xff || MapTiles[ j, i ].tile == 0xffff) ? byte.MaxValue : MapTiles[j,i].image;
					writer.Write(image);
				}
			
			
			// TODO: Need a proper resources array to write
			/*
			// Resource data is stored as a base-64 encoded stream of
			// {(1-byte) resource index, (1-byte) image index} pairs			
			for( int i = 0 ; i < Size.X ; i++ )
				for( int j = 0 ; j < Size.Y ; j++ )
				{			
					byte type = 0;
					byte image = 0;
					if (MapTiles[j,i].overlay != null)
					{
						var res = resourceMapping[MapTiles[j,i].overlay];
						type = res.First;
						image = res.Second;
					}
					
					writer.Write(type);
					writer.Write(image);
				}
			*/
			writer.Flush();
		}
		
		public void DebugContents()
		{
			foreach (var field in SimpleFields)
				Console.WriteLine("Loaded {0}: {1}", field, this.GetType().GetField(field).GetValue(this));
			
			Console.WriteLine("Loaded Waypoints:");
			foreach (var wp in Waypoints)
				Console.WriteLine("\t{0} => {1}",wp.Key,wp.Value);
			
			Console.WriteLine("Loaded Actors:");
			foreach (var wp in Actors)
				Console.WriteLine("\t{0} => {1} {2} {3}",wp.Key,wp.Value.Name, wp.Value.Owner,wp.Value.Location);
		}
	}
}
