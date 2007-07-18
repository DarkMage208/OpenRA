using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public class Map
	{
		public readonly string Title;
		public readonly string Theater;

		public readonly int XOffset;
		public readonly int YOffset;

		public readonly int Width;
		public readonly int Height;

		public PointF Size { get { return new PointF(Width, Height); } }

		public readonly TileReference[ , ] MapTiles = new TileReference[ 128, 128 ];
		public readonly List<TreeReference> Trees = new List<TreeReference>();

		static string Truncate( string s, int maxLength )
		{
			return s.Length <= maxLength ? s : s.Substring(0,maxLength );
		}

		public string TileSuffix { get { return "." + Truncate(Theater, 3); } }

		public Map(IniFile file)
		{
			IniSection basic = file.GetSection("Basic");
			Title = basic.GetValue("Name", "(null)");

			IniSection map = file.GetSection("Map");
			Theater = Truncate(map.GetValue("Theater", "TEMPERATE"), 8);

			XOffset = int.Parse(map.GetValue("X", "0"));
			YOffset = int.Parse(map.GetValue("Y", "0"));

			Width = int.Parse(map.GetValue("Width", "0"));
			Height = int.Parse(map.GetValue("Height", "0"));

			UnpackTileData(ReadMapPack(file));
			ReadTrees(file);
		}

		static MemoryStream ReadMapPack(IniFile file)
		{
			IniSection mapPackSection = file.GetSection("MapPack");

			StringBuilder sb = new StringBuilder();
			for (int i = 1; ; i++)
			{
				string line = mapPackSection.GetValue(i.ToString(), null);
				if (line == null)
					break;

				sb.Append(line.Trim());
			}

			byte[] data = Convert.FromBase64String(sb.ToString());

			List<byte[]> chunks = new List<byte[]>();

			BinaryReader reader = new BinaryReader(new MemoryStream(data));

			try
			{
				while (true)
				{
					uint length = reader.ReadUInt32() & 0xdfffffff;
					byte[] dest = new byte[8192];
					byte[] src = reader.ReadBytes((int)length);

					int actualLength = Format80.DecodeInto(new MemoryStream(src), dest);

					chunks.Add(dest);
				}
			}
			catch (EndOfStreamException) { }

			MemoryStream ms = new MemoryStream();
			foreach (byte[] chunk in chunks)
				ms.Write(chunk, 0, chunk.Length);

			ms.Position = 0;

			return ms;
		}

		static byte ReadByte( Stream s )
		{
			int ret = s.ReadByte();
			if( ret == -1 )
				throw new NotImplementedException();
			return (byte)ret;
		}

		void UnpackTileData( MemoryStream ms )
		{
			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
				{
					MapTiles[ j, i ].tile = ReadByte( ms );
					MapTiles[ j, i ].tile |= (ushort)( ReadByte( ms ) << 8 );
				}

			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
				{
					MapTiles[ j, i ].image = ReadByte( ms );
					if( MapTiles[ j, i ].tile == 0xff || MapTiles[ j, i ].tile == 0xffff )
						MapTiles[ j, i ].image = (byte)( i % 4 + ( j % 4 ) * 4 );
				}
		}

		void ReadTrees( IniFile file )
		{
			IniSection terrain = file.GetSection( "TERRAIN" );
			if( terrain == null )
				return;

			foreach( KeyValuePair<string, string> kv in terrain )
				Trees.Add( new TreeReference( int.Parse( kv.Key ), kv.Value ) );
		}

		public bool IsInMap(int x, int y)
		{
			return (x >= XOffset && y >= YOffset && x < XOffset + Width && y < YOffset + Height);
		}
	}

	public struct TileReference
	{
		public ushort tile;
		public byte image;

		public override int GetHashCode() { return tile.GetHashCode() ^ image.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			TileReference r = (TileReference)obj;
			return (r.image == image && r.tile == tile);
		}

		public static bool operator ==(TileReference a, TileReference b) { return a.Equals(b); }
		public static bool operator !=(TileReference a, TileReference b) { return !a.Equals(b); }
	}

	public struct TreeReference
	{
		public readonly int X;
		public readonly int Y;
		public readonly string Image;

		public TreeReference(int xy, string image)
		{
			X = xy % 128;
			Y = xy / 128;
			Image = image;
		}

		public Point Location { get { return new Point(X, Y); } }
	}
}
