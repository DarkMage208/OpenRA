using System.Collections.Generic;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using OpenRa.TechTree;
using System.Drawing;
using System.Linq;
using IrrKlang;

namespace OpenRa.Game
{
	class Game
	{
		public readonly World world;
		public readonly Map map;
		readonly TreeCache treeCache;
		public readonly TerrainRenderer terrain;
		public readonly Viewport viewport;
		public readonly PathFinder pathFinder;
		public readonly Network network;
		public readonly WorldRenderer worldRenderer;
		public readonly Controller controller;

		int localPlayerIndex = 2;

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		public Player LocalPlayer { get { return players[localPlayerIndex]; } }
		public BuildingInfluenceMap LocalPlayerBuildings;

		ISoundEngine soundEngine;

		public Game(string mapName, Renderer renderer, int2 clientSize)
		{
			Rules.LoadRules();

			for( int i = 0 ; i < 8 ; i++ )
				players.Add(i, new Player(i, string.Format("Multi{0}", i), Race.Soviet));

			map = new Map(new IniFile(FileSystem.Open(mapName)));
			FileSystem.Mount(new Package(map.Theater + ".mix"));

			viewport = new Viewport(clientSize, map.Size, renderer);

			terrain = new TerrainRenderer(renderer, map, viewport);
			world = new World(this);
			treeCache = new TreeCache(map);

			foreach( TreeReference treeReference in map.Trees )
				world.Add( new Actor( treeReference, treeCache, map.Offset ) );

			LocalPlayerBuildings = new BuildingInfluenceMap(world, LocalPlayer);

			pathFinder = new PathFinder(map, terrain.tileSet, LocalPlayerBuildings);

			network = new Network();

			controller = new Controller(this);		// CAREFUL THERES AN UGLY HIDDEN DEPENDENCY HERE STILL
			worldRenderer = new WorldRenderer(renderer, this);

			var sound = AudLoader.LoadSound(FileSystem.Open("intro.aud"));

			soundEngine = new ISoundEngine();
			
			var soundSource = soundEngine.AddSoundSourceFromPCMData(sound, "intro.aud",
				new AudioFormat()
				{
					ChannelCount = 1,
					FrameCount = sound.Length / 2,
					Format = SampleFormat.Signed16Bit,
					SampleRate = 22050
				});

			soundEngine.Play2D(soundSource, true, false, true);
		}

		public void Tick()
		{
			var stuffFromOtherPlayers = network.Tick();	// todo: actually use the orders!
			world.Update();

			viewport.DrawRegions();
		}

		public bool IsCellBuildable(int2 a)
		{
			if (LocalPlayerBuildings[a] != null) return false;

			a += map.Offset;

			return map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(UnitMovementType.Wheel,
					terrain.tileSet.GetWalkability(map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public IEnumerable<Actor> FindUnits(float2 a, float2 b)
		{
			var min = float2.Min(a, b);
			var max = float2.Max(a, b);

			var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);

			return world.Actors
				.Where(x => (x.Owner == LocalPlayer) && (x.Bounds.IntersectsWith(rect)));
		}
	}
}
