#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DiplomacyLogic
	{
		static List<Widget> controls = new List<Widget>();

		int validPlayers = 0;
		readonly World world;
		
		[ObjectCreator.UseCtor]
		public DiplomacyLogic( [ObjectCreator.Param] World world )
		{
			this.world = world;
			var root = Widget.RootWidget.GetWidget("INGAME_ROOT");
			var diplomacyBG = root.GetWidget("DIPLOMACY_BG");
			var diplomacy = root.GetWidget("INGAME_DIPLOMACY_BUTTON");
			
			diplomacy.OnMouseUp = mi =>
			{
				diplomacyBG.Visible = !diplomacyBG.Visible;
				if (diplomacyBG.IsVisible())
					LayoutDialog(diplomacyBG);
				return true;
			};
			
			Game.AfterGameStart += _ => validPlayers = world.players.Values.Where(a => a != world.LocalPlayer && !a.NonCombatant).Count();
			diplomacy.IsVisible = () => (validPlayers > 0);
		}
		
		// This is shit
		void LayoutDialog(Widget bg)
		{
			foreach (var c in controls)
				bg.RemoveChild(c);
			controls.Clear();

			var y = 50;
			var margin = 20;
			var labelWidth = (bg.Bounds.Width - 3 * margin) / 3;

			var ts = new LabelWidget
			{
				Font = "Bold",
				Bounds = new Rectangle(margin + labelWidth + 10, y, labelWidth, 25),
				Text = "Their Stance",
				Align = LabelWidget.TextAlign.Left,
			};

			bg.AddChild(ts);
			controls.Add(ts);

			var ms = new LabelWidget
			{
				Font = "Bold",
				Bounds = new Rectangle(margin + 2 * labelWidth + 20, y, labelWidth, 25),
				Text = "My Stance",
				Align = LabelWidget.TextAlign.Left,
			};

			bg.AddChild(ms);
			controls.Add(ms);

			y += 35;

			foreach (var p in world.players.Values.Where(a => a != world.LocalPlayer && !a.NonCombatant))
			{
				var pp = p;
				var label = new LabelWidget
				{
					Bounds = new Rectangle(margin, y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_{0}".F(p.Index),
					Text = p.PlayerName,
					Align = LabelWidget.TextAlign.Left,
					Font = "Bold",
				};

				bg.AddChild(label);
				controls.Add(label);

				var theirStance = new LabelWidget
				{
					Bounds = new Rectangle( margin + labelWidth + 10, y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_THEIR_{0}".F(p.Index),
					Text = p.PlayerName,
					Align = LabelWidget.TextAlign.Left,

					GetText = () => pp.Stances[ world.LocalPlayer ].ToString(),
				};

				bg.AddChild(theirStance);
				controls.Add(theirStance);

				var myStance = new DropDownButtonWidget
				{
					Bounds = new Rectangle( margin + 2 * labelWidth + 20,  y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_MY_{0}".F(p.Index),
					GetText = () => world.LocalPlayer.Stances[ pp ].ToString(),
				};

				myStance.OnMouseDown = mi => { ShowDropDown(pp, myStance); return true; };

				bg.AddChild(myStance);
				controls.Add(myStance);
				
				y += 35;
			}
		}

		void ShowDropDown(Player p, DropDownButtonWidget dropdown)
		{
			var stances = Enum.GetValues(typeof(Stance)).OfType<Stance>().ToList();
			Func<Stance, ScrollItemWidget, ScrollItemWidget> setupItem = (s, template) =>
			{
				var item = ScrollItemWidget.Setup(template,
				                                  () => s == world.LocalPlayer.Stances[ p ],
				                                  () => SetStance(dropdown, p, s));
				
				item.GetWidget<LabelWidget>("LABEL").GetText = () => s.ToString();
				return item;
			};
			
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, stances, setupItem);
		}

		void SetStance(ButtonWidget bw, Player p, Stance ss)
		{
			if (p.World.LobbyInfo.GlobalSettings.LockTeams)
				return;	// team changes are banned

			world.IssueOrder(new Order("SetStance", world.LocalPlayer.PlayerActor,
				false) { TargetLocation = new int2(p.Index, (int)ss) });

			bw.Text = ss.ToString();
		}
	}
}
