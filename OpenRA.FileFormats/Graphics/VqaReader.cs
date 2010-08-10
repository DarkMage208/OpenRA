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
using System.Drawing.Imaging;

namespace OpenRA.FileFormats
{
	public class VqaReader
	{
		Stream stream;
		ushort flags;
		ushort numFrames;
		ushort numColors;
		ushort width;
		ushort height;
		ushort blockWidth;
		ushort blockHeight;
		byte cbParts;
		int2 blocks;
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
			
			blockWidth = reader.ReadByte();
			blockHeight = reader.ReadByte();
			var framerate = reader.ReadByte();
			cbParts = reader.ReadByte();
			blocks = new int2(width / blockWidth, height / blockHeight);
			
			numColors = reader.ReadUInt16();
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
				if (reader.BaseStream.Length - reader.BaseStream.Position < 4)
					break;
				
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
						DecodeVQFR(reader);
					break;
					default: 
						throw new InvalidDataException("Unknown chunk {0}".F(type));
				}
			}
		}
		
		// VQA Sound sample
		public void DecodeSND2(BinaryReader reader)
		{
			int chunkLength = (int)Swap(reader.ReadUInt32());
			
			// Don't do anything with this data (yet)
			reader.ReadBytes(chunkLength);
		}
		
		// VQA Frame
		public void DecodeVQFR(BinaryReader reader)
		{
			int chunkLength = (int)Swap(reader.ReadUInt32());
			
			// Pixel table
			byte[] cbf = new byte[8*blocks.X*blocks.Y];
			
			List<byte> newcbfFormat80 = new List<byte>();			
			int cbpCount = 0;
			
			Color[] palette = new Color[numColors];
			
			while(true)
			{				
				// Chunks are aligned on even bytes; may be padded with a single null
				if (reader.PeekChar() == 0) reader.ReadByte();
				var type = new String(reader.ReadChars(4));
				int subchunkLength = (int)Swap(reader.ReadUInt32());

				Console.WriteLine("Parsing VQRF sub-chunk {0}@{1}",type, reader.BaseStream.Position-4);
				switch(type)
				{
					// Full compressed frame-modifier
					case "CBFZ":
						Format80.DecodeInto( reader.ReadBytes(subchunkLength), cbf );
					break;
					
					// Partial compressed frame-modifier
					case "CBPZ":
						var bytes = reader.ReadBytes(subchunkLength);
						foreach (var b in bytes) newcbfFormat80.Add(b);
						if (++cbpCount == cbParts) // Update the frame-modifier
							Format80.DecodeInto( newcbfFormat80.ToArray(), cbf );
					break;
					
					// Palette
					case "CPL0":
						Bitmap pal = new Bitmap(1,numColors);
						for (int i = 0; i < numColors; i++)
						{
							byte r = reader.ReadByte();
							byte g = reader.ReadByte();
							byte b = reader.ReadByte();
							palette[i] = Color.FromArgb(255,(r & 63) * 255 / 63, (g & 63) * 255 / 63, (b & 63) * 255 / 63);
							var p = palette[i];
							Console.WriteLine("{0} {1} {2}", p.R, p.G,p.B);
							pal.SetPixel(1,i,palette[i]);	
						}
						pal.Save("palette.bmp");
					break;
					
					// Frame data
					case "VPTZ":
						var framedata = new byte[2*blocks.X*blocks.Y];
						Format80.DecodeInto( reader.ReadBytes(subchunkLength), framedata );
						
					
						
						
						// Vomit the frame data to file
						Bitmap foo = new Bitmap(width, height);
						
						var bitmapData = foo.LockBits(new Rectangle(0, 0, width, height),
							ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
						
						unsafe
						{
							int* c = (int*)bitmapData.Scan0;
			
							for (var x = 0; x < blocks.X; x++)
								for (var y = 0; y < blocks.Y; y++)
									if (framedata[x + y*blocks.X + blocks.Y*blocks.X] == 0x0f)
										for (var xx = x*blockWidth; xx < (x+1)*blockWidth; xx++)
											for (var yy = y*blockHeight; yy < (y+1)*blockHeight; yy++)
												*(c + (yy * bitmapData.Stride >> 2) + xx) = palette[framedata[x + y*blocks.X]].ToArgb();
								
						}
						foo.UnlockBits(bitmapData);
						foo.Save("test.bmp");
						throw new InvalidDataException("foo");
						// This is the last subchunk
						return;
					default:
						throw new InvalidDataException("Unknown sub-chunk {0}".F(type));
				}
			}
		}
		
		// Change endianness of a uint32
		public UInt32 Swap(UInt32 orig)
		{
			return (UInt32)((orig & 0xff000000) >> 24) | ((orig & 0x00ff0000) >> 8) | ((orig & 0x0000ff00) << 8) | ((orig & 0x000000ff) << 24);
		}		
	}
}
