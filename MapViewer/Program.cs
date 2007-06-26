using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using OpenRa.FileFormats;

namespace MapViewer
{
	class Program
	{
		static Stream GetFile()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.RestoreDirectory = true;
			ofd.Filter = "Map files (*.ini)|*.ini";

			return (DialogResult.OK == ofd.ShowDialog()) ? ofd.OpenFile() : null;
		}

		static void Main(string[] args)
		{
			Stream s = GetFile();
			if (s == null)
			{
				Console.WriteLine("Fail");
				return;
			}

			IniFile iniFile = new IniFile(s);
			Console.WriteLine("Done.");

			IniSection basic = iniFile.GetSection("Basic");
			Console.WriteLine("Name: {0}", basic.GetValue("Name", "(null)"));
			Console.WriteLine("Official: {0}", basic.GetValue("Official", "no"));

			IniSection map = iniFile.GetSection("Map");
			Console.WriteLine("Theater: {0}", map.GetValue("Theater", "TEMPERATE"));
			Console.WriteLine("X: {0} Y: {1} Width: {2} Height: {3}",
				map.GetValue("X", "0"), map.GetValue("Y", "0"),
				map.GetValue("Width", "0"), map.GetValue("Height", "0"));

			int width = int.Parse(map.GetValue("Width", "0"));
			int height = int.Parse(map.GetValue("Height", "0"));

			int x = int.Parse(map.GetValue("X", "0"));
			int y = int.Parse(map.GetValue("Y", "0"));

			// parse MapPack section
			IniSection mapPackSection = iniFile.GetSection("MapPack");

			StringBuilder sb = new StringBuilder();
			for (int i = 1; ; i++)
			{
				string line = mapPackSection.GetValue(i.ToString(), null);
				if (line == null)
					break;

				sb.Append(line.Trim());
			}

			byte[] data = Convert.FromBase64String(sb.ToString());
			Console.WriteLine("Format80 data: {0}", data.Length);

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
					Console.WriteLine("Chunk length: {0}", actualLength);
				}
			}
			catch (EndOfStreamException) { }

			MemoryStream ms = new MemoryStream();
			foreach (byte[] chunk in chunks)
				ms.Write(chunk, 0, chunk.Length);

			ms.Position = 0;

			TileReference[,] tiles = new TileReference[width, height];
			for( int i = 0; i < width; i++ )
				for (int j = 0; j < height; j++)
					tiles[i, j].tile = (ushort)((ms.ReadByte() << 8) | ms.ReadByte());

			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
					tiles[i, j].image = (byte)ms.ReadByte();

			foreach( TileReference r in tiles )
				Console.Write("{0:x4}.{1:x2} ", r.tile, r.image);
		}
	}

	struct TileReference
	{
		public ushort tile;
		public byte image;
	}
}
