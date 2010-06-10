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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Net;
using System.IO.Compression;
using System.IO;

namespace OpenRA
{
	static class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			// brutal hack
			Application.CurrentCulture = CultureInfo.InvariantCulture;

			if (Debugger.IsAttached)
			{
				Run(args);
				return;
			}

			try
			{
				Run( args );
			}
			catch( Exception e )
			{
				Log.AddChannel("exception", "openra.exception.txt", true, false);
				Log.Write("exception", "{0}", e.ToString());
				Log.Upload(Game.GetGameId());
				throw;
			}
		}

		static void Run( string[] args )
		{
			Game.Initialize( new Settings( args ) );
			Game.Run();
		}
	}
}