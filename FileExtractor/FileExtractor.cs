#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.FileFormats;

namespace FileExtractor
{
	public class FileExtractor
	{
		int Length = 256;
		
		public FileExtractor (string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine("usage: FileExtractor mod[,mod]* filename");
				return;
			}

			var mods = args[0].Split(',');
			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest( manifest );
			
			try
			{
				var readStream = FileSystem.Open(args[1]);
				var writeStream = new FileStream(args[1], FileMode.OpenOrCreate, FileAccess.Write);
				
				WriteOutFile(readStream, writeStream);
				
			} 
			catch (FileNotFoundException) 
			{
				Console.WriteLine(String.Format("No Such File {0}", args[1]));
			}
		}
		
		void WriteOutFile (Stream readStream, Stream writeStream)
		{
   			Byte[] buffer = new Byte[Length];
   			int bytesRead = readStream.Read(buffer,0,Length);

 			while( bytesRead > 0 ) 
    		{
        		writeStream.Write(buffer,0,bytesRead);
        		bytesRead = readStream.Read(buffer,0,Length);
    		}
    		readStream.Close();
    		writeStream.Close();
		}
	}
}

