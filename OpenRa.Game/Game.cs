using System.Collections.Generic;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using OpenRa.TechTree;
using System.Drawing;
using System.Linq;
using IrrKlang;
using IjwFramework.Collections;

namespace OpenRa.Game
{
	class Game
	{
		public static readonly int CellSize = 24;

		public readonly World world;
		public readonly Map map;
		readonly TreeCache treeCache;
		public readonly TerrainRenderer terrain;
		public readonly Viewport viewport;
		public readonly PathFinder pathFinder;
		public readonly Network network;
		public readonly WorldRenderer worldRenderer;
		public readonly Controller controller;

		int localPlayerIndex = 0;

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		public Player LocalPlayer { get { return players[localPlayerIndex]; } }
		public BuildingInfluenceMap LocalPlayerBuildings;

		ISoundEngine soundEngine;

		public Game(string mapName, Renderer renderer, int2 clientSize)
		{
			Rules.LoadRules( mapName );

			for( int i = 0 ; i < 8 ; i++ )
				players.Add(i, new Player(i, string.Format("Multi{0}", i), Race.Soviet));

			var mapFile = new IniFile( FileSystem.Open( mapName ) );
			map = new Map( mapFile );
			FileSystem.Mount(new Package(map.Theater + ".mix"));

			viewport = new Viewport(clientSize, map.Size, renderer);

			terrain = new TerrainRenderer(renderer, map, viewport);
			world = new World(this);
			treeCache = new TreeCache(map);

			foreach( TreeReference treeReference in map.Trees )
				world.Add( new Actor( treeReference, treeCache, map.Offset ) );

			LoadMapBuildings( mapFile );
			LoadMapUnits( mapFile );

			LocalPlayerBuildings = new BuildingInfluenceMap(world, LocalPlayer);

			pathFinder = new PathFinder(map, terrain.tileSet, LocalPlayerBuildings);

			network = new Network();

			controller = new Controller(this);		// CAREFUL THERES AN UGLY HIDDEN DEPENDENCY HERE STILL
			worldRenderer = new WorldRenderer(renderer, this);

			soundEngine = new ISoundEngine();
			sounds = new Cache<string, ISoundSource>(LoadSound);

			PlaySound("intro.aud", false);
		}

		void LoadMapBuildings( IniFile mapfile )
		{
			foreach( var s in mapfile.GetSection( "STRUCTURES", true ) )
			{
				//num=owner,type,health,location,facing,trigger,unknown,shouldRepair
				var parts = s.Value.ToLowerInvariant().Split( ',' );
				var loc = int.Parse( parts[ 3 ] );
				world.Add( new Actor( parts[ 1 ], new int2( loc % 128 - map.Offset.X, loc / 128-map.Offset.Y ), players[ 0 ] ) );
			}
		}

		void LoadMapUnits( IniFile mapfile )
		{
			foreach( var s in mapfile.GetSection( "UNITS", true ) )
			{
				//num=owner,type,health,location,facing,action,trigger
				var parts = s.Value.ToLowerInvariant().Split( ',' );
				var loc = int.Parse( parts[ 3 ] );
				world.Add( new Actor( parts[ 1 ], new int2( loc % 128 - map.Offset.X, loc / 128 - map.Offset.Y ), players[ 0 ] ) );
			}
		}

		readonly Cache<string, ISoundSource> sounds;

		ISoundSource LoadSound(string filename)
		{
			var data = AudLoader.LoadSound(FileSystem.Open(filename));
			return soundEngine.AddSoundSourceFromPCMData(data, filename,
				new AudioFormat()
				{
					ChannelCount = 1,
					FrameCount = data.Length / 2,
					Format = SampleFormat.Signed16Bit,
					SampleRate = 22050
				});
		}

		public void PlaySound(string name, bool loop)
		{
			var sound = sounds[name];
			// todo: positioning
			soundEngine.Play2D(sound, loop, false, false);
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
