using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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

		public readonly TileReference[ , ] MapTiles = new TileReference[ 128, 128 ];

		public Map( IniFile file )
		{
			IniSection basic = file.GetSection( "Basic" );
			Title = basic.GetValue( "Name", "(null)" );

			IniSection map = file.GetSection( "Map" );
			Theater = map.GetValue( "Theater", "TEMPERATE" );

			XOffset = int.Parse( map.GetValue( "X", "0" ) );
			YOffset = int.Parse( map.GetValue( "Y", "0" ) );

			Width = int.Parse( map.GetValue( "Width", "0" ) );
			Height = int.Parse( map.GetValue( "Height", "0" ) );

			MemoryStream ms = ReadMapPack( file );

			UnpackTileData( ms );
		}

		static MemoryStream ReadMapPack( IniFile file )
		{
			IniSection mapPackSection = file.GetSection( "MapPack" );

			StringBuilder sb = new StringBuilder();
			for( int i = 1 ; ; i++ )
			{
				string line = mapPackSection.GetValue( i.ToString(), null );
				if( line == null )
					break;

				sb.Append( line.Trim() );
			}

			byte[] data = Convert.FromBase64String( sb.ToString() );
			Console.WriteLine( "Format80 data: {0}", data.Length );

			List<byte[]> chunks = new List<byte[]>();

			BinaryReader reader = new BinaryReader( new MemoryStream( data ) );

			try
			{
				while( true )
				{
					uint length = reader.ReadUInt32() & 0xdfffffff;
					byte[] dest = new byte[ 8192 ];
					byte[] src = reader.ReadBytes( (int)length );

					int actualLength = Format80.DecodeInto( new MemoryStream( src ), dest );

					chunks.Add( dest );
					Console.WriteLine( "Chunk length: {0}", actualLength );
				}
			}
			catch( EndOfStreamException ) { }

			MemoryStream ms = new MemoryStream();
			foreach( byte[] chunk in chunks )
				ms.Write( chunk, 0, chunk.Length );

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
			{
				for( int j = 0 ; j < 128 ; j++ )
				{
					MapTiles[ j, i ].tile = ReadByte( ms );
					MapTiles[ j, i ].tile |= (ushort)( ReadByte( ms ) << 8 );
				}
			}

			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
					MapTiles[ j, i ].image = ReadByte( ms );
		}
	}

	public struct TileReference
	{
		public ushort tile;
		public byte image;
	}
}
