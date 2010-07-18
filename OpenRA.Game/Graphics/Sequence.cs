#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Xml;

namespace OpenRA.Graphics
{
	public class Sequence
	{
		readonly Sprite[] sprites;
		readonly int start, length, facings, tick;

		public readonly string Name;
		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }
		public int Facings { get { return facings; } }
		public int Tick { get { return tick; } }

		public Sequence(string unit, XmlElement e)
		{
			string srcOverride = e.GetAttribute("src");
			Name = e.GetAttribute("name");

			sprites = SpriteSheetBuilder.LoadAllSprites(string.IsNullOrEmpty(srcOverride) ? unit : srcOverride );
			start = int.Parse(e.GetAttribute("start"));

			if (e.GetAttribute("length") == "*" || e.GetAttribute("end") == "*")
				length = sprites.Length - Start;
			else if (e.HasAttribute("length"))
				length = int.Parse(e.GetAttribute("length"));
			else if (e.HasAttribute("end"))
				length = int.Parse(e.GetAttribute("end")) - int.Parse(e.GetAttribute("start"));
			else
				length = 1;

			if( e.HasAttribute( "facings" ) )
				facings = int.Parse( e.GetAttribute( "facings" ) );
			else
				facings = 1;

			if (e.HasAttribute("tick"))
				tick = int.Parse(e.GetAttribute("tick"));
			else
				tick = 40;
		}

		public Sprite GetSprite( int frame )
		{
			return GetSprite( frame, 0 );
		}

		public Sprite GetSprite(int frame, int facing)
		{
			var f = Traits.Util.QuantizeFacing( facing, facings );
			return sprites[ (f * length) + ( frame % length ) + start ];
		}
	}
}
