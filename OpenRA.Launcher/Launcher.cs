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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO.Pipes;
using System.IO;

namespace OpenRA.Launcher
{
	public partial class Launcher : Form
	{
		Dictionary<string, Mod> allMods;
		public static string Renderer = "Gl";
		static string SupportDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "OpenRA";

		public Launcher()
		{
			InitializeComponent();

			Util.UacShield(installButton);

			webBrowser.ObjectForScripting = new JSBridge();
			webBrowser.DocumentCompleted += (o, e) =>
				{
					var b = o as WebBrowser;
					(b.ObjectForScripting as JSBridge).Document = b.Document;
				};
			RefreshMods();
			string response = UtilityProgram.CallSimpleResponse("--settings-value", SupportDir, "Graphics.Renderer");
			if (Util.IsError(ref response) || response.Equals("gl", StringComparison.InvariantCultureIgnoreCase))
				glButton.Checked = true;
			else
				cgButton.Checked = true;
		}

		Mod GetMetadata(string mod)
		{
			string responseString = UtilityProgram.CallSimpleResponse("-i", mod);
			
			if (Util.IsError(ref responseString)) return null;
			string[] lines = responseString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Length; i++)
				lines[i] = lines[i].Trim('\r');

			string title = "", version = "", author = "", description = "", requires = "";
			bool standalone = false;
			foreach (string line in lines)
			{
				string s = line.Trim(' ', '\r', '\n');
				int i = s.IndexOf(':');
				if (i + 2 > s.Length) continue;
				string value = s.Substring(i + 2);
				switch (s.Substring(0, i))
				{
					case "Title":
						title = value;
						break;
					case "Version":
						version = value;
						break;
					case "Author":
						author = value;
						break;
					case "Description":
						description = value;
						break;
					case "Requires":
						requires = value;
						break;
					case "Standalone":
						standalone = bool.Parse(value);
						break;
					default:
						break;
				}
			}

			return new Mod(title, version, author, description, requires, standalone);
		}

		void RefreshMods()
		{
			string responseString = UtilityProgram.CallSimpleResponse("--list-mods");

			string[] mods;
			if (!Util.IsError(ref responseString))
				mods = responseString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			else
				throw new Exception(string.Format("Could not list mods: {0}", responseString));
			
			for (int i = 0; i < mods.Length; i++)
				mods[i] = mods[i].Trim('\r');

			allMods = mods.ToDictionary(x => x, x => GetMetadata(x));

			(webBrowser.ObjectForScripting as JSBridge).AllMods = allMods;

			RefreshModTree(treeView, allMods.Keys.ToArray());
		}

		private void InstallMod(object sender, EventArgs e)
		{
			if (installModDialog.ShowDialog() != DialogResult.OK) return;
			string pipename = UtilityProgram.GetPipeName();
			var p = UtilityProgram.CallWithAdmin("--extract-zip", pipename, installModDialog.FileName, "");
			var pipe = new NamedPipeClientStream(".", pipename, PipeDirection.In);
			pipe.Connect();

			p.WaitForExit();

			using (var response = new StreamReader(pipe))
			{
				response.ReadToEnd();
			}

			RefreshMods();
		}

		void RefreshModTree(TreeView treeView, string[] modList)
		{
			treeView.Nodes["ModsNode"].Nodes.Clear();
			Dictionary<string, TreeNode> nodes;
			nodes = modList.Where(x => allMods[x].Standalone).ToDictionary(x => x, 
				x => new TreeNode(allMods[x].Title) { Name = x });
			string[] rootMods = modList.Where(x => allMods[x].Standalone).ToArray();
			Stack<string> remaining = new Stack<string>(modList.Except(nodes.Keys));

			bool progress = true;
			while (remaining.Count > 0 && progress)
			{
				progress = false;
				string s = remaining.Pop();
				var n = new TreeNode(allMods[s].Title) { Name = s };
				if (allMods[s].Requires == null) { remaining.Push(s); continue; }
				if (!nodes.ContainsKey(allMods[s].Requires)) { remaining.Push(s); continue; }
				nodes[allMods[s].Requires].Nodes.Add(n);
				nodes.Add(s, n);
				progress = true;
			}

			foreach (string s in rootMods)
				treeView.Nodes["ModsNode"].Nodes.Add(nodes[s]);

			if (remaining.Count > 0)
			{
				var unspecified = new TreeNode("<Unspecified Dependency>") { ForeColor = SystemColors.GrayText };
				var missing = new TreeNode("<Missing Dependency>") { ForeColor = SystemColors.GrayText };

				foreach (var s in remaining)
				{
					if (allMods[s].Requires == null)
						unspecified.Nodes.Add(new TreeNode(allMods[s].Title) 
						{ ForeColor = SystemColors.GrayText, Name = s });
					else if (!nodes.ContainsKey(allMods[s].Requires))
						missing.Nodes.Add(new TreeNode(allMods[s].Title) 
						{ ForeColor = SystemColors.GrayText, Name = s });
				}
				string brokenKey = "BrokenModsNode";
				if (treeView.Nodes[brokenKey] != null)
					treeView.Nodes.RemoveByKey(brokenKey);
				treeView.Nodes.Add(brokenKey, "Broken Mods");
				treeView.Nodes[brokenKey].Nodes.Add(unspecified);
				treeView.Nodes[brokenKey].Nodes.Add(missing);
			}
			treeView.Nodes["ModsNode"].ExpandAll();
			treeView.Invalidate();

			string responseString = UtilityProgram.CallSimpleResponse("--settings-value", SupportDir, "Game.Mods");

			if (Util.IsError(ref responseString))
				treeView.SelectedNode = treeView.Nodes["ModsNode"].Nodes["ra"];
			else
				treeView.SelectedNode = treeView.Nodes["ModsNode"].Nodes[responseString];
		}

		void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			Mod selectedMod;
			if (!allMods.TryGetValue(e.Node.Name, out selectedMod)) return;
			string modHtmlPath = string.Format("mods{0}{1}{0}mod.html", Path.DirectorySeparatorChar, e.Node.Name);
			if (!File.Exists(modHtmlPath)) return;
			webBrowser.Navigate(Path.GetFullPath(modHtmlPath));
		}

		private void rendererChanged(object sender, EventArgs e)
		{
			if (sender == glButton)
				Renderer = "Gl";
			else
				Renderer = "Cg";
		}
		
		void formClosing(object sender, FormClosingEventArgs e)
		{
			
		}
	}
}
