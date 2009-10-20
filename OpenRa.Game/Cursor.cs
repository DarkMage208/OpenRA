﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using System.IO;

namespace OpenRa.Game
{
	class Cursor
	{
		CursorSequence sequence;
		Cursor(string cursor)
		{
			sequence = SequenceProvider.GetCursorSequence(cursor);
		}

		public Sprite GetSprite(int frame) { return sequence.GetSprite(frame); }
		public int2 GetHotspot() { return sequence.Hotspot; }

		public static Cursor Default { get { return new Cursor("default"); } }
		public static Cursor Move { get { return new Cursor("move"); } }
		public static Cursor Select { get { return new Cursor("select"); } }
	}
}
