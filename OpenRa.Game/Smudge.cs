﻿using OpenRa.GameRules;
using OpenRa.FileFormats;

namespace OpenRa
{
	static class Smudge
	{
		const int firstScorch = 19;
		const int firstCrater = 25;
		const int framesPerCrater = 5;

		public static void AddSmudge(this Map map, bool isCrater, int x, int y)
		{
			var smudge = map.MapTiles[x, y].smudge;
			if (smudge == 0)
				map.MapTiles[x, y].smudge = (byte) (isCrater
					? (firstCrater + framesPerCrater * ChooseSmudge())
					: (firstScorch + ChooseSmudge()));

			if (smudge < firstCrater || !isCrater) return; /* bib or scorch; don't change */
			
			/* deepen the crater */
			var amount = (smudge - firstCrater) % framesPerCrater;
			if (amount < framesPerCrater - 1)
				map.MapTiles[x, y].smudge++;
		}

		public static void AddSmudge(this Map map, int2 targetTile, WarheadInfo warhead)
		{
			switch (warhead.Explosion)		/* todo: push the scorch/crater behavior into data */
			{
				case 4:
				case 5:
					map.AddSmudge(true, targetTile.X, targetTile.Y);
					break;

				case 3:
				case 6:
					map.AddSmudge(false, targetTile.X, targetTile.Y);
					break;
			}
		}

		static int lastSmudge = 0;
		static int ChooseSmudge() { lastSmudge = (lastSmudge + 1) % 6; return lastSmudge; }
	}
}
