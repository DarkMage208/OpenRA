#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA
{
	class Chrome : IHandleInput
	{
		public readonly Renderer renderer;
		public readonly LineRenderer lineRenderer;

		SpriteRenderer rgbaRenderer { get { return renderer.RgbaSpriteRenderer; } }
		SpriteRenderer shpRenderer { get { return renderer.WorldSpriteRenderer; } }
		
		string chromeCollection;
		string radarCollection;
		string paletteCollection;
		string digitCollection;
		
		// Build Palette tabs
		string currentTab = "Building";
		bool paletteOpen = false;
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
		
		// mapchooser
		Sheet mapChooserSheet;
		Sprite mapChooserSprite;
		int mapOffset = 0;
						
		public Chrome(Renderer r, Manifest m)
		{
			this.renderer = r;
			lineRenderer = new LineRenderer(renderer);
		
			tabSprites = Rules.Info.Values
				.Where(u => u.Traits.Contains<BuildableInfo>())
				.ToDictionary(
					u => u.Name,
					u => SpriteSheetBuilder.LoadAllSprites(u.Traits.Get<BuildableInfo>().Icon ?? (u.Name + "icon"))[0]);

			var groups = Rules.Categories();
			
			tabImageNames = groups.Select(
				(g, i) => Pair.New(g,
					OpenRA.Graphics.Util.MakeArray(3,
						n => i.ToString())))
				.ToDictionary(a => a.First, a => a.Second);

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);

			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
			
			var widgetYaml = m.ChromeLayout.Select(a => MiniYaml.FromFile(a)).Aggregate(MiniYaml.Merge);
			
			if (rootWidget == null)
			{
				rootWidget = WidgetLoader.LoadWidget( widgetYaml.FirstOrDefault() );
				rootWidget.Initialize();
				rootWidget.InitDelegates();
				Widget.WindowList.Push("MAINMENU_BG");
			}
		}

		public static Widget rootWidget = null;
		public static Widget selectedWidget;
		
		List<string> visibleTabs = new List<string>();
		
		public void Tick(World world)
		{
			if (!world.GameHasStarted) return;
			if (world.LocalPlayer == null) return;
			
			TickPaletteAnimation();
			TickRadarAnimation();

			visibleTabs.Clear();
			foreach (var q in tabImageNames)
				if (!Rules.TechTree.BuildableItems(world.LocalPlayer, q.Key).Any())
				{
					CheckDeadTab(world, q.Key);
					if (currentTab == q.Key)
						currentTab = null;
				}
				else
					visibleTabs.Add(q.Key);

			if (currentTab == null)
				currentTab = visibleTabs.FirstOrDefault();
		}
				
		public void Draw( World world )
		{
			DrawDownloadBar();

			chromeCollection = "chrome-" + world.LocalPlayer.Country.Race;
			radarCollection = "radar-" + world.LocalPlayer.Country.Race;
			paletteCollection = "palette-" + world.LocalPlayer.Country.Race;
			digitCollection = "digits-" + world.LocalPlayer.Country.Race;

			buttons.Clear();

			renderer.Device.DisableScissor();
			
			DrawRadar( world );
			DrawPower( world );
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, chromeCollection, "moneybin"), new float2(Game.viewport.Width - 320, 0), "chrome");
			DrawMoney( world );
			rgbaRenderer.Flush();
			DrawButtons( world );
			
			int paletteHeight = DrawBuildPalette(world, currentTab);
			DrawBuildTabs(world, paletteHeight);
			DrawChat();
		}

		public void DrawDownloadBar()
		{
			if (PackageDownloader.IsIdle())
				return;

			var r = new Rectangle((Game.viewport.Width - 400) / 2, Game.viewport.Height - 110, 400, 100);
			DrawDialogBackground(r, "dialog");

			DrawCentered("Downloading: {0} (+{1} more)".F(
				PackageDownloader.CurrentPackage.Split(':')[0],
				PackageDownloader.RemainingPackages),
				new int2( Game.viewport.Width  /2, Game.viewport.Height - 90),
				Color.White);

			DrawDialogBackground(new Rectangle(r.Left + 30, r.Top + 50, r.Width - 60, 20),
				"dialog2");

			var x1 = r.Left + 35;
			var x2 = r.Right - 35;
			var x = float2.Lerp(x1, x2, PackageDownloader.Fraction);

			for (var y = r.Top + 55; y < r.Top + 65; y++)
				lineRenderer.DrawLine(
					new float2(x1, y) + Game.viewport.Location, 
					new float2(x, y) + Game.viewport.Location,
					Color.White, Color.White);

			lineRenderer.Flush();
		}

		public void DrawDialog(string text)
		{
			DrawDialog(text, null, _ => { }, null, _ => { });
		}
		
		public void DrawDialog(string text, string button1String, Action<bool> button1Action, string button2String, Action<bool> button2Action)
		{
			var w = Math.Max(renderer.BoldFont.Measure(text).X + 120, 400);
			var h = (button1String == null) ? 100 : 140;
			var r = new Rectangle((Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h);
			DrawDialogBackground(r, "dialog");
			DrawCentered(text, new int2(Game.viewport.Width / 2, Game.viewport.Height / 2 - ((button1String == null) ? 8 : 28)), Color.White);

			// don't allow clicks through the dialog
			AddButton(r, _ => { });
			
			if (button1String != null)
			{
				AddUiButton(new int2(r.Right - 300, r.Bottom - 40),button1String, button1Action);
			}
			
			if (button2String != null)
			{
				AddUiButton(new int2(r.Right - 100, r.Bottom - 40),button2String, button2Action);
			}
				
		}
		
		bool showMapChooser = false;
		MapStub currentMap;
		bool mapPreviewDirty = true;

		void AddUiButton(int2 pos, string text, Action<bool> a)
		{
			var rect = new Rectangle(pos.X - 160 / 2, pos.Y - 4, 160, 24);
			DrawDialogBackground( rect, "dialog2");
			DrawCentered(text, new int2(pos.X, pos.Y), Color.White);
			rgbaRenderer.Flush();
			AddButton(rect, a);
		}

		public void DrawMapChooser()
		{
			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );
			DrawDialogBackground(r, "dialog");
			DrawCentered("Choose Map", new int2(r.Left + w / 2, r.Top + 20), Color.White);
			rgbaRenderer.Flush();

			AddUiButton(new int2(r.Left + 200, r.Bottom - 40), "OK",
				_ =>
				{
					Game.IssueOrder(Order.Chat("/map " + currentMap.Uid));
					showMapChooser = false;
					mapPreviewDirty = true;
				});

			AddUiButton(new int2(r.Right - 200, r.Bottom - 40), "Cancel",
				_ =>
				{
					showMapChooser = false;
					mapPreviewDirty = true;
				});
			
			if (mapPreviewDirty)
			{
				if (mapChooserSheet == null || mapChooserSheet.Size.Width != currentMap.Width || mapChooserSheet.Size.Height != currentMap.Height)
					mapChooserSheet = new Sheet(renderer, new Size(currentMap.Width, currentMap.Height));
				
				mapChooserSheet.Texture.SetData(currentMap.Preview.Value);
				mapChooserSprite = new Sprite(mapChooserSheet, new Rectangle(0,0,currentMap.Width, currentMap.Height), TextureChannel.Alpha);
				mapPreviewDirty = false;
			}
			var mapBackground = new Rectangle(r.Right - 284, r.Top + 26, 264, 264);
			var mapContainer = new Rectangle(r.Right - 280, r.Top + 30, 256, 256);
			var mapRect = currentMap.PreviewBounds(new Rectangle(mapContainer.X,mapContainer.Y,mapContainer.Width,mapContainer.Height));
			
			DrawDialogBackground(mapBackground, "dialog3");
			rgbaRenderer.DrawSprite(mapChooserSprite, 
				new float2(mapRect.Location), 
				"chrome", 
				new float2(mapRect.Size));
			DrawSpawnPoints(currentMap,mapContainer);
			rgbaRenderer.Flush();
			
			var y = r.Top + 50;
			
			int maxListItems = ((r.Bottom - 60 - y ) / 20);
		
			// Don't bother showing a subset of the data
			// This will be fixed properly when we move the map list to widgets
			foreach (var kv in Game.AvailableMaps)
			{
				var map = kv.Value;
				if (!map.Selectable)
					continue;
				
				var itemRect = new Rectangle(r.Left + 50, y - 2, r.Width - 340, 20);
				if (map == currentMap)
				{
					rgbaRenderer.Flush();
					DrawDialogBackground(itemRect, "dialog2");
				}

				renderer.RegularFont.DrawText(map.Title, new int2(r.Left + 60, y), Color.White);
				rgbaRenderer.Flush();
				var closureMap = map;
				AddButton(itemRect, _ => { currentMap = closureMap; mapPreviewDirty = true; });
				y += 20;
			}

			y = mapContainer.Bottom + 20;
			DrawCentered("Title: {0}".F(currentMap.Title),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Size: {0}x{1}".F(currentMap.Width, currentMap.Height),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;
			
			var theaterInfo = Rules.Info["world"].Traits.WithInterface<TheaterInfo>().FirstOrDefault(t => t.Theater == currentMap.Tileset);
			DrawCentered("Theater: {0}".F(theaterInfo.Name),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Spawnpoints: {0}".F(currentMap.PlayerCount),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			
			AddButton(r, _ => { });
		}
		bool PaletteAvailable(int index) { return Game.LobbyInfo.Clients.All(c => c.PaletteIndex != index); }
		bool SpawnPointAvailable(int index) { return (index == 0) || Game.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }
		
		void CyclePalette(bool left)
		{
			var d = left ? +1 : Player.PlayerColors(Game.world).Count() - 1;

			var newIndex = ((int)Game.LocalClient.PaletteIndex + d) % Player.PlayerColors(Game.world).Count();
				
			while (!PaletteAvailable(newIndex) && newIndex != (int)Game.LocalClient.PaletteIndex)
				newIndex = (newIndex + d) % Player.PlayerColors(Game.world).Count();
			
			Game.IssueOrder(
				Order.Chat("/pal " + newIndex));
		}

		void CycleRace(bool left)
		{
			var countries = new[] { "Random" }.Concat(Game.world.GetCountries().Select(c => c.Name));
			var nextCountry = countries
				.SkipWhile(c => c != Game.LocalClient.Country)
				.Skip(1)
				.FirstOrDefault();

			if (nextCountry == null)
				nextCountry = countries.First();

			Game.IssueOrder(Order.Chat("/race " + nextCountry));
		}

		void CycleReady(bool left)
		{
			Game.IssueOrder(Order.Chat("/ready"));
		}

		void CycleSpawnPoint(bool left)
		{
			var d = left ? +1 : Game.world.Map.SpawnPoints.Count();

			var newIndex = (Game.LocalClient.SpawnPoint + d) % (Game.world.Map.SpawnPoints.Count()+1);

			while (!SpawnPointAvailable(newIndex) && newIndex != (int)Game.LocalClient.SpawnPoint)
				newIndex = (newIndex + d) % (Game.world.Map.SpawnPoints.Count()+1);

			Game.IssueOrder(
				Order.Chat("/spawn " + newIndex));
			
		}

		public void DrawWidgets(World world) { rootWidget.Draw(world); shpRenderer.Flush(); rgbaRenderer.Flush(); }
		public void DrawSpawnPoints(MapStub map, Rectangle container)
		{
			var points = map.Waypoints;
			//	.Select( (sp,i) => Pair.New(sp, Game.LobbyInfo.Clients.FirstOrDefault( 
			//		c => c.SpawnPoint == i + 1 ) ))
			//	.ToList();
			
			foreach (var p in points)
			{
				var pos = map.ConvertToPreview(p.Value,container);

				//if (p.Second == null)
					rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, "spawnpoints", "unowned"), pos, "chrome");
				//else
				//{
				//	lineRenderer.FillRect(new RectangleF(
				//		Game.viewport.Location.X + pos.X + 2,
				//		Game.viewport.Location.Y + pos.Y + 2,
				//		12, 12), Player.PlayerColors[ p.Second.PaletteIndex % Player.PlayerColors.Count() ].c);
				//
				//	rgbaRenderer.DrawSprite(ownedSpawnPoint, pos, "chrome");
				//}
			}

			lineRenderer.Flush();
			rgbaRenderer.Flush();
		}
		
		string lastMap = "";
		public void DrawLobby()
		{
			buttons.Clear();
			DrawDownloadBar();
			
			if (showMapChooser)
			{
				DrawMapChooser();
				return;
			}
			
			// HACK HACK HACK
			if (lastMap != Game.LobbyInfo.GlobalSettings.Map)
			{
				mapPreviewDirty = true;
				lastMap = Game.LobbyInfo.GlobalSettings.Map;
			}
			
			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );
			DrawDialogBackground(r, "dialog");
			DrawCentered("OpenRA Multiplayer Lobby", new int2(r.Left + w / 2, r.Top + 20), Color.White);
			rgbaRenderer.Flush();
			
			if (Game.LobbyInfo.GlobalSettings.Map != null)
			{
				var mapBackground = new Rectangle(r.Right - 268, r.Top + 39, 252, 252);
				var mapContainer = new Rectangle(r.Right - 264, r.Top + 43, 244, 244);
				var mapRect = Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].PreviewBounds(new Rectangle(mapContainer.X,mapContainer.Y,mapContainer.Width,mapContainer.Height));
				DrawDialogBackground(mapBackground,"dialog3");
				
				if (mapPreviewDirty)
				{
					if (mapChooserSheet == null || mapChooserSheet.Size.Width != Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].Width || mapChooserSheet.Size.Height != Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].Height)
						mapChooserSheet = new Sheet(renderer, new Size(Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].Width, Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].Height));
					
					mapChooserSheet.Texture.SetData(Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].Preview.Value);
					mapChooserSprite = new Sprite(mapChooserSheet, new Rectangle(0,0,Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].Width, Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map].Height), TextureChannel.Alpha);
					mapPreviewDirty = false;
				}
				
				rgbaRenderer.DrawSprite(mapChooserSprite, 
					new float2(mapRect.Location), 
					"chrome", 
					new float2(mapRect.Size));
					
				DrawSpawnPoints(Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map],mapContainer);
				rgbaRenderer.Flush();
				}
			
			if (Game.IsHost)
			{
				AddUiButton(new int2(r.Right - 100, r.Top + 300), "Change Map",
				_ =>
				{
					showMapChooser = true;					
					currentMap = Game.AvailableMaps[Game.LobbyInfo.GlobalSettings.Map];
					mapPreviewDirty = true;
				});
			}
			
			
			var f = renderer.BoldFont;
			f.DrawText("Name", new int2(r.Left + 40, r.Top + 50), Color.White);
			f.DrawText("Color", new int2(r.Left + 140, r.Top + 50), Color.White);
			f.DrawText("Faction", new int2(r.Left + 220, r.Top + 50), Color.White);
			f.DrawText("Status", new int2(r.Left + 290, r.Top + 50), Color.White);
			f.DrawText("Spawn", new int2(r.Left + 390, r.Top + 50), Color.White);

			rgbaRenderer.Flush();
				
			var y = r.Top + 80;
			foreach (var client in Game.LobbyInfo.Clients)
			{
				var isLocalPlayer = client.Index == Game.orderManager.Connection.LocalClientId;
				var paletteRect = new Rectangle(r.Left + 130, y - 2, 65, 22);

				if (isLocalPlayer)
				{
					// todo: name editing
					var nameRect = new Rectangle(r.Left + 30, y - 2, 95, 22);
					DrawDialogBackground(nameRect, "dialog3");

					DrawDialogBackground(paletteRect, "dialog3");
					AddButton(paletteRect, CyclePalette);

					var raceRect = new Rectangle(r.Left + 210, y - 2, 65, 22);
					DrawDialogBackground(raceRect, "dialog3");
					AddButton(raceRect, CycleRace);

					var readyRect = new Rectangle(r.Left + 280, y - 2, 95, 22);
					DrawDialogBackground(readyRect, "dialog3");
					AddButton(readyRect, CycleReady);
					
					var spawnPointRect = new Rectangle(r.Left + 380, y - 2, 70, 22);
					DrawDialogBackground(spawnPointRect, "dialog3");
					AddButton(spawnPointRect, CycleSpawnPoint);
				}

				shpRenderer.Flush();

				f = renderer.RegularFont;
				f.DrawText(client.Name, new int2(r.Left + 40, y), Color.White);
				lineRenderer.FillRect(RectangleF.FromLTRB(paletteRect.Left + Game.viewport.Location.X + 5,
															paletteRect.Top + Game.viewport.Location.Y + 5,
															paletteRect.Right + Game.viewport.Location.X - 5,
															paletteRect.Bottom+Game.viewport.Location.Y - 5),
													Player.PlayerColors(Game.world)[client.PaletteIndex % Player.PlayerColors(Game.world).Count()].c);
				lineRenderer.Flush();
				f.DrawText(client.Country, new int2(r.Left + 220, y), Color.White);
				f.DrawText(client.State.ToString(), new int2(r.Left + 290, y), Color.White);
				f.DrawText((client.SpawnPoint == 0) ? "-" : client.SpawnPoint.ToString(), new int2(r.Left + 410, y), Color.White);
				y += 30;

				rgbaRenderer.Flush();
			}

			var typingBox = new Rectangle(r.Left + 20, r.Bottom - 47, r.Width - 40, 27);
			var chatBox = new Rectangle(r.Left + 20, r.Bottom - 269, r.Width - 40, 220);

			DrawDialogBackground(typingBox, "dialog2");
			DrawDialogBackground(chatBox, "dialog3");

			DrawChat(typingBox, chatBox);

			// block clicks `through` the dialog
			AddButton(r, _ => { });
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

			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();
			
			// Play radar-on sound at the start of the activate anim (open)
			if (radarAnimationFrame == radarSlideAnimationLength && hasRadar)
				Sound.Play(eva.RadarUp);

			// Play radar-on sound at the start of the activate anim (close)
			if (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength - 1 && !hasRadar)
				Sound.Play(eva.RadarDown);

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
		
		void DrawRadar( World world )
		{
			var hasNewRadar = world.Queries.OwnedBy[world.LocalPlayer]
				.WithTrait<ProvidesRadar>()
				.Any(a => a.Trait.IsActive);
			
			if (hasNewRadar != hasRadar)
			{
				radarAnimating = true;
			}
			
			hasRadar = hasNewRadar;

			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "left"), radarOrigin, "chrome");
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "right"), radarOrigin + new float2(201, 0), "chrome");
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "bottom"), radarOrigin + new float2(0, 192), "chrome");	

			if (radarAnimating)
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "bg"), radarOrigin + new float2(9, 0), "chrome");	
			
			rgbaRenderer.Flush();

			if (radarAnimationFrame >= radarSlideAnimationLength)
			{
				RectangleF mapRect = new RectangleF(radarOrigin.X + 9, radarOrigin.Y+(192-radarMinimapHeight)/2, 192, radarMinimapHeight);
				world.Minimap.Draw(mapRect, false);
			}
		}
		
		void AddButton(RectangleF r, Action<bool> b) { buttons.Add(Pair.New(r, b)); }
		
		void DrawBuildTabs( World world, int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;

			var queue = world.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();

			foreach (var q in tabImageNames)
			{
				var groupName = q.Key;
				if (!visibleTabs.Contains(groupName))
					continue;

				string[] tabKeys = { "normal", "ready", "selected" };
				var producing = queue.CurrentItem(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				var race = world.LocalPlayer.Country.Race;
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer,"tabs-"+tabKeys[index], race+"-"+q.Key), new float2(x, y), "chrome");

				buttons.Add(Pair.New(new RectangleF(x, y, tabWidth, tabHeight),
					(Action<bool>)(isLmb => HandleTabClick(groupName))));
				y += tabHeight;
			}

			rgbaRenderer.Flush();
		}
		
		void HandleTabClick(string button)
		{
			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();
			Sound.Play(eva.TabClick);
			var wasOpen = paletteOpen;
			paletteOpen = (currentTab == button && wasOpen) ? false : true;
			currentTab = button;
			if (wasOpen != paletteOpen)
				paletteAnimating = true;
		}
		
		void CheckDeadTab( World world, string groupName )
		{
			var queue = world.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			foreach( var item in queue.AllItems( groupName ) )
				Game.IssueOrder(Order.CancelProduction(world.LocalPlayer, item.Item));		
		}

		void DrawMoney( World world )
		{
			var moneyDigits = world.LocalPlayer.DisplayCash.ToString();
			var x = Game.viewport.Width - 65;
			foreach (var d in moneyDigits.Reverse())
			{
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, digitCollection, (d - '0').ToString()), new float2(x, 6), "chrome");
				x -= 14;
			}
		}

		float? lastPowerProvidedPos;
		float? lastPowerDrainedPos;
		
		void DrawPower( World world )
		{
			// Nothing to draw
			if (world.LocalPlayer.PowerProvided == 0 && world.LocalPlayer.PowerDrained == 0)
				return;
			
			// Draw bar horizontally
			var barStart = powerOrigin + radarOrigin;
			var barEnd = barStart + new float2(powerSize.Width, 0);

			float powerScaleBy = 100;
			var maxPower = Math.Max(world.LocalPlayer.PowerProvided, world.LocalPlayer.PowerDrained);
			while (maxPower >= powerScaleBy) powerScaleBy *= 2;
			
			// Current power supply
			var powerLevelTemp = barStart.X + (barEnd.X - barStart.X) * (world.LocalPlayer.PowerProvided / powerScaleBy);
			lastPowerProvidedPos = float2.Lerp(lastPowerProvidedPos.GetValueOrDefault(powerLevelTemp), powerLevelTemp, .3f);
			float2 powerLevel = new float2(lastPowerProvidedPos.Value, barStart.Y);

			var color = Color.LimeGreen;
			if (world.LocalPlayer.GetPowerState() == PowerState.Low)
				color = Color.Orange;
			if (world.LocalPlayer.GetPowerState() == PowerState.Critical)
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
				lineRenderer.DrawLine(Game.viewport.Location + barStart + leftOffset, Game.viewport.Location + powerLevel + rightOffset, color, color);
			}
			lineRenderer.Flush();

			// Power usage indicator
			var indicator = ChromeProvider.GetImage(renderer, radarCollection, "power-indicator");
			var powerDrainedTemp = barStart.X + (barEnd.X - barStart.X) * (world.LocalPlayer.PowerDrained / powerScaleBy);
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(powerDrainedTemp), powerDrainedTemp, .3f);
			float2 powerDrainLevel = new float2(lastPowerDrainedPos.Value-indicator.size.X/2, barStart.Y-1);
		
			rgbaRenderer.DrawSprite(indicator, powerDrainLevel, "chrome");
			rgbaRenderer.Flush();
		}

		const int chromeButtonGap = 2;

		void DrawButtons( World world )
		{
			var origin = new int2(Game.viewport.Width - 200, 2);
			
			foreach (var cb in world.WorldActor.traits.WithInterface<IChromeButton>())
			{
				var state = cb.Enabled ? cb.Pressed ? "pressed" : "normal" : "disabled";
				var image = ChromeProvider.GetImage(renderer, cb.Image + "-button", state);
				
				origin.X -= (int)image.size.X + chromeButtonGap;
				rgbaRenderer.DrawSprite(image, origin, "chrome");

				var button = cb;
				AddButton(new RectangleF(origin.X, origin.Y, image.size.X, image.size.Y),
					_ => { if (button.Enabled) button.OnClick(); });
			}
		}

		void DrawDialogBackground(Rectangle r, string collection)
		{
			renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);

			string[] images = { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = images.Select(x => ChromeProvider.GetImage(renderer, collection, x)).ToArray();
			
			for( var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[8].size.X )
				for( var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[8].size.Y )
					rgbaRenderer.DrawSprite(ss[8], new float2(x, y), "chrome");

			//draw borders
			for (var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
			{
				rgbaRenderer.DrawSprite(ss[2], new float2(r.Left, y), "chrome");
				rgbaRenderer.DrawSprite(ss[3], new float2(r.Right - ss[3].size.X, y), "chrome");
			}

			for (var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[0].size.X)
			{
				rgbaRenderer.DrawSprite(ss[0], new float2(x, r.Top), "chrome");
				rgbaRenderer.DrawSprite(ss[1], new float2(x, r.Bottom - ss[1].size.Y), "chrome");
			}

			rgbaRenderer.DrawSprite(ss[4], new float2(r.Left, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[5], new float2(r.Right - ss[5].size.X, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[6], new float2(r.Left, r.Bottom - ss[6].size.Y), "chrome");
			rgbaRenderer.DrawSprite(ss[7], new float2(r.Right - ss[7].size.X, r.Bottom - ss[7].size.Y), "chrome");
			rgbaRenderer.Flush();

			renderer.Device.DisableScissor();
		}

		void DrawChat()
		{
			var typingArea = new Rectangle(400, Game.viewport.Height - 30, Game.viewport.Width - 420, 30);
			var chatLogArea = new Rectangle(400, Game.viewport.Height - 500, Game.viewport.Width - 420, 500 - 40);

			DrawChat(typingArea, chatLogArea);
		}

		void DrawChat(Rectangle typingArea, Rectangle chatLogArea)
		{
			var chatpos = new int2(chatLogArea.X + 10, chatLogArea.Bottom - 6);

			renderer.Device.EnableScissor(typingArea.Left, typingArea.Top, typingArea.Width, typingArea.Height);
			if (Game.chat.isChatting)
				RenderChatLine(Tuple.New(Color.White, "Chat:", Game.chat.typing), 
					new int2(typingArea.X + 10, typingArea.Y + 6));

			rgbaRenderer.Flush();
			renderer.Device.DisableScissor();

			renderer.Device.EnableScissor(chatLogArea.Left, chatLogArea.Top, chatLogArea.Width, chatLogArea.Height);
			foreach (var line in Game.chat.recentLines.AsEnumerable().Reverse())
			{
				chatpos.Y -= 20;
				RenderChatLine(line, chatpos);
			}

			rgbaRenderer.Flush();
			renderer.Device.DisableScissor();
		}

		void RenderChatLine(Tuple<Color, string, string> line, int2 p)
		{
			var size = renderer.RegularFont.Measure(line.b);
			renderer.RegularFont.DrawText(line.b, p, line.a);
			renderer.RegularFont.DrawText(line.c, p + new int2(size.X + 10, 0), Color.White);
		}
		
		void TickPaletteAnimation()
		{		
			if (!paletteAnimating)
				return;

			// Increment frame
			if (paletteOpen)
				paletteAnimationFrame++;
			else
				paletteAnimationFrame--;
			
			// Calculate palette position
			if (paletteAnimationFrame <= paletteAnimationLength)
				paletteOrigin = float2.Lerp(paletteClosedOrigin, paletteOpenOrigin, paletteAnimationFrame * 1.0f / paletteAnimationLength);

			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();
			
			// Play palette-open sound at the start of the activate anim (open)
			if (paletteAnimationFrame == 1 && paletteOpen)
				Sound.Play(eva.BuildPaletteOpen);

			// Play palette-close sound at the start of the activate anim (close)
			if (paletteAnimationFrame == paletteAnimationLength + -1 && !paletteOpen)
				Sound.Play(eva.BuildPaletteClose);

			// Animation is complete
			if ((paletteAnimationFrame == 0 && !paletteOpen)
					|| (paletteAnimationFrame == paletteAnimationLength && paletteOpen))
			{
				paletteAnimating = false;
			}
		}
		
		
		// Return an int telling us the y coordinate at the bottom of the palette
		int DrawBuildPalette( World world, string queueName )
		{
			// Hack
			int columns = paletteColumns;
			float2 origin = new float2(paletteOrigin.X + 9, paletteOrigin.Y + 9);
			
			if (queueName == null) return 0;

			var x = 0;
			var y = 0;

			var buildableItems = Rules.TechTree.BuildableItems(world.LocalPlayer, queueName).ToArray();

			var allBuildables = Rules.TechTree.AllBuildables(queueName)
				.Where(a => a.Traits.Get<BuildableInfo>().Owner.Contains(world.LocalPlayer.Country.Race))
				.OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder)
				.ThenBy(a => a.Traits.Get<BuildableInfo>().TechLevel).ToArray();

			var queue = world.LocalPlayer.PlayerActor.traits.Get<ProductionQueue>();

			var overlayBits = new List<Pair<Sprite, float2>>();

			string tooltipItem = null;

			// Draw the top border
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "top"), 
				new float2(origin.X - 9, origin.Y - 9), "chrome");

			var numActualRows = Math.Max((allBuildables.Length + columns - 1) / columns, paletteRows);
			for (var w = 0; w < numActualRows; w++)
				rgbaRenderer.DrawSprite(
					ChromeProvider.GetImage(renderer, paletteCollection,
					"bg-" + (w % 4).ToString()),
					new float2(origin.X - 9, origin.Y + 48 * w),
					"chrome");

			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "bottom"), 
				new float2(origin.X - 9, origin.Y - 1 + 48 * numActualRows), "chrome");

			rgbaRenderer.Flush();

			// Draw the icons
			foreach (var item in allBuildables)
			{	
				var rect = new RectangleF(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = new float2(rect.Location);
				var isBuildingSomething = queue.CurrentItem(queueName) != null;

				shpRenderer.DrawSprite(tabSprites[item.Name], drawPos, "chrome");

				var firstOfThis = queue.AllItems(queueName).FirstOrDefault(a => a.Item == item.Name);

				if (rect.Contains(lastMousePos.ToPoint()))
					tooltipItem = item.Name;

				var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

				if (firstOfThis != null)
				{
					clock.PlayFetchIndex( "idle", 
						() => (firstOfThis.TotalTime - firstOfThis.RemainingTime) 
							* (clock.CurrentSequence.Length - 1)/ firstOfThis.TotalTime);
					clock.Tick();
					shpRenderer.DrawSprite(clock.Image, drawPos, "chrome");

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

					var repeats = queue.AllItems(queueName).Count(a => a.Item == item.Name);
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
					if (!buildableItems.Contains(item.Name) || isBuildingSomething)
						overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				var closureItemName = item.Name;
				
				var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
				
				AddButton(rect, buildableItems.Contains(item.Name)
					? isLmb => HandleBuildPalette(world, closureItemName, isLmb)
					: (Action<bool>)(_ => Sound.Play(eva.TabClick)));
	
				if (++x == columns) { x = 0; y++; }
			}
			if (x != 0) y++;

			foreach (var ob in overlayBits)
				shpRenderer.DrawSprite(ob.First, ob.Second, "chrome");

			shpRenderer.Flush();
			
			// Draw dock
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "dock-top"), 
				new float2(Game.viewport.Width - 14, origin.Y - 23), "chrome");

			for (int i = 0; i < numActualRows; i++)
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "dock-" + (i % 4).ToString()), 
					new float2(Game.viewport.Width - 14, origin.Y + 48 * i), "chrome");

			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, paletteCollection, "dock-bottom"), 
				new float2(Game.viewport.Width - 14, origin.Y - 1 + 48 * numActualRows), "chrome");

			rgbaRenderer.Flush();

			if (tooltipItem != null && paletteOpen)
				DrawProductionTooltip(world, tooltipItem, 
					new float2(Game.viewport.Width, origin.Y + numActualRows * 48 + 9).ToInt2());
				
			return y*48+9;
		}

		void StartProduction( World world, string item )
		{
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var unit = Rules.Info[item];

			Sound.Play(unit.Traits.Contains<BuildingInfo>() ? eva.BuildingSelectAudio : eva.UnitSelectAudio);
			Game.IssueOrder(Order.StartProduction(world.LocalPlayer, item, 
				Game.controller.GetModifiers().HasModifier(Modifiers.Shift) ? 5 : 1));
		}

		void HandleBuildPalette( World world, string item, bool isLmb )
		{
			var player = world.LocalPlayer;
			var unit = Rules.Info[item];
			var queue = player.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var producing = queue.AllItems(unit.Category).FirstOrDefault( a => a.Item == item );

			Sound.Play(eva.TabClick);

			if (isLmb)
			{
				if (producing != null && producing == queue.CurrentItem(unit.Category))
				{
					if (producing.Done)
					{
						if (unit.Traits.Contains<BuildingInfo>())
							Game.controller.orderGenerator = new PlaceBuildingOrderGenerator(player.PlayerActor, item);
						return;
					}

					if (producing.Paused)
					{
						Game.IssueOrder(Order.PauseProduction(player, item, false));
						return;
					}
				}

				StartProduction(world, item);
			}
			else
			{
				if (producing != null)
				{
					// instant cancel of things we havent really started yet, and things that are finished
					if (producing.Paused || producing.Done || producing.TotalCost == producing.RemainingCost)
					{
						Sound.Play(eva.CancelledAudio);
						Game.IssueOrder(Order.CancelProduction(player, item));
					}
					else
					{
						Sound.Play(eva.OnHoldAudio);
						Game.IssueOrder(Order.PauseProduction(player, item, true));
					}
				}
			}
		}

		public int2 lastMousePos;
		public bool HandleInput(World world, MouseInput mi)
		{
			if (selectedWidget != null)
				return selectedWidget.HandleInput(mi);
				
			if (rootWidget.HandleInput(mi))
				return true;
			
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
			if (selectedWidget != null)
				return true;
			
			return rootWidget.HitTest(mousePos)
				|| buttons.Any(a => a.First.Contains(mousePos.ToPoint()));
		}

		void DrawRightAligned(string text, int2 pos, Color c)
		{
			renderer.BoldFont.DrawText(text, pos - new int2(renderer.BoldFont.Measure(text).X, 0), c);
		}

		void DrawCentered(string text, int2 pos, Color c)
		{
			renderer.BoldFont.DrawText(text, pos - new int2(renderer.BoldFont.Measure(text).X / 2, 0), c);
		}

		void DrawProductionTooltip(World world, string unit, int2 pos)
		{
			var tooltipSprite = ChromeProvider.GetImage(renderer, chromeCollection, "tooltip-bg");
			var p = pos.ToFloat2() - new float2(tooltipSprite.size.X, 0);
			rgbaRenderer.DrawSprite(tooltipSprite, p, "chrome");
			

			var info = Rules.Info[unit];
			var buildable = info.Traits.Get<BuildableInfo>();

			renderer.BoldFont.DrawText(buildable.Description, p.ToInt2() + new int2(5, 5), Color.White);

			DrawRightAligned( "${0}".F(buildable.Cost), pos + new int2(-5,5), 
				world.LocalPlayer.Cash + world.LocalPlayer.Ore >= buildable.Cost ? Color.White : Color.Red);

			var bi = info.Traits.GetOrDefault<BuildingInfo>();
			if (bi != null)
				DrawRightAligned("ϟ{0}".F(bi.Power), pos + new int2(-5, 20),
					world.LocalPlayer.PowerProvided - world.LocalPlayer.PowerDrained + bi.Power >= 0
					? Color.White : Color.Red);

			var buildings = Rules.TechTree.GatherBuildings( world.LocalPlayer );
			p += new int2(5, 5);
			p += new int2(0, 15);
			if (!Rules.TechTree.CanBuild(info, world.LocalPlayer, buildings))
			{
				var prereqs = buildable.Prerequisites
					.Select( a => Description( a ) );
				renderer.RegularFont.DrawText("Requires {0}".F(string.Join(", ", prereqs.ToArray())), p.ToInt2(),
					Color.White);
			}

			if (buildable.LongDesc != null)
			{
				p += new int2(0, 15);
				renderer.RegularFont.DrawText(buildable.LongDesc.Replace( "\\n", "\n" ), p.ToInt2(), Color.White);
			}

			rgbaRenderer.Flush();
		}

		static string Description( string a )
		{
			if( a[ 0 ] == '@' )
				return "any " + a.Substring( 1 );
			else
				return Rules.Info[ a.ToLowerInvariant() ].Traits.Get<BuildableInfo>().Description;
		}

		public void SetCurrentTab(string produces)
		{
			if (!paletteOpen)
				paletteAnimating = true;
			paletteOpen = true;
			currentTab = produces;
		}
	}
}
