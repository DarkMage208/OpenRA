﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class ReplayBrowserDelegate : IWidgetDelegate
	{
		Widget widget; 

		[ObjectCreator.UseCtor]
		public ReplayBrowserDelegate( [ObjectCreator.Param] Widget widget )
		{
			this.widget = widget;

			widget.GetWidget("CANCEL_BUTTON").OnMouseUp = mi =>
				{
					Widget.CloseWindow();
					return true;
				};

			/* find some replays? */
			var rl = widget.GetWidget<ScrollPanelWidget>("REPLAY_LIST");
			var replayDir = Path.Combine(Platform.SupportDir, "Replays");

			var template = widget.GetWidget<LabelWidget>("REPLAY_TEMPLATE");
			CurrentReplay = null;

			rl.RemoveChildren();
			if (Directory.Exists(replayDir))
				foreach (var replayFile in Directory.GetFiles(replayDir, "*.rep").Reverse())
					AddReplay(rl, replayFile, template);

			widget.GetWidget("WATCH_BUTTON").OnMouseUp = mi =>
				{
					if (currentReplay != null)
					{
						Widget.CloseWindow();
						Game.JoinReplay(CurrentReplay);
					}
					return true;
				};

			widget.GetWidget("REPLAY_INFO").IsVisible = () => currentReplay != null;
		}

		string currentReplay = null;
		string CurrentReplay
		{
			get { return currentReplay; }
			set
			{
				currentReplay = value;
				if (currentReplay != null)
				{
					try
					{
						var summary = new ReplaySummary(currentReplay);
						var mapStub = summary.Map();

						widget.GetWidget<LabelWidget>("DURATION").GetText =
							() => WidgetUtils.FormatTime(summary.Duration * 3	/* todo: 3:1 ratio isnt always true. */);
						widget.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => mapStub;
						widget.GetWidget<LabelWidget>("MAP_TITLE").GetText =
							() => mapStub != null ? mapStub.Title : "(Unknown Map)";
					}
					catch(Exception e)
					{
						Log.Write("debug", "Exception while parsing replay: {0}", e.ToString());
						currentReplay = null;
					}
				}
			}
		}

		void AddReplay(ScrollPanelWidget list, string filename, LabelWidget template)
		{
			var entry = template.Clone() as LabelWidget;
			entry.Id = "REPLAY_";
			entry.GetText = () => "   {0}".F(Path.GetFileName(filename));
			entry.GetBackground = () => (CurrentReplay == filename) ? "dialog2" : null;
			entry.OnMouseDown = mi => { if (mi.Button != MouseButton.Left) return false; CurrentReplay = filename; return true; };
			entry.IsVisible = () => true;
			list.AddChild(entry);
		}
	}

	/* a maze of twisty little hacks,... */
	public class ReplaySummary
	{
		public readonly int Duration;
		public readonly Session LobbyInfo;

		public ReplaySummary(string filename)
		{
			var lastFrame = 0;
			var hasSeenGameStart = false;
			var lobbyInfo = null as Session;
			using (var conn = new ReplayConnection(filename))
				conn.Receive((client, packet) =>
					{
						var frame = BitConverter.ToInt32(packet, 0);
						if (packet.Length == 5 && packet[4] == 0xBF)
							return;	// disconnect
						else if (packet.Length >= 5 && packet[4] == 0x65)
							return;	// sync
						else if (frame == 0)
						{
							/* decode this to recover lobbyinfo, etc */
							var orders = packet.ToOrderList(null);
							foreach (var o in orders)
								if (o.OrderString == "StartGame")
									hasSeenGameStart = true;
								else if (o.OrderString == "SyncInfo" && !hasSeenGameStart)
									lobbyInfo = Session.Deserialize(o.TargetString);
						}
						else
							lastFrame = Math.Max(lastFrame, frame);
					});

			Duration = lastFrame;
			LobbyInfo = lobbyInfo;
		}

		public Map Map()
		{
			if (LobbyInfo == null)
				return null;

			var map = LobbyInfo.GlobalSettings.Map;
			if (!Game.modData.AvailableMaps.ContainsKey(map))
				return null;

			return Game.modData.AvailableMaps[map];
		}
	}
}
