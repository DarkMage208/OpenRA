using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

namespace OpenRa.FileFormats
{
	public class TileSet
	{
		public readonly Dictionary<ushort, Terrain> tiles = new Dictionary<ushort, Terrain>();
		public readonly Package MixFile;

		public TileSet( Package mixFile, string suffix )
		{
			MixFile = mixFile;
			StreamReader tileIdFile = File.OpenText( "../../../tileSet.til" );

			while( true )
			{
				string countStr = tileIdFile.ReadLine();
				string startStr = tileIdFile.ReadLine();
				string pattern = tileIdFile.ReadLine() + suffix;
				if( countStr == null || startStr == null || pattern == null )
					break;

				int count = int.Parse( countStr );
				int start = int.Parse( startStr, NumberStyles.HexNumber );
				for( int i = 0 ; i < count ; i++ )
				{
					try
					{
						Stream s = mixFile.GetContent(string.Format(pattern, i + 1));
						if (!tiles.ContainsKey((ushort)(start + i)))
							tiles.Add((ushort)(start + i), new Terrain(s));
					}
					catch { }
				}
			}

			tileIdFile.Close();
		}

		public byte[] GetBytes(TileReference r) { return tiles[r.tile].TileBitmapBytes[r.image]; }
	}
}
