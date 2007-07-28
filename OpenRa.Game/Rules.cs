using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	static class Rules
	{
		static readonly Dictionary<string, UnitInfo> unitInfos = new Dictionary<string, UnitInfo>();

		static Rules()
		{
			IniFile rulesIni = new IniFile(FileSystem.Open("rules.ini"));

			foreach (string line in Util.ReadAllLines(FileSystem.Open("units.txt")))
			{
				string unit = line.Substring(0, line.IndexOf(','));
				IniSection section = rulesIni.GetSection(unit.ToUpperInvariant());
				if (section == null)
				{
					Log.Write("rules.ini doesnt contain entry for unit \"{0}\"", unit);
					continue;
				}
				unitInfos.Add(unit, new UnitInfo(section));
			}
		}

		public static UnitInfo UnitInfo( string name )
		{
			return unitInfos[ name.ToUpperInvariant() ];
		}
	}

	class UnitInfo
	{
		public readonly int Speed;

		public UnitInfo( IniSection ini )
		{
			Speed = int.Parse( ini.GetValue( "Speed", "0" ) );
		}
	}

	//Unit Missions:
	//{
	//	Sleep - no-op
	//	Harmless - no-op, and also not considered a threat
	//	Sticky
	//	Attack
	//	Move
	//	QMove
	//	Retreat
	//	Guard
	//	Enter
	//	Capture
	//	Harvest
	//	Area Guard
	//	[Return]
	//	Stop
	//	[Ambush]
	//	Hunt
	//	Unload
	//	Sabotage
	//	Construction
	//	Selling
	//	Repair
	//	Rescue
	//	Missile
	//}
}
