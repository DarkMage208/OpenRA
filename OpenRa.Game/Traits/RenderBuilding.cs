﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;
using OpenRa.FileFormats;
using OpenRa.Game;

namespace OpenRa.Game.Traits
{
	class RenderBuilding : RenderSimple
	{
		const int SmallBibStart = 1;
		const int LargeBibStart = 5;

		public RenderBuilding(Actor self)
			: base(self)
		{
			anim.PlayThen("make", () => anim.PlayRepeating("idle"));

			// at this point, we already know where we are, so we can safely place the bib in the smudge
			if (((UnitInfo.BuildingInfo)self.unitInfo).Bib)
			{
				var fp = Rules.Footprint.GetFootprint(self.unitInfo.Name);
				var bibOffset = fp.Length - 2;
				var size = fp.First().Length;
				var startIndex = (size == 2) ? SmallBibStart : LargeBibStart;

				
				for (int i = 0; i < 2*size; i++)
					Game.map.MapTiles[
						self.Location.X + i % size + Game.map.Offset.X,
						self.Location.Y + i / size + Game.map.Offset.Y + bibOffset].smudge = (byte)(i + startIndex);
			}
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Pair.New(anim.Image, 24f * (float2)self.Location);
		}
	}
}
