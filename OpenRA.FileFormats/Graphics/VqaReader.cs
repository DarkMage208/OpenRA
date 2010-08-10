#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;

namespace OpenRA.FileFormats
{
	public class VqaReader
	{
		Stream stream;
		ushort flags;
		ushort numFrames;
		ushort width;
		ushort height;
		UInt32[] frames;
		
		public VqaReader( Stream stream )
		{
			this.stream = stream;
			BinaryReader reader = new BinaryReader( stream );
			// Decode FORM chunk
			if (new String(reader.ReadChars(4)) != "FORM")
				throw new InvalidDataException("Invalid vqa (invalid FORM section)");
			
			var fileBTF = reader.ReadUInt32();
			
			if (new String(reader.ReadChars(8)) != "WVQAVQHD")
				throw new InvalidDataException("Invalid vqa (not WVQAVQHD)");
			
			var rStartPos = reader.ReadUInt32();
			var version = reader.ReadUInt16();
			flags = reader.ReadUInt16();
			numFrames = reader.ReadUInt16();
			width = reader.ReadUInt16();
			height = reader.ReadUInt16();
			
			var blockWidth = reader.ReadByte();
			var blockHeight = reader.ReadByte();
			var framerate = reader.ReadByte();
			var cbParts = reader.ReadByte();
			
			var colors = reader.ReadUInt16();
			var maxBlocks = reader.ReadUInt16();
			/*var unknown1 = */reader.ReadUInt16();
			/*var unknown2 = */reader.ReadUInt32();
			
			// Audio?
			var freq = reader.ReadUInt16();
			var channels = reader.ReadByte();
			var bits = reader.ReadByte();
			
			/*var unknown3 = */reader.ReadChars(14);
			
			Console.WriteLine("FORM Info");
			Console.WriteLine("\tVersion: {0}",version);
			Console.WriteLine("\tFlags: {0}",flags);
			Console.WriteLine("\tFrames: {0}",numFrames);
			Console.WriteLine("\tFramerate: {0}",framerate);
			Console.WriteLine("\tSize: {0}x{1}",width,height);
			Console.WriteLine("\tBlocksize: {0}x{1}",blockWidth,blockHeight);
			Console.WriteLine("\tAudio: {0}hz, {1} channel(s), {2} bit",freq, channels, bits);

			// Decode FINF chunk
			if (new String(reader.ReadChars(4)) != "FINF")
				throw new InvalidDataException("Invalid vqa (invalid FINF section)");
			
			/*var offset = */reader.ReadUInt16();
			/*var unknown4 = */reader.ReadUInt16();
			
			// Frame offsets
			frames = new UInt32[numFrames];
			for (int i = 0; i < numFrames; i++)
			{
				frames[i] = reader.ReadUInt32();
				if (frames[i] > 0x40000000) frames[i] -= 0x40000000;
				frames[i] <<= 1;
			}
						
			while(true)
			{
				
				// Chunks are aligned on even bytes; may be padded with a single null
				if (reader.PeekChar() == 0) reader.ReadByte();
				
				var type = new String(reader.ReadChars(4));
				
				Console.WriteLine("Parsing chunk {0}@{1}",type, reader.BaseStream.Position-4);
				switch(type)
				{
					case "SND2":
						DecodeSND2(reader);
					break;
					case "VQFR":
					return;
					default: 
						throw new InvalidDataException("Unknown section {0}".F(type));
				}
			}
		}
		
		public void DecodeSND2(BinaryReader reader)
		{
			int chunkLength = (int)Swap(reader.ReadUInt32());
			
			// Don't do anything with this data (yet)
			reader.ReadBytes(chunkLength);
		}
		
		public UInt32 Swap(UInt32 orig)
		{
			return (UInt32)((orig & 0xff000000) >> 24) | ((orig & 0x00ff0000) >> 8) | ((orig & 0x0000ff00) << 8) | ((orig & 0x000000ff) << 24);
		}		
	}
}
