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

using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileFormats
{
	public class Folder : IFolder
	{
		readonly string path;

		public Folder(string path) { this.path = path; }

		public Stream GetContent(string filename)
		{
			Log.Write( "GetContent from folder: {0}", filename );
			try { return File.OpenRead( Path.Combine( path, filename ) ); }
			catch { return null; }
		}

		public IEnumerable<uint> AllFileHashes()
		{
			foreach( var filename in Directory.GetFiles( path, "*", SearchOption.TopDirectoryOnly ) )
				yield return PackageEntry.HashFilename( filename );
		}
	}
}
