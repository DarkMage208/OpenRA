#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Server;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class ServerBrowserDelegate : IWidgetDelegate
	{
		static List<Widget> GameButtons = new List<Widget>();

		GameServer currentServer = null;
		Widget ServerTemplate;

		[ObjectCreator.UseCtor]
		public ServerBrowserDelegate( [ObjectCreator.Param] Widget widget )
		{
			var bg = widget.GetWidget("JOINSERVER_BG");

			MasterServerQuery.OnComplete += games => RefreshServerList(games);

			bg.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
			bg.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";

			bg.Children.RemoveAll(a => GameButtons.Contains(a));
			GameButtons.Clear();

			MasterServerQuery.Refresh(Game.Settings.Server.MasterServer);

			bg.GetWidget("SERVER_INFO").IsVisible = () => currentServer != null;
			var preview = bg.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			preview.Map = () => CurrentMap();
			preview.IsVisible = () => CurrentMap() != null;

			bg.GetWidget<LabelWidget>("SERVER_IP").GetText = () => currentServer.Address;
			bg.GetWidget<LabelWidget>("SERVER_MODS").GetText = () => GenerateModsLabel();
			bg.GetWidget<LabelWidget>("MAP_TITLE").GetText = () => (CurrentMap() != null) ? CurrentMap().Title : "Unknown";
			bg.GetWidget<LabelWidget>("MAP_PLAYERS").GetText = () =>
			{
				if (currentServer == null)
					return "";
				string ret = currentServer.Players.ToString();
				if (CurrentMap() != null)
					ret += "/" + CurrentMap().PlayerCount.ToString();
				return ret;
			};


			var sl = bg.GetWidget<ScrollPanelWidget>("SERVER_LIST");
			ServerTemplate = sl.GetWidget<LabelWidget>("SERVER_TEMPLATE");

			bg.GetWidget("REFRESH_BUTTON").OnMouseUp = mi =>
			{
				bg.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				bg.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";

				bg.Children.RemoveAll(a => GameButtons.Contains(a));
				GameButtons.Clear();

				MasterServerQuery.Refresh(Game.Settings.Server.MasterServer);

				return true;
			};

			bg.GetWidget("CANCEL_BUTTON").OnMouseUp = mi =>
			{
				Widget.CloseWindow();
				return true;
			};

			bg.GetWidget("DIRECTCONNECT_BUTTON").OnMouseUp = mi =>
			{
				Widget.CloseWindow();
				Widget.OpenWindow("DIRECTCONNECT_BG");
				return true;
			};

			bg.GetWidget("JOIN_BUTTON").OnMouseUp = mi =>
			{
				if (currentServer == null)
					return false;

				Widget.CloseWindow();
				Game.JoinServer(currentServer.Address.Split(':')[0], int.Parse(currentServer.Address.Split(':')[1]));
				return true;
			};
		}

		Map CurrentMap()
		{
			return (currentServer == null || !Game.modData.AvailableMaps.ContainsKey(currentServer.Map))
				? null : Game.modData.AvailableMaps[currentServer.Map];
		}
		
		string GenerateModsLabel()
		{
			return string.Join("\n", currentServer.UsefulMods
				.Select(m => 
			       Mod.AllMods.ContainsKey(m.Key) ? string.Format("{0} ({1})", Mod.AllMods[m.Key].Title, m.Value)
			                                   : string.Format("Unknown Mod: {0}",m.Key)).ToArray());
		}

		void RefreshServerList(IEnumerable<GameServer> games)
		{
			var r = Widget.RootWidget;
			var bg = r.GetWidget("JOINSERVER_BG");

            if (bg == null) // We got a MasterServer reply AFTER the browser is gone, just return to prevent crash - Gecko
                return;

			var sl = bg.GetWidget<ScrollPanelWidget>("SERVER_LIST");

			sl.RemoveChildren();
			currentServer = null;

			if (games == null)
			{
				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Failed to contact master server.";
				return;
			}

            var gamesWaiting = games.Where(g => CanJoin(g));

            if (gamesWaiting.Count() == 0)
			{
				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "No games found.";
				return;
			}

			r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = false;

			int i = 0;
            foreach (var loop in gamesWaiting)
			{
				var game = loop;
				var template = ServerTemplate.Clone() as LabelWidget;
				template.Id = "JOIN_GAME_{0}".F(i);
				template.GetText = () => "   {0} ({1})".F(			/* /8 = hack */
						game.Name,
						game.Address);
				template.GetBackground = () => (currentServer == game) ? "dialog2" : null;
				template.OnMouseDown = mi => { if (mi.Button != MouseButton.Left) return false; currentServer = game; return true; };
				template.IsVisible = () => true;
				sl.AddChild(template);

				if (i == 0) currentServer = game;
				i++;
			}
		}
		
		bool CanJoin(GameServer game)
		{
			//"waiting for players"
			if (game.State != 1)
				return false;
			
			// Mods won't match if there are a different number
			if (Game.CurrentMods.Count != game.Mods.Count())
				return false;
			
			return game.Mods.All( m => m.Contains('@')) &&  game.Mods.Select( m => Pair.New(m.Split('@')[0], m.Split('@')[1]))
				.All(kv => Game.CurrentMods.ContainsKey(kv.First) &&
				     (kv.Second == "{DEV_VERSION}" || Game.CurrentMods[kv.First].Version == "{DEV_VERSION}" || kv.Second == Game.CurrentMods[kv.First].Version));
		}
		
	}

	public class DirectConnectDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public DirectConnectDelegate( [ObjectCreator.Param] Widget widget )
		{
			var dc = widget.GetWidget("DIRECTCONNECT_BG");

			dc.GetWidget<TextFieldWidget>("SERVER_ADDRESS").Text = Game.Settings.Player.LastServer;

            dc.GetWidget("JOIN_BUTTON").OnMouseUp = mi =>
            {
                var address = dc.GetWidget<TextFieldWidget>("SERVER_ADDRESS").Text;
                var cpts = address.Split(':').ToArray();
                if (cpts.Length < 1 || cpts.Length > 2)
                    return true;

                int port;
                if (cpts.Length != 2 || !int.TryParse(cpts[1], out port))
                    port = 1234;

                Game.Settings.Player.LastServer = address;
                Game.Settings.Save();

                Widget.CloseWindow();
                Game.JoinServer(cpts[0], port);
                return true;
            };

			dc.GetWidget("CANCEL_BUTTON").OnMouseUp = mi =>
			{
				Widget.CloseWindow();
				Widget.OpenWindow("MAINMENU_BG");
				return true;
			};
		}
	}
}
