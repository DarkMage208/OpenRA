﻿using System.Collections.Generic;
using System.Drawing;

namespace OpenRa.Game
{
	class Chat
	{
		const int logLength = 10;

		public List<Tuple<Color, string, string>> recentLines = new List<Tuple<Color, string, string>>();
		public string typing = "";
		public bool isChatting = false;

		public void Toggle()
		{
			if (isChatting && typing.Length > 0)
			{
				Game.controller.AddOrder(Order.Chat(Game.LocalPlayer, typing));
				AddLine(Game.LocalPlayer, typing);
			}

			typing = "";
			isChatting ^= true;
		}

		public void TypeChar(char c)
		{
			if (c == '\b')
			{
				if (typing.Length > 0)
					typing = typing.Remove(typing.Length - 1);
			}
			else
				typing += c;
		}

		static readonly Color[] paletteColors =
		{
			Color.FromArgb(228, 200, 112),
			Color.FromArgb(56, 72, 125),
			Color.FromArgb(238, 0, 0),
			Color.FromArgb(198,97,0),
			Color.FromArgb(28,109,97),
			Color.FromArgb(153,76,53),
			Color.FromArgb(76,101,60),
			Color.FromArgb(133,113,101),
		};

		public void AddLine(Player p, string text)
		{
			AddLine(paletteColors[p.Palette], p.PlayerName, text);
		}

		public void AddLine(Color c, string from, string text)
		{
			recentLines.Add(Tuple.New(c, from, text));
			Game.PlaySound("rabeep1.aud", false);
			while (recentLines.Count > logLength) recentLines.RemoveAt(0);
		}
	}
}
