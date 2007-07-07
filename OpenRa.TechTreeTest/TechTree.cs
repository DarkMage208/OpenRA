using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenRa.TechTreeTest
{
	class TechTree
	{
		Dictionary<string, Building> buildings = new Dictionary<string,Building>();
		List<string> built;
		public TechTree()
		{
			LoadBuildings();
			LoadRules();
		}

		void LoadRules()
		{
			IniFile rulesFile;
			rulesFile = new IniFile(File.OpenRead("rules.ini"));
			foreach (string key in buildings.Keys)
			{
				IniSection section = rulesFile.GetSection(key);
				Building b = buildings[key];
				string s = section.GetValue("Prerequisite", "").ToUpper();
				b.Prerequisites = s.Split(',');
				b.TechLevel = int.Parse(section.GetValue("TechLevel", "-1"));
			}
		}

		void LoadBuildings()
		{
			foreach (string line in File.ReadAllLines("buildings.txt"))
			{
				Regex pattern = new Regex(@"^(\w+),([\w ]+)$");
				Match m = pattern.Match(line);
				if (!m.Success) continue;
				buildings.Add(m.Groups[0].Value, new Building(m.Groups[1].Value));
			}
		}

		public bool Build(string key)
		{
			Building b = buildings[key];
			if (!b.Buildable) return false;
			built.Add(key);
			CheckAll();
			return true;
		}

		public bool Unbuild(string key)
		{
			Building b = buildings[key];
			if (!built.Contains(key)) return false;
			built.Remove(key);
			CheckAll();
			return true;
		}

		void CheckAll()
		{
			foreach (Building building in buildings.Values)
			{
				building.CheckPrerequisites(built);
			}
		}
	}
}
