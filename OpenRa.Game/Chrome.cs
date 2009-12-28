﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IjwFramework.Collections;
using IjwFramework.Types;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;
using OpenRa.Game.GameRules;


namespace OpenRa.Game
{
	class Chrome : IHandleInput
	{
		readonly Renderer renderer;
		readonly Sheet specialBin;
		readonly SpriteRenderer chromeRenderer;
		readonly Sprite specialBinSprite;
		readonly Sprite moneyBinSprite;
		readonly Sprite tooltipSprite;
		readonly SpriteRenderer buildPaletteRenderer;
		readonly Animation cantBuild;
		readonly Animation ready;

		readonly List<Pair<Rectangle, Action<bool>>> buttons = new List<Pair<Rectangle, Action<bool>>>();
		readonly Cache<string, Animation> clockAnimations;
		readonly List<Sprite> digitSprites;
		readonly Dictionary<string, Sprite[]> tabSprites;
		readonly Sprite[] shimSprites;
		readonly Sprite blank;
	
		readonly int paletteColumns;
		readonly int2 paletteOrigin;
		
		public Chrome(Renderer r)
		{
			// Positioning of chrome elements
			// Build palette
			paletteColumns = 4;
			paletteOrigin = new int2(Game.viewport.Width - paletteColumns * 64 - 9, 240 - 9);
			
			this.renderer = r;
			specialBin = new Sheet(renderer, "specialbin.png");
			chromeRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			buildPaletteRenderer = new SpriteRenderer(renderer, true);

			specialBinSprite = new Sprite(specialBin, new Rectangle(0, 0, 32, 192), TextureChannel.Alpha);
			moneyBinSprite = new Sprite(specialBin, new Rectangle(512 - 320, 0, 320, 32), TextureChannel.Alpha);
			tooltipSprite = new Sprite(specialBin, new Rectangle(0, 288, 272, 136), TextureChannel.Alpha);

			blank = SheetBuilder.Add(new Size(64, 48), 16);

			sprites = groups
				.SelectMany(g => Rules.Categories[g])
				.Where(u => Rules.UnitInfo[u].TechLevel != -1)
				.ToDictionary(
					u => u,
					u => SpriteSheetBuilder.LoadAllSprites(Rules.UnitInfo[u].Icon ?? (u + "icon"))[0]);

			tabSprites = groups.Select(
				(g, i) => Pair.New(g,
					Util.MakeArray(3,
						n => new Sprite(specialBin,
							new Rectangle(512 - (n + 1) * 27, 64 + i * 40, 27, 40),
							TextureChannel.Alpha))))
				.ToDictionary(a => a.First, a => a.Second);

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			clockAnimations = new Cache<string, Animation>(
				s =>
				{
					var anim = new Animation("clock");
					anim.PlayFetchIndex("idle", ClockAnimFrame(s));
					return anim;
				});

			digitSprites = Util.MakeArray(10, a => a)
				.Select(n => new Sprite(specialBin, new Rectangle(32 + 13 * n, 0, 13, 17), TextureChannel.Alpha)).ToList();

			shimSprites = new[] 
			{
				new Sprite( specialBin, new Rectangle( 0, 192, 9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 202, 9, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 0, 216, 9, 48 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 11, 192, 64, 10 ), TextureChannel.Alpha ),
				new Sprite( specialBin, new Rectangle( 11, 202, 64, 10 ), TextureChannel.Alpha ),
			};

			ready = new Animation("pips");
			ready.PlayRepeating("ready");
		}
		
		public void Draw()
		{
			buttons.Clear();

			renderer.Device.DisableScissor();
			renderer.DrawText("RenderFrame {0} ({2:F1} ms)\nTick {1} ({3:F1} ms)\nPower {4}/{5}\nReady: {6} (F8 to toggle)".F(
				Game.RenderFrame,
				Game.orderManager.FrameNumber,
				PerfHistory.items["render"].LastValue,
				PerfHistory.items["tick_time"].LastValue,
				Game.LocalPlayer.PowerDrained,
				Game.LocalPlayer.PowerProvided,
				Game.LocalPlayer.IsReady ? "Yes" : "No"
				), new int2(140, 5), Color.White);

			PerfHistory.Render(renderer, Game.worldRenderer.lineRenderer);

			chromeRenderer.DrawSprite(specialBinSprite, float2.Zero, PaletteType.Chrome);
			chromeRenderer.DrawSprite(moneyBinSprite, new float2(Game.viewport.Width - 320, 0), PaletteType.Chrome);

			DrawMoney();
			
			chromeRenderer.Flush();
			
			int paletteHeight = DrawBuildPalette(currentTab);
			DrawBuildTabs(paletteHeight);
			DrawChat();
		}

		void DrawBuildTabs(int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;

			if (currentTab == null || !Rules.TechTree.BuildableItems(Game.LocalPlayer, currentTab).Any())
				ChooseAvailableTab();

			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			foreach (var q in tabSprites)
			{
				var groupName = q.Key;
				if (!Rules.TechTree.BuildableItems(Game.LocalPlayer, groupName).Any())
				{
					CheckDeadTab(groupName);
					continue;
				}

				var producing = queue.Producing(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				
				
				// Don't let tabs overlap the bevel
				if (y > paletteOrigin.Y + paletteHeight - tabHeight - 9 && y < paletteOrigin.Y + paletteHeight)
				{
					y += tabHeight;	
				}
				
				// Stick tabs to the edge of the screen
				if (y > paletteOrigin.Y + paletteHeight)
				{
					x = Game.viewport.Width - tabWidth;
				}

				chromeRenderer.DrawSprite(q.Value[index], new float2(x, y), PaletteType.Chrome);

				buttons.Add(Pair.New(new Rectangle(x, y, tabWidth, tabHeight), 
					(Action<bool>)(isLmb => currentTab = groupName)));
				y += tabHeight;
			}

			chromeRenderer.Flush();
		}
		
		void CheckDeadTab( string groupName )
		{
			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var item = queue.Producing( groupName );
			if (item != null)
				for( var n = 0; n <= item.Repeats; n++ )
					Game.controller.AddOrder(Order.CancelProduction(Game.LocalPlayer, item.Item));
		}

		void ChooseAvailableTab()
		{
			currentTab = tabSprites.Select(q => q.Key).FirstOrDefault(
				t => Rules.TechTree.BuildableItems(Game.LocalPlayer, t).Any());
		}

		void DrawMoney()
		{
			var moneyDigits = Game.LocalPlayer.DisplayCash.ToString();
			var x = Game.viewport.Width - 155;
			foreach (var d in moneyDigits.Reverse())
			{
				chromeRenderer.DrawSprite(digitSprites[d - '0'], new float2(x, 6), PaletteType.Chrome);
				x -= 14;
			}
		}

		void DrawChat()
		{
			var chatpos = new int2(400, Game.viewport.Height - 20);

			if (Game.chat.isChatting)
				RenderChatLine(Tuple.New(Color.White, "Chat:", Game.chat.typing), chatpos);

			foreach (var line in Game.chat.recentLines.AsEnumerable().Reverse())
			{
				chatpos.Y -= 20;
				RenderChatLine(line, chatpos);
			}
		}

		void RenderChatLine(Tuple<Color, string, string> line, int2 p)
		{
			var size = renderer.MeasureText(line.b);
			renderer.DrawText(line.b, p, line.a);
			renderer.DrawText(line.c, p + new int2(size.X + 10, 0), Color.White);
		}

		string currentTab = "Building";
		static string[] groups = new string[] { "Building", "Defense", "Infantry", "Vehicle", "Plane", "Ship" };
		Dictionary<string, Sprite> sprites;

		const int NumClockFrames = 54;
		Func<int> ClockAnimFrame(string group)
		{
			return () =>
			{
				var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
				var producing = queue.Producing( group );
				if (producing == null) return 0;
				return (producing.TotalTime - producing.RemainingTime) * NumClockFrames / producing.TotalTime;
			};
		}
		
		// Return an int telling us the y coordinate at the bottom of the palette
		int DrawBuildPalette(string queueName)
		{
			// Hack
			int columns = paletteColumns;
			int2 origin = new int2(paletteOrigin.X + 9, paletteOrigin.Y + 9);
			
			if (queueName == null) return 0;

			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(Game.LocalPlayer, queueName).ToArray();

			var allItems = Rules.TechTree.AllItems(Game.LocalPlayer, queueName)
				.Where(a => Rules.UnitInfo[a].TechLevel != -1)
				.OrderBy(a => Rules.UnitInfo[a].TechLevel);

			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var currentItem = queue.Producing( queueName );

			var overlayBits = new List<Pair<Sprite, float2>>();

			string tooltipItem = null;

			foreach (var item in allItems)
			{
				var rect = new Rectangle(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = Game.viewport.Location + new float2(rect.Location);
				var isBuildingThis = currentItem != null && currentItem.Item == item;
				var isBuildingSomethingElse = currentItem != null && currentItem.Item != item;

				buildPaletteRenderer.DrawSprite(sprites[item], drawPos, PaletteType.Chrome);

				if (rect.Contains(lastMousePos.ToPoint()))
					tooltipItem = item;

				if (!buildableItems.Contains(item) || isBuildingSomethingElse)
					overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				if (isBuildingThis)
				{
					clockAnimations[queueName].Tick();
					buildPaletteRenderer.DrawSprite(clockAnimations[queueName].Image,
						drawPos, PaletteType.Chrome);

					var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

					if (currentItem.Done)
					{
						ready.Play("ready");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}
					else if (currentItem.Paused)
					{
						ready.Play("hold");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}

					if (currentItem.Repeats > 0)
					{
						var offset = -20;
						var digits = (currentItem.Repeats + 1).ToString();
						foreach (var d in digits)
						{
							ready.PlayFetchIndex("groups", () => d - '0');
							ready.Tick();
							overlayBits.Add(Pair.New(ready.Image, overlayPos + new float2(offset, 0)));
							offset += 6;
						}
					}
				}

				var closureItem = item;
				buttons.Add(Pair.New(rect,
					(Action<bool>)(isLmb => HandleBuildPalette(closureItem, isLmb))));
				if (++x == columns) { x = 0; y++; }
			}

			while (x != 0)
			{
				var rect = new Rectangle(origin.X +  x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = Game.viewport.Location + new float2(rect.Location);
				buildPaletteRenderer.DrawSprite(blank, drawPos, PaletteType.Chrome);
				buttons.Add(Pair.New(rect, (Action<bool>)(_ => { })));
				if (++x == columns) { x = 0; y++; }
			}

			foreach (var ob in overlayBits)
				buildPaletteRenderer.DrawSprite(ob.First, ob.Second, PaletteType.Chrome);

			buildPaletteRenderer.Flush();

			for (var j = 0; j < y; j++)
				chromeRenderer.DrawSprite(shimSprites[2], new float2(origin.X - 9, origin.Y + 48 * j), PaletteType.Chrome);

			chromeRenderer.DrawSprite(shimSprites[0], new float2(origin.X - 9, origin.Y - 9), PaletteType.Chrome);
			chromeRenderer.DrawSprite(shimSprites[1], new float2(origin.X - 9, origin.Y - 1 + 48 * y), PaletteType.Chrome);

			for (var i = 0; i < columns; i++)
			{
				chromeRenderer.DrawSprite(shimSprites[3], new float2(origin.X + 64 * i, origin.Y - 9), PaletteType.Chrome);
				chromeRenderer.DrawSprite(shimSprites[4], new float2(origin.X + 64 * i, origin.Y - 1 + 48 * y), PaletteType.Chrome);
			}
			chromeRenderer.Flush();

			if (tooltipItem != null)
				DrawProductionTooltip(tooltipItem, new int2(Game.viewport.Width, origin.Y + y * 48 + 9)/*tooltipPos*/);
				
			return y*48+9;
		}

		void HandleBuildPalette(string item, bool isLmb)
		{
			var player = Game.LocalPlayer;
			var group = Rules.UnitCategory[item];
			var queue = player.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var producing = queue.Producing( group );

			Sound.Play("ramenu1.aud");

			if (isLmb)
			{
				if (producing == null)
				{
					Game.controller.AddOrder(Order.StartProduction(player, item));
					Sound.Play((group == "Building" || group == "Defense") ? "abldgin1.aud" : "train1.aud");
				}
				else if (producing.Item == item)
				{
					if (producing.Done)
					{
						if (group == "Building" || group == "Defense")
							Game.controller.orderGenerator = new PlaceBuilding(player.PlayerActor, item);
					}
					else if (producing.Paused)
						Game.controller.AddOrder(Order.PauseProduction(player, item, false));
					else
					{
						Sound.Play((group == "Building" || group == "Defense") ? "abldgin1.aud" : "train1.aud");
						Game.controller.AddOrder(Order.StartProduction(player, item));
					}
				}
				else
				{
					Sound.Play("progres1.aud");
				}
			}
			else
			{
				if (producing == null) return;
				if (item != producing.Item) return;

				if (producing.Paused || producing.Done)
				{
					Sound.Play("cancld1.aud");
					Game.controller.AddOrder(Order.CancelProduction(player, item));
				}
				else
				{
					Sound.Play("onhold1.aud");
					Game.controller.AddOrder(Order.PauseProduction(player, item, true));
				}
			}
		}

		int2 lastMousePos;
		public bool HandleInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				lastMousePos = mi.Location;

			var action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();

			if (action == null)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				action(mi.Button == MouseButton.Left);

			return true;
		}

		public bool HitTest(int2 mousePos)
		{
			return buttons.Any(a => a.First.Contains(mousePos.ToPoint()));
		}

		void DrawRightAligned(string text, int2 pos, Color c)
		{
			renderer.DrawText2(text, pos - new int2(renderer.MeasureText2(text).X, 0), c);
		}

		void DrawProductionTooltip(string unit, int2 pos)
		{
			var p = pos.ToFloat2() - new float2(tooltipSprite.size.X, 0);
			chromeRenderer.DrawSprite(tooltipSprite, p, PaletteType.Chrome);
			chromeRenderer.Flush();

			var info = Rules.UnitInfo[unit];

			renderer.DrawText2(info.Description, p.ToInt2() + new int2(5,5), Color.White);

			DrawRightAligned( "${0}".F(info.Cost), pos + new int2(-5,5), 
				Game.LocalPlayer.Cash + Game.LocalPlayer.Ore >= info.Cost ? Color.White : Color.Red);

			var bi = info as BuildingInfo;
			if (bi != null)
				DrawRightAligned("ϟ{0}".F(bi.Power), pos + new int2(-5, 20),
					Game.LocalPlayer.PowerProvided - Game.LocalPlayer.PowerDrained + bi.Power >= 0
					? Color.White : Color.Red);

			var buildings = Rules.TechTree.GatherBuildings( Game.LocalPlayer );
			p += new int2(5, 5);
			p += new int2(0, 15);
			if (!Rules.TechTree.CanBuild(info, Game.LocalPlayer, buildings))
			{
				var prereqs = info.Prerequisite.Select(a => Rules.UnitInfo[a.ToLowerInvariant()].Description);
				renderer.DrawText("Requires {0}".F( string.Join( ", ", prereqs.ToArray() ) ), p.ToInt2(),
					Color.White);
			}

			if (info.LongDesc != null)
			{
				p += new int2(0, 15);
				renderer.DrawText(info.LongDesc.Replace( "\\n", "\n" ), p.ToInt2(), Color.White);
			}
		}
	}
}
