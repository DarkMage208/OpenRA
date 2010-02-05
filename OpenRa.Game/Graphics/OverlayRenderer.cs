﻿using System.Linq;
using OpenRa.FileFormats;

namespace OpenRa.Graphics
{
	class OverlayRenderer
	{
		static string[] overlaySpriteNames =
			{
				"sbag", "cycl", "brik", "fenc", "wood",
				"gold01", "gold02", "gold03", "gold04",
				"gem01", "gem02", "gem03", "gem04",
				"v12", "v13", "v14", "v15", "v16", "v17", "v18",
				"fpls", "wcrate", "scrate", "barb", "sbag",
			};

		static string[] smudgeSpriteNames =
			{ 
				"bib3", "bib2", "bib1", "sc1", "sc2", "sc3", "sc4", "sc5", "sc6",
				"cr1", "cr2", "cr3", "cr4", "cr5", "cr6", 
			};

		readonly Sprite[][] overlaySprites;
		readonly Sprite[] smudgeSprites;

		SpriteRenderer spriteRenderer;
		Map map;

		public OverlayRenderer( Renderer renderer, Map map )
		{
			this.spriteRenderer = new SpriteRenderer( renderer, true );
			this.map = map;

			overlaySprites = overlaySpriteNames.Select(f => SpriteSheetBuilder.LoadAllSprites(f)).ToArray();
			smudgeSprites = smudgeSpriteNames.SelectMany(f => SpriteSheetBuilder.LoadAllSprites(f)).ToArray();
		}

		public void Draw()
		{
			var shroud = Game.world.LocalPlayer.Shroud;

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
				{
					if (!shroud.IsExplored(new int2(x,y))) continue;

					var tr = map.MapTiles[x,y];
					if (tr.smudge != 0 && tr.smudge <= smudgeSprites.Length)
					{
						var location = new int2(x, y);
						spriteRenderer.DrawSprite(smudgeSprites[tr.smudge - 1],
							Game.CellSize * (float2)location, "terrain");
					}

					var o = tr.overlay;
					if (o < overlaySprites.Length)
					{
						var location = new int2(x, y);
						var sprites = overlaySprites[o];
						var spriteIndex = 0;
						if (Ore.overlayIsFence[o]) spriteIndex = NearbyFences(x, y);
						else if (Ore.overlayIsOre[o]) spriteIndex = map.MapTiles[x,y].density - 1;
						else if (Ore.overlayIsGems[o]) spriteIndex = map.MapTiles[x,y].density - 1;
						spriteRenderer.DrawSprite(sprites[spriteIndex],
							Game.CellSize * (float2)location, "terrain");
					}
				}

			spriteRenderer.Flush();
		}

		bool IsFence( int x, int y )
		{
			var o = map.MapTiles[ x, y ].overlay;
			if (o < Ore.overlayIsFence.Length)
				return Ore.overlayIsFence[o];
			return false;
		}

		int NearbyFences( int x, int y )
		{
			int ret = 0;
			if( IsFence( x, y - 1 ) )
				ret |= 1;
			if( IsFence( x + 1, y ) )
				ret |= 2;
			if( IsFence( x, y + 1 ) )
				ret |= 4;
			if( IsFence( x - 1, y ) )
				ret |= 8;
			return ret;
		}
	}
}
