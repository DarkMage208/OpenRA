#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Net;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class CreateServerMenuDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public CreateServerMenuDelegate( [ObjectCreator.Param( "widget" )] Widget cs )
		{
			var settings = Game.Settings;

			cs.GetWidget("BUTTON_CANCEL").OnMouseUp = mi => {
				Widget.CloseWindow();
				return true;
			};
			
			cs.GetWidget("BUTTON_START").OnMouseUp = mi => {
				var map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Key;
				
				settings.Server.Name = cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text;
				settings.Server.ListenPort = int.Parse(cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text);
				settings.Server.ExternalPort = int.Parse(cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text);
				settings.Save();

				Game.CreateAndJoinServer(settings, map);
				return true;
			};
			
			cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text = settings.Server.Name ?? "";
			cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();
			cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text = settings.Server.ExternalPort.ToString();
			cs.GetWidget<CheckboxWidget>("CHECKBOX_ONLINE").Bind(settings.Server, "AdvertiseOnline");
			cs.GetWidget<CheckboxWidget>("CHECKBOX_ONLINE").OnChange += _ => settings.Save();
		}
	}
}
