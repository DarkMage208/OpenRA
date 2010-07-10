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

using System.Net;
using System.Linq;
using System.Collections.Generic;
namespace OpenRA.Widgets.Delegates
{
	public class CreateServerMenuDelegate : IWidgetDelegate
	{		
		public CreateServerMenuDelegate()
		{
			var r = Chrome.rootWidget;
			var cs = Chrome.rootWidget.GetWidget("CREATESERVER_BG");
			r.GetWidget("MAINMENU_BUTTON_CREATE").OnMouseUp = mi => {
				r.OpenWindow("CREATESERVER_BG");
				return true;
			};
			
			cs.GetWidget("BUTTON_CANCEL").OnMouseUp = mi => {
				r.CloseWindow();
				return true;
			};
			
			cs.GetWidget("BUTTON_START").OnMouseUp = mi => {
				r.OpenWindow("SERVER_LOBBY");
				Log.Write("debug", "Creating server");
				
				// TODO: Get this from a map chooser
				string map = Game.AvailableMaps.Keys.FirstOrDefault();
				
				// TODO: Get this from a mod chooser
				var mods = Game.Settings.InitialMods;
				
				var gameName = cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text;

				int listenPort = int.Parse(cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text);
				int extPort = int.Parse(cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text);
				
				Server.Server.ServerMain(Game.Settings.InternetServer, Game.Settings.MasterServer,
										gameName, listenPort, extPort, mods, map);

				Log.Write("debug", "Joining server");
				Game.JoinServer(IPAddress.Loopback.ToString(), Game.Settings.ListenPort);
				return true;
			};
			
			cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = Game.Settings.ListenPort.ToString();
			cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text = Game.Settings.ExternalPort.ToString();
			cs.GetWidget<CheckboxWidget>("CHECKBOX_ONLINE").Checked = () => Game.Settings.InternetServer;
			cs.GetWidget("CHECKBOX_ONLINE").OnMouseDown = mi => {
				Game.Settings.InternetServer ^= true;
				Game.Settings.Save();
				return true;	
			};
		}
	}
}
