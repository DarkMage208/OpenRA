﻿
namespace OpenRa.GameRules
{
	public class UserSettings
	{
		// Debug settings
		public readonly bool UnitDebug = false;
		public readonly bool PathDebug = false;
		public readonly bool PerfGraph = true;
		
		// Window settings
		public readonly int Width = 0;
		public readonly int Height = 0;
		public readonly bool Fullscreen = false;
		
		// Internal game settings
		public readonly int Timestep = 40;
		public readonly int SheetSize = 512;
		
		// External game settings
		public readonly string NetworkHost = "";
		public readonly int NetworkPort = 0;
		public readonly string Map = "scm12ea.ini";
		public readonly int Player = 1;
		public readonly string Replay = "";
		public readonly string PlayerName = "";
		public readonly string[] InitialMods = { "ra" };
		
		// Gameplay options
		// TODO: These need to die
		public readonly bool RepairRequiresConyard = true;
		public readonly bool PowerDownBuildings = true;
	}
}
