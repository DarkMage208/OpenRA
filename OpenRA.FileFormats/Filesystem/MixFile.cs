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
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	public interface IFolder
	{
		Stream GetContent(string filename);
		bool Exists(string filename);
		IEnumerable<uint> AllFileHashes();
		int Priority { get; }
	}

	public class MixFile : IFolder
	{
		readonly Dictionary<uint, PackageEntry> index;
		readonly bool isRmix, isEncrypted;
		readonly long dataStart;
		readonly Stream s;
		int priority;

		public MixFile(string filename, int priority)
		{
			this.priority = priority;
			s = FileSystem.Open(filename);

			BinaryReader reader = new BinaryReader(s);
			uint signature = reader.ReadUInt32();

			isRmix = 0 == (signature & ~(uint)(MixFileFlags.Checksum | MixFileFlags.Encrypted));

			if (isRmix)
			{
				isEncrypted = 0 != (signature & (uint)MixFileFlags.Encrypted);
				if( isEncrypted )
				{
					index = ParseRaHeader( s, out dataStart ).ToDictionary(x => x.Hash);
					return;
				}
			}
			else
				s.Seek( 0, SeekOrigin.Begin );

			isEncrypted = false;
			index = ParseTdHeader(s, out dataStart).ToDictionary(x => x.Hash);
		}

		const long headerStart = 84;

		List<PackageEntry> ParseRaHeader(Stream s, out long dataStart)
		{
			BinaryReader reader = new BinaryReader(s);
			byte[] keyblock = reader.ReadBytes(80);
			byte[] blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);

			uint[] h = ReadUints(reader, 2);

			Blowfish fish = new Blowfish(blowfishKey);
			MemoryStream ms = Decrypt( h, fish );
			BinaryReader reader2 = new BinaryReader(ms);

			ushort numFiles = reader2.ReadUInt16();
			reader2.ReadUInt32(); /*datasize*/

			s.Position = headerStart;
			reader = new BinaryReader(s);

			int byteCount = 6 + numFiles * PackageEntry.Size;
			h = ReadUints( reader, ( byteCount + 3 ) / 4 );

			ms = Decrypt( h, fish );

			dataStart = headerStart + byteCount + ( ( ~byteCount + 1 ) & 7 );

			long ds;
			return ParseTdHeader( ms, out ds );
		}

		static MemoryStream Decrypt( uint[] h, Blowfish fish )
		{
			uint[] decrypted = fish.Decrypt( h );

			MemoryStream ms = new MemoryStream();
			BinaryWriter writer = new BinaryWriter( ms );
			foreach( uint t in decrypted )
				writer.Write( t );
			writer.Flush();

			ms.Position = 0;
			return ms;
		}

		uint[] ReadUints(BinaryReader r, int count)
		{
			uint[] ret = new uint[count];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = r.ReadUInt32();

			return ret;
		}

		List<PackageEntry> ParseTdHeader(Stream s, out long dataStart)
		{
			List<PackageEntry> items = new List<PackageEntry>();

			BinaryReader reader = new BinaryReader(s);
			ushort numFiles = reader.ReadUInt16();
			/*uint dataSize = */reader.ReadUInt32();

			for (int i = 0; i < numFiles; i++)
				items.Add(new PackageEntry(reader));

			dataStart = s.Position;
			return items;
		}
		
		public Stream GetContent(uint hash)
		{
			PackageEntry e;
			if (!index.TryGetValue(hash, out e))
				return null;

			s.Seek( dataStart + e.Offset, SeekOrigin.Begin );
			byte[] data = new byte[ e.Length ];
			s.Read( data, 0, (int)e.Length );
			return new MemoryStream(data);
		}

		public Stream GetContent(string filename)
		{
			return GetContent(PackageEntry.HashFilename(filename));
		}

		public IEnumerable<uint> AllFileHashes()
		{
			return index.Keys;
		}
		
		public bool Exists(string filename)
		{
			return index.ContainsKey(PackageEntry.HashFilename(filename));
		}


		public int Priority
		{
			get { return 1000 + priority; }
		}
	}

	[Flags]
	enum MixFileFlags : uint
	{
		Checksum = 0x10000,
		Encrypted = 0x20000,
	}
}
