using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public class Terrain
	{
		public readonly int Width;
		public readonly int Height;
		public readonly int XDim;
		public readonly int YDim;
		public readonly int NumTiles;

		readonly byte[] index;
		readonly List<Bitmap> TileBitmaps = new List<Bitmap>();
		public readonly List<byte[]> TileBitmapBytes = new List<byte[]>();

		public Terrain( Stream stream, Palette pal )
		{
			BinaryReader reader = new BinaryReader( stream );
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();

			if( Width != 24 || Height != 24 )
				throw new InvalidDataException( string.Format( "{0}x{1}", Width, Height ) );

			NumTiles = reader.ReadUInt16();
			reader.ReadUInt16();
			XDim = reader.ReadUInt16();
			YDim = reader.ReadUInt16();
			uint FileSize = reader.ReadUInt32();
			uint ImgStart = reader.ReadUInt32();
			reader.ReadUInt32();
			reader.ReadUInt32();
			int IndexEnd = reader.ReadInt32();
			reader.ReadUInt32();
			int IndexStart = reader.ReadInt32();

			stream.Position = IndexStart;
			index = new byte[ IndexEnd - IndexStart ];
			stream.Read( index, 0, IndexEnd - IndexStart );

			for( int i = 0 ; i < index.Length ; i++ )
			{
				if (index[i] != 255)
				{
					byte[] tileData = new byte[24 * 24];
					stream.Position = ImgStart + index[i] * 24 * 24;
					stream.Read(tileData, 0, 24 * 24);
					TileBitmaps.Add(BitmapBuilder.FromBytes(tileData, new Size(24, 24), pal));
					TileBitmapBytes.Add(tileData);
				}
				else
				{
					TileBitmaps.Add(null);
					TileBitmapBytes.Add(null);
				}
			}
		}

		public Bitmap GetTile( int index )
		{
			if( index < TileBitmaps.Count )
				return TileBitmaps[ index ];
			else
				return null;
		}

		public Bitmap[ , ] GetTiles( int tileNum )
		{
			int startIndex = tileNum * XDim * YDim;
			Bitmap[ , ] ret = new Bitmap[ XDim, YDim ];

			for( int x = 0 ; x < XDim ; x++ )
				for( int y = 0 ; y < YDim ; y++ )
					ret[ x, y ] = GetTile( startIndex + x + XDim * y );

			return ret;
		}
	}
}
