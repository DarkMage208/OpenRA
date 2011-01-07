﻿#region Copyright & License Information
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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OpenRA.Launcher
{
	public class JSBridge
	{
		Dictionary<string, Mod> allMods = new Dictionary<string,Mod>();

		public Dictionary<string, Mod> AllMods
		{
			get { return allMods; }
			set { allMods = value; }
		}
		Dictionary<string, Download> downloads = new Dictionary<string,Download>();

		HtmlDocument document = null;

		public HtmlDocument Document
		{
			get { return document; }
			set { document = value; }
		}
		
		public bool existsInMod(string file, string mod)
		{
			string cleanedPath = CleanPath(file);
			return File.Exists(string.Format("mods{0}{1}{0}{2}", Path.DirectorySeparatorChar, mod, cleanedPath));
		}

		public void log(string message)
		{
			Console.WriteLine("js: " + message);
		}

		public bool launchMod(string mod)
		{
			string m = mod;
			List<string> modList = new List<string>();
			modList.Add(m);
			if (!allMods.ContainsKey(m))
			{
				System.Windows.Forms.MessageBox.Show("allMods does not contain " + m);
				return false;
			}
			while (!string.IsNullOrEmpty(allMods[m].Requires))
			{
				m = allMods[m].Requires;
				modList.Add(m);
			}

			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Game.exe";
			p.StartInfo.Arguments = "Game.Mods=" + string.Join(",", modList.ToArray());
			p.StartInfo.Arguments += " Graphics.Renderer=" + Launcher.Renderer;
			p.Start();
			return true;
		}

		Regex p = new Regex(@"\.\.[/\\]?");
		string CleanPath(string path)
		{
			string root = Path.GetPathRoot(path);
			string cleanedPath = path.Remove(0, root.Length);
			return p.Replace(cleanedPath, "");
		}

		public void registerDownload(string key, string url, string filename)
		{
			string cleanedPath = CleanPath(filename);
			if (!downloads.ContainsKey(key))
				downloads.Add(key, new Download(document, key, url, cleanedPath));
			else
				downloads[key] = new Download(document, key, url, cleanedPath);
		}

		public bool startDownload(string key)
		{
			if (!downloads.ContainsKey(key))
				return false;

			downloads[key].StartDownload();

			return true;
		}

		public bool cancelDownload(string key)
		{
			if (!downloads.ContainsKey(key))
				return false;

			downloads[key].CancelDownload();

			return true;
		}

		public string downloadStatus(string key)
		{
			if (!downloads.ContainsKey(key))
				return DownloadStatus.NOT_REGISTERED.ToString();

			return downloads[key].Status.ToString();
		}

		public string downloadError(string key)
		{
			if (!downloads.ContainsKey(key))
				return "";
			
			return downloads[key].ErrorMessage;
		}

		public int bytesCompleted(string key)
		{
			if (!downloads.ContainsKey(key))
				return -1;

			return downloads[key].BytesDone;
		}

		public int bytesTotal(string key)
		{
			if (!downloads.ContainsKey(key))
				return -1;

			return downloads[key].BytesTotal;
		}

		public bool extractDownload(string key, string targetDir, string mod)
		{
			string cleanedPath = CleanPath(targetDir);

			string targetPath = Path.Combine(mod, cleanedPath);

			if (!downloads.ContainsKey(key))
				return false;

			if (downloads[key].Status != DownloadStatus.DOWNLOADED)
				return false;

			downloads[key].ExtractDownload(targetPath);

			return true;
		}
		
		public string metadata(string field, string mod)
		{
			if (field == "VERSION")
				return allMods[mod].Version;
			
			return "";
		}

		public void httpRequest(string url, string callbackName)
		{
			string pipename = UtilityProgram.GetPipeName();
			var pipe = new NamedPipeClientStream(".", pipename, PipeDirection.In);

			var p = UtilityProgram.Call("--download-url", pipename, 
				(_, e) =>
				{
					
					using (var reader = new StreamReader(pipe))
					{
						var data = reader.ReadToEnd();
						document.InvokeScript(callbackName, new object[] { data });
					}
				}, url);
			
			pipe.Connect();
		}
	}
}
