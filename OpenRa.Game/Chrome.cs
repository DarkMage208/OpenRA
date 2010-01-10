﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using IjwFramework.Types;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Orders;
using OpenRa.Game.Support;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class Chrome : IHandleInput
	{
		readonly Renderer renderer;
		readonly LineRenderer lineRenderer;
		readonly SpriteRenderer rgbaRenderer;
		readonly SpriteRenderer shpRenderer;
		
		string chromeCollection;
		string radarCollection;
		string paletteCollection;
		string digitCollection;
		
		// Special power bin
		readonly Dictionary<string, Sprite> spsprites;
		
		// Options menu (to be refactored)
		bool optionsPressed = false;
		readonly Sprite optionsTop;
		readonly Sprite optionsBottom;
		readonly Sprite optionsLeft;
		readonly Sprite optionsRight;
		readonly Sprite optionsTopLeft;
		readonly Sprite optionsTopRight;
		readonly Sprite optionsBottomLeft;
		readonly Sprite optionsBottomRight;
		readonly Sprite optionsBackground;
		
		// Buttons
		readonly Animation repairButton;
		readonly Animation sellButton;
		readonly Animation pwrdownButton;
		readonly Animation optionsButton;

		// Build Palette tabs
		string currentTab = "Building";
		bool paletteOpen = false;
		static string[] groups = new string[] { "Building", "Defense", "Infantry", "Vehicle", "Plane", "Ship" };
		readonly Dictionary<string, string[]> tabImageNames;
		readonly Dictionary<string, Sprite> tabSprites;
		
		// Build Palette
		const int paletteColumns = 3;
		const int paletteRows = 5;
		static float2 paletteOpenOrigin = new float2(Game.viewport.Width - 215, 280);
		static float2 paletteClosedOrigin = new float2(Game.viewport.Width - 16, 280);
		static float2 paletteOrigin = paletteClosedOrigin;
		const int paletteAnimationLength = 7;
		int paletteAnimationFrame = 0;
		bool paletteAnimating = false;
		readonly List<Pair<RectangleF, Action<bool>>> buttons = new List<Pair<RectangleF, Action<bool>>>();
		readonly Animation cantBuild;
		readonly Animation ready;
		readonly Animation clock;
		const int NumClockFrames = 54;

		// Radar
		static float2 radarOpenOrigin = new float2(Game.viewport.Width - 215, 29);
		static float2 radarClosedOrigin = new float2(Game.viewport.Width - 215, -166);
		static float2 radarOrigin = radarClosedOrigin;
		float radarMinimapHeight;
		const int radarSlideAnimationLength = 15;
		const int radarActivateAnimationLength = 5;
		int radarAnimationFrame = 0;
		bool radarAnimating = false;
		bool hasRadar = false;
				
		// Power bar 
		static float2 powerOrigin = new float2(42, 205); // Relative to radarOrigin
		static Size powerSize = new Size(138,5);
		
		public Chrome(Renderer r)
		{		
			this.renderer = r;
			rgbaRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			lineRenderer = new LineRenderer(renderer);
			shpRenderer = new SpriteRenderer(renderer, true);

			repairButton = new Animation("repair");
			repairButton.PlayRepeating("normal");

			sellButton = new Animation("sell");
			sellButton.PlayRepeating("normal");

			pwrdownButton = new Animation("repair");
			pwrdownButton.PlayRepeating("normal");
			
			optionsButton = new Animation("tabs");
			optionsButton.PlayRepeating("left-normal");
			
			optionsLeft = SpriteSheetBuilder.LoadAllSprites("dd-left")[0];
			optionsRight = SpriteSheetBuilder.LoadAllSprites("dd-right")[0];
			optionsTop = SpriteSheetBuilder.LoadAllSprites("dd-top")[0];
			optionsBottom = SpriteSheetBuilder.LoadAllSprites("dd-botm")[0];
			optionsTopLeft = SpriteSheetBuilder.LoadAllSprites("dd-crnr")[0];
			optionsTopRight = SpriteSheetBuilder.LoadAllSprites("dd-crnr")[1];
			optionsBottomLeft = SpriteSheetBuilder.LoadAllSprites("dd-crnr")[2];
			optionsBottomRight = SpriteSheetBuilder.LoadAllSprites("dd-crnr")[3];	
			optionsBackground = SpriteSheetBuilder.LoadAllSprites("dd-bkgnd")[Game.CosmeticRandom.Next(4)];
			
			tabSprites = groups
				.SelectMany(g => Rules.Categories[g])
				.Where(u => Rules.UnitInfo[u].TechLevel != -1)
				.ToDictionary(
					u => u,
					u => SpriteSheetBuilder.LoadAllSprites(Rules.UnitInfo[u].Icon ?? (u + "icon"))[0]);

			spsprites = Rules.SupportPowerInfo
				.ToDictionary(
					u => u.Key,
					u => SpriteSheetBuilder.LoadAllSprites(u.Value.Image)[0]);
			
			tabImageNames = groups.Select(
				(g, i) => Pair.New(g,
					OpenRa.Game.Graphics.Util.MakeArray(3,
						n => i.ToString())))
				.ToDictionary(a => a.First, a => a.Second);

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
		}
		
		public void Tick()
		{
			TickPaletteAnimation();
			TickRadarAnimation();
		}
				
		public void Draw()
		{
			chromeCollection = (Game.LocalPlayer.Race == Race.Allies) ? "chrome-allies" : "chrome-soviet";
			radarCollection = (Game.LocalPlayer.Race == Race.Allies) ? "radar-allies" : "radar-soviet";
			paletteCollection = (Game.LocalPlayer.Race == Race.Allies) ? "palette-allies" : "palette-soviet";
			digitCollection = (Game.LocalPlayer.Race == Race.Allies) ? "digits-allies" : "digits-soviet";

			buttons.Clear();

			renderer.Device.DisableScissor();
			renderer.DrawText("RenderFrame {0} ({2:F1} ms)\nTick {1} ({3:F1} ms)\nReady: {4} (F8 to toggle)".F(
				Game.RenderFrame,
				Game.orderManager.FrameNumber,
				PerfHistory.items["render"].LastValue,
				PerfHistory.items["tick_time"].LastValue,
				Game.LocalPlayer.IsReady ? "Yes" : "No"
				), new int2(140, 15), Color.White);

			if (Game.Settings.PerfGraph)
				PerfHistory.Render(renderer, Game.worldRenderer.lineRenderer);

			DrawRadar();
			DrawPower();
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, chromeCollection, "moneybin"), new float2(Game.viewport.Width - 320, 0), PaletteType.Chrome);
			DrawMoney();
			rgbaRenderer.Flush();
			DrawButtons();
			
			int paletteHeight = DrawBuildPalette(currentTab);
			DrawSupportPowers();
			DrawBuildTabs(paletteHeight);
			DrawChat();
			DrawOptionsMenu();
		}

		public void TickRadarAnimation()
		{
			if (!radarAnimating)
				return;

			// Increment frame
			if (hasRadar)
				radarAnimationFrame++;
			else
				radarAnimationFrame--;

			// Calculate radar bin position
			if (radarAnimationFrame <= radarSlideAnimationLength)
				radarOrigin = float2.Lerp(radarClosedOrigin, radarOpenOrigin, radarAnimationFrame * 1.0f / radarSlideAnimationLength);

			// Play radar-on sound at the start of the activate anim (open)
			if (radarAnimationFrame == radarSlideAnimationLength && hasRadar)
				Sound.Play("radaron2.aud");

			// Play radar-on sound at the start of the activate anim (close)
			if (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength - 1 && !hasRadar)
				Sound.Play("radardn1.aud");

			// Minimap height
			if (radarAnimationFrame >= radarSlideAnimationLength)
				radarMinimapHeight = float2.Lerp(0, 192, (radarAnimationFrame - radarSlideAnimationLength) * 1.0f / radarActivateAnimationLength);

			// Animation is complete
			if ((radarAnimationFrame == 0 && !hasRadar)
					|| (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength && hasRadar))
			{
				radarAnimating = false;
			}
		}
		
		void DrawRadar()
		{
			var hasNewRadar = Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer 
				&& a.traits.Contains<ProvidesRadar>() 
				&& a.traits.Get<ProvidesRadar>().IsActive());
			
			if (hasNewRadar != hasRadar)
			{
				radarAnimating = true;
			}
			
			hasRadar = hasNewRadar;

			var isJammed = false;		// todo: MRJ can do this

			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, radarCollection, "left"), radarOrigin, PaletteType.Chrome);
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, radarCollection, "right"), radarOrigin + new float2(201, 0), PaletteType.Chrome);
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, radarCollection, "bottom"), radarOrigin + new float2(0, 192), PaletteType.Chrome);	

			if (radarAnimating)
				rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, radarCollection, "bg"), radarOrigin + new float2(9, 0), PaletteType.Chrome);	
			
			rgbaRenderer.Flush();

			if (radarAnimationFrame >= radarSlideAnimationLength)
			{
				RectangleF mapRect = new RectangleF(radarOrigin.X + 9, radarOrigin.Y+(192-radarMinimapHeight)/2, 192, radarMinimapHeight);
				Game.minimap.Draw(mapRect, hasRadar, isJammed);
			}
		}
		
		void AddButton(RectangleF r, Action<bool> b) { buttons.Add(Pair.New(r, b)); }
		
		void DrawBuildTabs(int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;

			if (currentTab == null || !Rules.TechTree.BuildableItems(Game.LocalPlayer, currentTab).Any())
				ChooseAvailableTab();

			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			foreach (var q in tabImageNames)
			{
				var groupName = q.Key;
				if (!Rules.TechTree.BuildableItems(Game.LocalPlayer, groupName).Any())
				{
					CheckDeadTab(groupName);
					continue;
				}
				string[] tabKeys = { "normal", "ready", "selected" };
				var producing = queue.CurrentItem(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				var race = (Game.LocalPlayer.Race == Race.Allies) ? "allies" : "soviet";
				rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer,"tabs-"+tabKeys[index], race+"-"+q.Key), new float2(x, y), PaletteType.Chrome);

				buttons.Add(Pair.New(new RectangleF(x, y, tabWidth, tabHeight),
					(Action<bool>)(isLmb => HandleTabClick(groupName))));
				y += tabHeight;
			}

			rgbaRenderer.Flush();
		}
		
		void HandleTabClick(string button)
		{
			Sound.Play("ramenu1.aud");
			var wasOpen = paletteOpen;
			paletteOpen = (currentTab == button && wasOpen) ? false : true;
			currentTab = button;
			if (wasOpen != paletteOpen)
				paletteAnimating = true;
		}
		
		void CheckDeadTab( string groupName )
		{
			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			foreach( var item in queue.AllItems( groupName ) )
				Game.controller.AddOrder(Order.CancelProduction(Game.LocalPlayer, item.Item));		
		}

		void ChooseAvailableTab()
		{
			currentTab = tabImageNames.Select(q => q.Key).FirstOrDefault(
				t => Rules.TechTree.BuildableItems(Game.LocalPlayer, t).Any());
		}

		void DrawMoney()
		{
			var moneyDigits = Game.LocalPlayer.DisplayCash.ToString();
			var x = Game.viewport.Width - 65;
			foreach (var d in moneyDigits.Reverse())
			{
				rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, digitCollection, (d - '0').ToString()), new float2(x, 6), PaletteType.Chrome);
				x -= 14;
			}
		}

		float? lastPowerProvidedPos;
		float? lastPowerDrainedPos;
		
		void DrawPower()
		{
			// Nothing to draw
			if (Game.LocalPlayer.PowerProvided == 0 && Game.LocalPlayer.PowerDrained == 0)
				return;
			
			// Draw bar horizontally
			var barStart = powerOrigin + Game.viewport.Location + radarOrigin;
			var barEnd = barStart + new float2(powerSize.Width, 0);

			float powerScaleBy = 100;
			var maxPower = Math.Max(Game.LocalPlayer.PowerProvided, Game.LocalPlayer.PowerDrained);
			while (maxPower >= powerScaleBy) powerScaleBy *= 2;
			
			// Current power supply
			var powerLevelTemp = barStart.X + (barEnd.X - barStart.X) * (Game.LocalPlayer.PowerProvided / powerScaleBy) - Game.viewport.Location.X;
			lastPowerProvidedPos = float2.Lerp(lastPowerProvidedPos.GetValueOrDefault(powerLevelTemp), powerLevelTemp, .3f);
			float2 powerLevel = new float2(lastPowerProvidedPos.Value + Game.viewport.Location.X, barStart.Y);

			var color = Color.LimeGreen;
			if (Game.LocalPlayer.GetPowerState() == PowerState.Low)
				color = Color.Orange;
			if (Game.LocalPlayer.GetPowerState() == PowerState.Critical)
				color = Color.Red;
		
			var colorDark = Graphics.Util.Lerp(0.25f, color, Color.Black);
			for (int i = 0; i < powerSize.Height; i++)
			{
				color = (i-1 < powerSize.Height/2) ? color : colorDark;
				float2 leftOffset = new float2(0,i);
				float2 rightOffset = new float2(0,i);
				// Indent corners
				if ((i == 0 || i == powerSize.Height - 1) && powerLevel.X - barStart.X > 1)
				{
					leftOffset.X += 1;
					rightOffset.X -= 1;
				}
				lineRenderer.DrawLine(barStart + leftOffset, powerLevel + rightOffset, color, color);
			}
			lineRenderer.Flush();

			// Power usage indicator
			var indicator = SequenceProvider.GetImageFromCollection(renderer, radarCollection, "power-indicator");
			var powerDrainedTemp = barStart.X + (barEnd.X - barStart.X) * (Game.LocalPlayer.PowerDrained / powerScaleBy) - Game.viewport.Location.X;
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(powerDrainedTemp), powerDrainedTemp, .3f);
			float2 powerDrainLevel = new float2(lastPowerDrainedPos.Value-indicator.size.X/2, barStart.Y - Game.viewport.Location.Y-1);
		
			rgbaRenderer.DrawSprite(indicator, powerDrainLevel, PaletteType.Chrome);
			rgbaRenderer.Flush();
		}

		void DrawButtons()
		{
			int2 buttonOrigin = new int2(Game.viewport.Width - 320, 2);
			// Repair
			Rectangle repairRect = new Rectangle(buttonOrigin.X, buttonOrigin.Y, repairButton.Image.bounds.Width, repairButton.Image.bounds.Height);
			var repairDrawPos = Game.viewport.Location + new float2(repairRect.Location);

			var hasFact = Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer && a.traits.Contains<ConstructionYard>());

			if (Game.Settings.RepairRequiresConyard && !hasFact)
				repairButton.ReplaceAnim("disabled");
			else
			{
				repairButton.ReplaceAnim(Game.controller.orderGenerator is RepairOrderGenerator ? "pressed" : "normal");
				AddButton(repairRect, isLmb => Game.controller.ToggleInputMode<RepairOrderGenerator>());
			}
			shpRenderer.DrawSprite(repairButton.Image, repairDrawPos, PaletteType.Chrome);
			
			// Sell
			Rectangle sellRect = new Rectangle(buttonOrigin.X+40, buttonOrigin.Y, 
				sellButton.Image.bounds.Width, sellButton.Image.bounds.Height);

			var sellDrawPos = Game.viewport.Location + new float2(sellRect.Location);

			sellButton.ReplaceAnim(Game.controller.orderGenerator is SellOrderGenerator ? "pressed" : "normal");
			
			AddButton(sellRect, isLmb => Game.controller.ToggleInputMode<SellOrderGenerator>());
			shpRenderer.DrawSprite(sellButton.Image, sellDrawPos, PaletteType.Chrome);
			shpRenderer.Flush();

			if (Game.Settings.PowerDownBuildings)
			{
				// Power Down
				Rectangle pwrdownRect = new Rectangle(buttonOrigin.X+80, buttonOrigin.Y,
					pwrdownButton.Image.bounds.Width, pwrdownButton.Image.bounds.Height);

				var pwrdownDrawPos = Game.viewport.Location + new float2(pwrdownRect.Location);

				pwrdownButton.ReplaceAnim(Game.controller.orderGenerator is PowerDownOrderGenerator ? "pressed" : "normal");

				AddButton(pwrdownRect, isLmb => Game.controller.ToggleInputMode<PowerDownOrderGenerator>());
				shpRenderer.DrawSprite(pwrdownButton.Image, pwrdownDrawPos, PaletteType.Chrome);
			}
			shpRenderer.Flush();
			
			//Options
			Rectangle optionsRect = new Rectangle(0,0, optionsButton.Image.bounds.Width, 
				optionsButton.Image.bounds.Height);
			
			var optionsDrawPos = Game.viewport.Location + new float2(optionsRect.Location);
			
			optionsButton.ReplaceAnim(optionsPressed ? "left-pressed" : "left-normal");
			
			AddButton(optionsRect, isLmb => optionsPressed = !optionsPressed);
			shpRenderer.DrawSprite(optionsButton.Image, optionsDrawPos, PaletteType.Chrome);
			shpRenderer.Flush();
			
			renderer.DrawText("Options", new int2(80, -2) , Color.White);
		}
		
		void DrawOptionsMenu()
		{
			if (optionsPressed){
				var menuDrawPos = Game.viewport.Location + new float2(Game.viewport.Width/2, Game.viewport.Height/2);
				var width = optionsTop.bounds.Width + optionsTopLeft.bounds.Width + optionsTopRight.bounds.Width;
				var height = optionsLeft.bounds.Height + optionsTopLeft.bounds.Height + optionsBottomLeft.bounds.Height;
				var adjust = 8;
				
				menuDrawPos = menuDrawPos + new float2(-width/2, -height/2);
				
				var backgroundDrawPos = menuDrawPos + new float2( (width - optionsBackground.bounds.Width)/2, (height - optionsBackground.bounds.Height)/2);
				
				//draw background
				shpRenderer.DrawSprite(optionsBackground, backgroundDrawPos, PaletteType.Chrome);
				
				//draw borders
				shpRenderer.DrawSprite(optionsTopLeft, menuDrawPos, PaletteType.Chrome);
				shpRenderer.DrawSprite(optionsLeft, menuDrawPos + new float2(0, optionsTopLeft.bounds.Height), PaletteType.Chrome);
				shpRenderer.DrawSprite(optionsBottomLeft, menuDrawPos + new float2(0, optionsTopLeft.bounds.Height + optionsLeft.bounds.Height), PaletteType.Chrome);

				shpRenderer.DrawSprite(optionsTop, menuDrawPos + new float2(optionsTopLeft.bounds.Width, 0), PaletteType.Chrome);
				shpRenderer.DrawSprite(optionsTopRight, menuDrawPos + new float2(optionsTopLeft.bounds.Width + optionsTop.bounds.Width, 0), PaletteType.Chrome);

				shpRenderer.DrawSprite(optionsBottom, menuDrawPos + new float2(optionsTopLeft.bounds.Width, optionsTopLeft.bounds.Height + optionsLeft.bounds.Height +adjust), PaletteType.Chrome);
				shpRenderer.DrawSprite(optionsBottomRight, menuDrawPos + new float2(optionsBottomLeft.bounds.Width + optionsBottom.bounds.Width, optionsTopLeft.bounds.Height + optionsLeft.bounds.Height), PaletteType.Chrome);

				shpRenderer.DrawSprite(optionsRight, menuDrawPos + new float2(optionsTopLeft.bounds.Width + optionsTop.bounds.Width + adjust + 1, optionsTopRight.bounds.Height), PaletteType.Chrome);
				
				shpRenderer.Flush();
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
		
		void TickPaletteAnimation()
		{
			Log.Write("{0} {1} {2} {3}", paletteAnimationFrame, paletteOrigin.X, paletteAnimating, paletteOpen);
			
			if (!paletteAnimating)
				return;

			// Increment frame
			if (paletteOpen)
				paletteAnimationFrame++;
			else
				paletteAnimationFrame--;

			Log.Write("{0}",paletteAnimationFrame);
			
			// Calculate palette position
			if (paletteAnimationFrame <= paletteAnimationLength)
				paletteOrigin = float2.Lerp(paletteClosedOrigin, paletteOpenOrigin, paletteAnimationFrame * 1.0f / paletteAnimationLength);

			// Play radar-on sound at the start of the activate anim (open)
			if (paletteAnimationFrame == 1 && paletteOpen)
				Sound.Play("bleep13.aud");

			// Play radar-on sound at the start of the activate anim (close)
			if (paletteAnimationFrame == paletteAnimationLength + -1 && !paletteOpen)
				Sound.Play("bleep13.aud");

			// Animation is complete
			if ((paletteAnimationFrame == 0 && !paletteOpen)
					|| (paletteAnimationFrame == paletteAnimationLength && paletteOpen))
			{
				paletteAnimating = false;
			}
		}
		
		
		// Return an int telling us the y coordinate at the bottom of the palette
		int DrawBuildPalette(string queueName)
		{
			// Hack
			int columns = paletteColumns;
			float2 origin = new float2(paletteOrigin.X + 9, paletteOrigin.Y + 9);
			
			if (queueName == null) return 0;

			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(Game.LocalPlayer, queueName).ToArray();

			var allItems = Rules.TechTree.AllItems(Game.LocalPlayer, queueName)
				.Where(a => Rules.UnitInfo[a].TechLevel != -1)
				.OrderBy(a => Rules.UnitInfo[a].TechLevel);

			var queue = Game.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			var overlayBits = new List<Pair<Sprite, float2>>();

			string tooltipItem = null;

			// Draw the top border
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, paletteCollection, "top"), new float2(origin.X - 9, origin.Y - 9), PaletteType.Chrome);

			// Draw the icons
			int lasty = -1;
			foreach (var item in allItems)
			{
				// Draw the background for this row
				if (y != lasty)
				{
					rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, paletteCollection, "bg-" + (y % 4).ToString()), new float2(origin.X - 9, origin.Y + 48 * y), PaletteType.Chrome);
					rgbaRenderer.Flush();
					lasty = y;
				}
				
				var rect = new RectangleF(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = Game.viewport.Location + new float2(rect.Location);
				var isBuildingSomething = queue.CurrentItem(queueName) != null;

				shpRenderer.DrawSprite(tabSprites[item], drawPos, PaletteType.Chrome);

				var firstOfThis = queue.AllItems(queueName).FirstOrDefault(a => a.Item == item);

				if (rect.Contains(lastMousePos.ToPoint()))
					tooltipItem = item;

				var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

				if (firstOfThis != null)
				{
					clock.PlayFetchIndex( "idle", 
						() => (firstOfThis.TotalTime - firstOfThis.RemainingTime) 
							* NumClockFrames / firstOfThis.TotalTime);
					clock.Tick();

					shpRenderer.DrawSprite(clock.Image, drawPos, PaletteType.Chrome);

					if (firstOfThis.Done)
					{
						ready.Play("ready");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}
					else if (firstOfThis.Paused)
					{
						ready.Play("hold");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}

					var repeats = queue.AllItems(queueName).Count(a => a.Item == item);
					if (repeats > 1 || queue.CurrentItem(queueName) != firstOfThis)
					{
						var offset = -22;
						var digits = repeats.ToString();
						foreach (var d in digits)
						{
							ready.PlayFetchIndex("groups", () => d - '0');
							ready.Tick();
							overlayBits.Add(Pair.New(ready.Image, overlayPos + new float2(offset, 0)));
							offset += 6;
						}
					}
				}
				else
					if (!buildableItems.Contains(item) || isBuildingSomething)
						overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				var closureItem = item;
				AddButton(rect, isLmb => HandleBuildPalette(closureItem, isLmb));

	
				if (++x == columns) { x = 0; y++; }
			}
			if (x != 0) y++;
			
			while (y < paletteRows)
			{
				rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, paletteCollection, "bg-" + (y % 4).ToString()), new float2(origin.X - 9, origin.Y + 48 * y), PaletteType.Chrome);
				y++;
			}

			foreach (var ob in overlayBits)
				shpRenderer.DrawSprite(ob.First, ob.Second, PaletteType.Chrome);

			shpRenderer.Flush();
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, paletteCollection, "bottom"), new float2(origin.X - 9, origin.Y - 1 + 48 * y), PaletteType.Chrome);

			// Draw dock
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, paletteCollection, "dock-top"), new float2(Game.viewport.Width - 14, origin.Y - 23), PaletteType.Chrome);
			for (int i = 0; i < y; i++)
			{
				rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, paletteCollection, "dock-" + (y % 4).ToString()), new float2(Game.viewport.Width - 14, origin.Y + 48 * i), PaletteType.Chrome);
			}
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, paletteCollection, "dock-bottom"), new float2(Game.viewport.Width - 14, origin.Y - 1 + 48 * y), PaletteType.Chrome);
			rgbaRenderer.Flush();

			if (tooltipItem != null)
				DrawProductionTooltip(tooltipItem, new float2(Game.viewport.Width, origin.Y + y * 48 + 9).ToInt2()/*tooltipPos*/);
				
			return y*48+9;
		}

		void StartProduction( string item )
		{
			var group = Rules.UnitCategory[item];
			Sound.Play((group == "Building" || group == "Defense") ? "abldgin1.aud" : "train1.aud");
			Game.controller.AddOrder(Order.StartProduction(Game.LocalPlayer, item));
		}

		void HandleBuildPalette(string item, bool isLmb)
		{
			var player = Game.LocalPlayer;
			var group = Rules.UnitCategory[item];
			var queue = player.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var producing = queue.AllItems(group).FirstOrDefault( a => a.Item == item );

			Sound.Play("ramenu1.aud");

			if (isLmb)
			{
				if (producing != null && producing == queue.CurrentItem(group))
				{
					if (producing.Done)
					{
						if (group == "Building" || group == "Defense")
							Game.controller.orderGenerator = new PlaceBuildingOrderGenerator(player.PlayerActor, item);
						return;
					}

					if (producing.Paused)
					{
						Game.controller.AddOrder(Order.PauseProduction(player, item, false));
						return;
					}
				}

				StartProduction(item);
			}
			else
			{
				if (producing != null)
				{
					// instant cancel of things we havent really started yet, and things that are finished
					if (producing.Paused || producing.Done || producing.TotalCost == producing.RemainingCost)
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
			var tooltipSprite = SequenceProvider.GetImageFromCollection(renderer, chromeCollection, "tooltip-bg");
			var p = pos.ToFloat2() - new float2(tooltipSprite.size.X, 0);
			rgbaRenderer.DrawSprite(tooltipSprite, p, PaletteType.Chrome);
			rgbaRenderer.Flush();

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
				var prereqs = info.Prerequisite
					.Select(a => Rules.UnitInfo[a.ToLowerInvariant()])
					.Where( u => u.Owner.Any( o => o == Game.LocalPlayer.Race ) )
					.Select( a => a.Description );
				renderer.DrawText("Requires {0}".F( string.Join( ", ", prereqs.ToArray() ) ), p.ToInt2(),
					Color.White);
			}

			if (info.LongDesc != null)
			{
				p += new int2(0, 15);
				renderer.DrawText(info.LongDesc.Replace( "\\n", "\n" ), p.ToInt2(), Color.White);
			}
		}

		void DrawSupportPowers()
		{
			var numPowers = Game.LocalPlayer.SupportPowers.Values
				.Where(a => a.IsAvailable).Count();

			if (numPowers == 0) return;

			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, chromeCollection, "specialbin-top"), new float2(0, 14), PaletteType.Chrome);
			for (var i = 1; i < numPowers; i++)
				rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, chromeCollection, "specialbin-middle"), new float2(0, 14 + i * 51), PaletteType.Chrome);
			rgbaRenderer.DrawSprite(SequenceProvider.GetImageFromCollection(renderer, chromeCollection, "specialbin-bottom"), new float2(0, 14 + numPowers * 51), PaletteType.Chrome);

			rgbaRenderer.Flush();

			var y = 24;

			string tooltipItem = null;
			int2 tooltipPos = int2.Zero;

			foreach (var sp in Game.LocalPlayer.SupportPowers)
			{
				var image = spsprites[sp.Key];
				if (sp.Value.IsAvailable)
				{
					var drawPos = Game.viewport.Location + new float2(5, y);
					shpRenderer.DrawSprite(image, drawPos, PaletteType.Chrome);

					clock.PlayFetchIndex("idle",
						() => (sp.Value.TotalTime - sp.Value.RemainingTime)
							* NumClockFrames / sp.Value.TotalTime);
					clock.Tick();

					shpRenderer.DrawSprite(clock.Image, drawPos, PaletteType.Chrome);

					var rect = new Rectangle(5, y, 64, 48);
					if (sp.Value.IsDone)
					{
						ready.Play("ready");
						shpRenderer.DrawSprite(ready.Image, 
							drawPos + new float2((64 - ready.Image.size.X) / 2, 2), 
							PaletteType.Chrome);

						AddButton(rect, HandleSupportPower( sp.Value ));
					}

					if (rect.Contains(lastMousePos.ToPoint()))
					{
						tooltipItem = sp.Key;
						tooltipPos = drawPos.ToInt2() + new int2(72, 0) - Game.viewport.Location.ToInt2();
					}

					y += 51;
				}
			}

			shpRenderer.Flush();

			if (tooltipItem != null)
				DrawSupportPowerTooltip(tooltipItem, tooltipPos);
		}

		Action<bool> HandleSupportPower(SupportPower sp)
		{
			return b => { if (b) sp.Activate(); };
		}

		string FormatTime(int ticks)
		{
			var seconds = ticks / 25;
			var minutes = seconds / 60;

			return "{0:D2}:{1:D2}".F(minutes, seconds % 60);
		}

		void DrawSupportPowerTooltip(string sp, int2 pos)
		{
			var tooltipSprite = SequenceProvider.GetImageFromCollection(renderer, chromeCollection, "tooltip-bg");
			rgbaRenderer.DrawSprite(tooltipSprite, pos, PaletteType.Chrome);
			rgbaRenderer.Flush();

			var info = Rules.SupportPowerInfo[sp];

			pos += new int2(5, 5);

			renderer.DrawText2(info.Description, pos, Color.White);

			var timer = "Charge Time: {0}".F(FormatTime(Game.LocalPlayer.SupportPowers[sp].RemainingTime));
			DrawRightAligned(timer, pos + new int2((int)tooltipSprite.size.X - 10, 0), Color.White);

			if (info.LongDesc != null)
			{
				pos += new int2(0, 25);
				renderer.DrawText(info.LongDesc.Replace("\\n", "\n"), pos, Color.White);
			}
		}
	}
}
