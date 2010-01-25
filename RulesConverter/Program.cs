﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using System.IO;
using IjwFramework.Types;

namespace RulesConverter
{
	using PL = Dictionary<string, string>;

	class Program
	{
		static void Main(string[] args)
		{
			FileSystem.Mount(new Folder("./"));

			var ruleStreams = args
				.Where(a => a.EndsWith(".ini"))
				.Select(a => FileSystem.Open(a)).ToArray();

			var rules = new IniFile(ruleStreams);

			var outputFile = args.Single(a => !a.EndsWith(".ini"));

			var categoryMap = new Dictionary<string,Pair<string,string>>
			{
				{ "VehicleTypes", Pair.New( "^Vehicle", "Vehicle" ) },
				{ "ShipTypes", Pair.New( "^Ship", "Ship" ) },
				{ "PlaneTypes", Pair.New( "^Plane", "Plane" ) },
				{ "BuildingTypes", Pair.New( "^Building", "Building" ) },
				{ "InfantryTypes", Pair.New( "^Infantry", "Infantry" ) },
			};

			var traitMap = new Dictionary<string, PL>
			{
				{ "Unit", new PL {	
					{ "HP", "Strength" }, 
					{ "Armor", "Armor" }, 
					{ "Crewed", "Crewed" },
					{ "InitialFacing", "InitialFacing" },
					{ "ROT", "ROT" },
					{ "Sight", "Sight" },
					{ "Speed", "Speed" },
					{ "WaterBound", "WaterBound" } }
				},

				{ "Selectable", new PL {
					{ "Priority", "SelectionPriority" },
					{ "Voice", "Voice" },
					{ "Bounds", "SelectionSize" } } 
				},

				{ "Mobile", new PL {
					{ "MovementType", "$MovementType" } }
				},

				{ "RenderBuilding", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderUnitSpinner", new PL {
					{ "Image", "Image" },
					{ "Offset", "PrimaryOffset" } }
				},

				{ "RenderUnitRotor", new PL {
					{ "Image", "Image" },
					{ "PrimaryOffset", "RotorOffset" },
					{ "SecondaryOffset", "RotorOffset2" } }
				},

				{ "Buildable", new PL {
					{ "TechLevel", "TechLevel" },
					{ "Prerequisites", "Prerequisite" },
					{ "BuiltAt", "BuiltAt" },
					{ "Owner", "Owner" },
					{ "Cost", "Cost" },
					{ "Icon", "Icon" },
					{ "Description", "Description" },
					{ "LongDesc", "LongDesc" },
					{ "AlternateName", "AlternateName" } }
				},

				{ "Cargo", new PL { 
					{ "PassengerTypes", "PassengerTypes" },
					{ "Passengers", "Passengers" },
					{ "UnloadFacing", "UnloadFacing" } }
				},

				{ "LimitedAmmo", new PL {
					{ "Ammo", "Ammo" } }
				},

				{ "Building", new PL {
					{ "Power", "Power" },
					{ "Footprint", "Footprint" },
					{ "Dimensions", "Dimensions" },
					{ "Capturable", "Capturable" },
					{ "Repairable",  "Repairable" }, 
					{ "BaseNormal", "BaseNormal" },
					{ "Adjacent", "Adjacent" },
					{ "Bib", "Bib" },
					{ "HP", "Strength" }, 
					{ "Armor", "Armor" }, 
					{ "Crewed", "Crewed" },
					{ "WaterBound", "WaterBound" },
					{ "Sight", "Sight" },
					{ "Unsellable", "Unsellable" } }
				},

				{ "StoresOre", new PL {
					{ "Pips", "OrePips" },
					{ "Capacity", "Storage" } }
				},

				{ "Harvester", new PL {
					{ "Pips", "OrePips" } }
					//{ "Capacity"
				},

				{ "AttackBase", new PL {
					{ "PrimaryWeapon", "Primary" },
					{ "SecondaryWeapon", "Secondary" },
					{ "PrimaryOffset", "PrimaryOffset" },
					{ "SecondaryOffset", "SecondaryOffset" },
					{ "PrimaryLocalOffset", "PrimaryLocalOffset" },
					{ "SecondaryLocalOffset", "SecondaryLocalOffset" },
					{ "MuzzleFlash", "MuzzleFlash" },		// maybe
					{ "Recoil", "Recoil"},
					{ "FireDelay", "FireDelay" } }
				},

				{ "Production", new PL {
					{ "SpawnOffset", "SpawnOffset" },
					{ "Produces", "Produces" } }
				},

				{ "ProductionSurround", new PL {
					{ "Produces", "Produces" } }
				},

				{ "Minelayer", new PL {
					{ "Mine", "Primary" } }
				},

				{ "Turreted", new PL {
					{ "ROT", "ROT" },
					{ "InitialFacing", "InitialFacing" } }
				},
			};

			traitMap["RenderUnit"] = traitMap["RenderBuilding"];
			traitMap["RenderBuildingCharge"] = traitMap["RenderBuilding"];
			traitMap["RenderBuildingOre"] = traitMap["RenderBuilding"];
			traitMap["RenderBuildingTurreted"] = traitMap["RenderBuilding"];
			traitMap["RenderInfantry"] = traitMap["RenderBuilding"];
			traitMap["RenderUnitMuzzleFlash"] = traitMap["RenderBuilding"];
			traitMap["RenderUnitReload"] = traitMap["RenderBuilding"];
			traitMap["RenderUnitTurreted"] = traitMap["RenderBuilding"];

			traitMap["AttackTurreted"] = traitMap["AttackBase"];
			traitMap["AttackPlane"] = traitMap["AttackBase"];
			traitMap["AttackHeli"] = traitMap["AttackBase"];

			using (var writer = File.CreateText(outputFile))
			{
				foreach (var cat in categoryMap)
					try
					{
						foreach (var item in rules.GetSection(cat.Key).Select(a => a.Key))
						{
							var iniSection = rules.GetSection(item);
							writer.WriteLine("{0}:", item);
							writer.WriteLine("\tInherits: {0}", cat.Value.First);
							writer.WriteLine("\tCategory: {0}", cat.Value.Second);

							var traits = iniSection.GetValue("Traits", "")
								.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

							if (iniSection.GetValue("Selectable", "yes") == "yes")
								traits.Insert(0, "Selectable");

							if (iniSection.GetValue("TechLevel", "-1") != "-1")
								traits.Insert(0, "Buildable");

							foreach (var t in traits)
							{
								writer.WriteLine("\t{0}:", t);

								if (traitMap.ContainsKey(t))
									foreach (var kv in traitMap[t])
									{
										var v = iniSection.GetValue(kv.Value, "");
										if (kv.Value == "$Tab") v = cat.Value.Second;
										if (kv.Value == "$MovementType") v = GetMovementType(iniSection, traits);
										if (!string.IsNullOrEmpty(v)) writer.WriteLine("\t\t{0}: {1}", kv.Key, v);
									}
							}

							writer.WriteLine();
						}
					}
					catch { }
			}

			var yaml = MiniYaml.FromFile( outputFile );
			if( File.Exists( "merge-" + outputFile ) )
				yaml = MiniYaml.Merge( MiniYaml.FromFile( "merge-" + outputFile ), yaml );
			// A hack, but it works
			yaml.OptimizeInherits( MiniYaml.FromFile( "../ra/defaults.yaml" ) );
			yaml.WriteToFile( outputFile );
		}

		static string GetMovementType(IniSection unit, List<string> traits)
		{
			if (unit.GetValue("WaterBound", "no") == "yes")
				return "Float";
			if (unit.GetValue("Tracked", "no") == "yes")
				return "Track";
			if (traits.Contains("Plane") || traits.Contains("Helicopter"))
				return "Fly";
			if (traits.Contains("RenderInfantry") || traits.Contains("RenderSpy"))
				return "Foot";

			return "Wheel";
		}
	}
}
