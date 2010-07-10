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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA
{
	class Chrome : IHandleInput
	{
		//todo: remove when have a real chat widget
		public static int ChatWidth = 760;
		
		public readonly Renderer renderer;
		public readonly LineRenderer lineRenderer;

		SpriteRenderer rgbaRenderer { get { return renderer.RgbaSpriteRenderer; } }
		SpriteRenderer shpRenderer { get { return renderer.WorldSpriteRenderer; } }
	
		readonly List<Pair<RectangleF, Action<bool>>> buttons = new List<Pair<RectangleF, Action<bool>>>();

		internal MapStub currentMap;

		public Chrome(Renderer r, Manifest m)
		{
			this.renderer = r;
			lineRenderer = new LineRenderer(renderer);
					
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
				
		public void Tick(World world)
		{
			if (!world.GameHasStarted) return;
			if (world.LocalPlayer == null) return;

			++ticksSinceLastMove;
			
			rootWidget.Tick(world);
		}
				
		public void Draw( World world )
		{
			buttons.Clear();
			renderer.Device.DisableScissor();
			
			var typingArea = new Rectangle(240, Game.viewport.Height - 30, Game.viewport.Width - 420, 30);
			var chatLogArea = new Rectangle(240, Game.viewport.Height - 500, Game.viewport.Width - 420, 500 - 40);
			DrawChat(typingArea, chatLogArea);
		}
		
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
			buttons.Clear();

			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );

			AddUiButton(new int2(r.Left + 200, r.Bottom - 40), "OK",
				_ =>
				{
					Game.IssueOrder(Order.Chat("/map " + currentMap.Uid));
					Chrome.rootWidget.CloseWindow();
				});

			AddUiButton(new int2(r.Right - 200, r.Bottom - 40), "Cancel",
				_ =>
				{
					Chrome.rootWidget.CloseWindow();
				});

			var mapContainer = new Rectangle(r.Right - 280, r.Top + 30, 256, 256);

			var y = r.Top + 50;
					
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
				AddButton(itemRect, _ => { currentMap = closureMap; });
				y += 20;
			}

			y = mapContainer.Bottom + 20;
			DrawCentered("Title: {0}".F(currentMap.Title),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Size: {0}x{1}".F(currentMap.Width, currentMap.Height),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;

			DrawCentered("Theater: {0}".F(Rules.TileSets[currentMap.Tileset].Name),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);
			y += 20;
			DrawCentered("Spawnpoints: {0}".F(currentMap.PlayerCount),
				new int2(mapContainer.Left + mapContainer.Width / 2, y), Color.White);

			AddButton(r, _ => { });
		}
		

		public void DrawWidgets(World world) { rootWidget.Draw(world); shpRenderer.Flush(); rgbaRenderer.Flush(); lineRenderer.Flush(); }
		
		public void DrawLobby()
		{
			buttons.Clear();

			if( Game.LobbyInfo.GlobalSettings.Map == null )
				currentMap = null;
			else
				currentMap = Game.AvailableMaps[ Game.LobbyInfo.GlobalSettings.Map ];
			
			var w = 800;
			var h = 600;
			var r = new Rectangle( (Game.viewport.Width - w) / 2, (Game.viewport.Height - h) / 2, w, h );

			var typingBox = new Rectangle(r.Left + 20, r.Bottom - 77, r.Width - 40, 27);
			var chatBox = new Rectangle(r.Left + 20, r.Bottom - 269, r.Width - 40, 190);

			DrawDialogBackground(typingBox, "dialog2");
			DrawDialogBackground(chatBox, "dialog3");

			DrawChat(typingBox, chatBox);
		}
		
		void AddButton(RectangleF r, Action<bool> b) { buttons.Add(Pair.New(r, b)); }

		void DrawDialogBackground(Rectangle r, string collection)
		{
			WidgetUtils.DrawPanel(collection, r);
		}

		void DrawChat(Rectangle typingArea, Rectangle chatLogArea)
		{
			
			var chatpos = new int2(chatLogArea.X + 10, chatLogArea.Bottom - 6);
			ChatWidth = chatLogArea.Width - 10;
			
			renderer.Device.EnableScissor(typingArea.Left, typingArea.Top, typingArea.Width, typingArea.Height);
			if (Game.chat.isChatting)
				RenderChatLine(new ChatLine { Owner = Game.chat.isTeamChat ? "TeamChat:" : "Chat:", Text = Game.chat.typing },
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

		void RenderChatLine(ChatLine line, int2 p)
		{
			var size = renderer.RegularFont.Measure(line.Owner);
			renderer.RegularFont.DrawText(line.Owner, p, line.Color);
			renderer.RegularFont.DrawText(line.Text, p + new int2(size.X + 10, 0), Color.White);
		}

		public int ticksSinceLastMove = 0;
		public int2 lastMousePos;
		public bool HandleInput(World world, MouseInput mi)
		{
			if (selectedWidget != null)
				return selectedWidget.HandleInput(mi);
				
			if (rootWidget.HandleInput(mi))
				return true;

			if (mi.Event == MouseInputEvent.Move)
			{
				lastMousePos = mi.Location;
				ticksSinceLastMove = 0;
			}

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

		void DrawCentered(string text, int2 pos, Color c)
		{
			renderer.BoldFont.DrawText(text, pos - new int2(renderer.BoldFont.Measure(text).X / 2, 0), c);
		}
	}
}
