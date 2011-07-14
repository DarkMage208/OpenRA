#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ConnectionDialogsLogic
	{
		[ObjectCreator.UseCtor]
		public ConnectionDialogsLogic(
			[ObjectCreator.Param] Widget widget,
			[ObjectCreator.Param] string host,
			[ObjectCreator.Param] int port )
		{
			widget.GetWidget<ButtonWidget>("CONNECTION_BUTTON_ABORT").OnMouseUp = mi => {
				widget.GetWidget("CONNECTION_BUTTON_ABORT").Parent.Visible = false;
				Game.Disconnect();
				Game.LoadShellMap();
				Widget.CloseWindow();
				Widget.OpenWindow("MAINMENU_BG");
			};

			widget.GetWidget<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Connecting to {0}:{1}...".F(host, port);
		}
    }

	public class ConnectionFailedLogic
	{
		[ObjectCreator.UseCtor]
		public ConnectionFailedLogic(
			[ObjectCreator.Param] Widget widget,
		    [ObjectCreator.Param] OrderManager orderManager)
		{
			widget.GetWidget<ButtonWidget>("CONNECTION_BUTTON_CANCEL").OnMouseUp = mi => {
				widget.GetWidget("CONNECTION_BUTTON_CANCEL").Parent.Visible = false;
				Game.Disconnect();
				Game.LoadShellMap();
				Widget.CloseWindow();
				Widget.OpenWindow("MAINMENU_BG");
			};
			widget.GetWidget<ButtonWidget>("CONNECTION_BUTTON_RETRY").OnMouseUp = mi =>
				Game.JoinServer(orderManager.Host, orderManager.Port);

			widget.GetWidget<LabelWidget>("CONNECTION_FAILED_DESC").GetText = () => string.IsNullOrEmpty(orderManager.ServerError) ? 
				"Could not connect to {0}:{1}".F(orderManager.Host, orderManager.Port) : orderManager.ServerError;
		}
	}
}
