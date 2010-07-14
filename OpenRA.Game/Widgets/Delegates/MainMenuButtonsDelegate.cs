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

using OpenRA.FileFormats;

namespace OpenRA.Widgets.Delegates
{
	public class MainMenuButtonsDelegate : IWidgetDelegate
	{
		public MainMenuButtonsDelegate()
		{
			// Main menu is the default window
			Widget.WindowList.Push("MAINMENU_BG");
			Chrome.rootWidget.GetWidget("MAINMENU_BUTTON_QUIT").OnMouseUp = mi => { Game.Exit(); return true; };

			var version = Chrome.rootWidget.GetWidget("MAINMENU_BG").GetWidget<LabelWidget>("VERSION_STRING");

			if (FileSystem.Exists("VERSION"))
			{
				var s = FileSystem.Open("VERSION");
				version.Text = s.ReadAllText();
				s.Close();
			}
		}
	}
}
