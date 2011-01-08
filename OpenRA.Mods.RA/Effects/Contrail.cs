﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ContrailInfo : ITraitInfo
	{
		public readonly int[] ContrailOffset = {0, 0};

		public readonly int TrailLength = 20;
		public readonly bool UsePlayerColor = true;

		public object Create(ActorInitializer init) { return new Contrail(init.self, this); }
	}

	class Contrail : ITick, IPostRender
	{
		private ContrailInfo Info = null;

		private List<float2> positions = new List<float2>();

		private Turret ContrailTurret = null;

		private int TrailLength = 0;
		private Color TrailColor = Color.White;

		public Contrail(Actor self, ContrailInfo info)
		{
			Info = info;

			ContrailTurret = new Turret(Info.ContrailOffset);

			TrailLength = Info.TrailLength;

			if (Info.UsePlayerColor)
			{
				var ownerColor = Color.FromArgb(255, self.Owner.ColorRamp.GetColor(0));
				TrailColor = PlayerColorRemap.ColorLerp(0.5f, ownerColor, Color.White);
			}
		}

		public void Tick(Actor self)
		{
			var facing = self.Trait<IFacing>();
			var altitude = new float2(0, self.Trait<IMove>().Altitude);

			float2 pos = self.CenterLocation - Combat.GetTurretPosition(self, facing, ContrailTurret) - altitude;
		
			positions.Add(pos);

			if (positions.Count >= TrailLength)
			{
				positions.RemoveAt(0);
			}
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			Color trailStart = TrailColor;
			Color trailEnd = Color.FromArgb(trailStart.A - 255 / TrailLength, trailStart.R,
											trailStart.G, trailStart.B);

			for (int i = positions.Count - 1; i >= 1; --i)
			{
				var conPos = positions[i];
				var nextPos = positions[i - 1];

				if (self.World.LocalShroud.IsVisible(OpenRA.Traits.Util.CellContaining(conPos)) ||
					self.World.LocalShroud.IsVisible(OpenRA.Traits.Util.CellContaining(nextPos)))
				{
					Game.Renderer.LineRenderer.DrawLine(conPos, nextPos, trailStart, trailEnd);

					trailStart = trailEnd;
					trailEnd = Color.FromArgb(trailStart.A - 255 / positions.Count, trailStart.R,
												trailStart.G, trailStart.B);
				}
			}
		}
	}
}
