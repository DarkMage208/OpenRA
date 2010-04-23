﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class RadarBinWidget : Widget
	{
		// Radar
		static float2 radarOpenOrigin = new float2(Game.viewport.Width - 215, 29);
		static float2 radarClosedOrigin = new float2(Game.viewport.Width - 215, -166);
		static float2 radarOrigin = radarClosedOrigin;
		float radarMinimapHeight;
		const int radarSlideAnimationLength = 15;
		const int radarActivateAnimationLength = 5;
		int radarAnimationFrame = 0;
		bool radarAnimating = false;
		bool hasRadar = false;

		// Power bar 
		static float2 powerOrigin = new float2(42, 205); // Relative to radarOrigin
		static Size powerSize = new Size(138, 5);
		float? lastPowerProvidedPos;
		float? lastPowerDrainedPos;

		string radarCollection;

		public override void Draw(World world)
		{
			DrawRadar(world);
			DrawPower(world);
		}

		public override void Tick(World world)
		{
			if (!radarAnimating)
				return;

			// Increment frame
			if (hasRadar)
				radarAnimationFrame++;
			else
				radarAnimationFrame--;

			// Calculate radar bin position
			if (radarAnimationFrame <= radarSlideAnimationLength)
				radarOrigin = float2.Lerp(radarClosedOrigin, radarOpenOrigin, radarAnimationFrame * 1.0f / radarSlideAnimationLength);

			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();

			// Play radar-on sound at the start of the activate anim (open)
			if (radarAnimationFrame == radarSlideAnimationLength && hasRadar)
				Sound.Play(eva.RadarUp);

			// Play radar-on sound at the start of the activate anim (close)
			if (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength - 1 && !hasRadar)
				Sound.Play(eva.RadarDown);

			// Minimap height
			if (radarAnimationFrame >= radarSlideAnimationLength)
				radarMinimapHeight = float2.Lerp(0, 192, (radarAnimationFrame - radarSlideAnimationLength) * 1.0f / radarActivateAnimationLength);

			// Animation is complete
			if ((radarAnimationFrame == 0 && !hasRadar)
					|| (radarAnimationFrame == radarSlideAnimationLength + radarActivateAnimationLength && hasRadar))
			{
				radarAnimating = false;
			}
		}

		void DrawRadar(World world)
		{
			radarCollection = "radar-" + world.LocalPlayer.Country.Race;

			var hasNewRadar = world.Queries.OwnedBy[world.LocalPlayer]
				.WithTrait<ProvidesRadar>()
				.Any(a => a.Trait.IsActive);

			if (hasNewRadar != hasRadar)
			{
				radarAnimating = true;
			}

			hasRadar = hasNewRadar;

			var renderer = Game.chrome.renderer;
			var rgbaRenderer = renderer.RgbaSpriteRenderer;

			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "left"), radarOrigin, "chrome");
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "right"), radarOrigin + new float2(201, 0), "chrome");
			rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "bottom"), radarOrigin + new float2(0, 192), "chrome");

			if (radarAnimating)
				rgbaRenderer.DrawSprite(ChromeProvider.GetImage(renderer, radarCollection, "bg"), radarOrigin + new float2(9, 0), "chrome");

			rgbaRenderer.Flush();

			if (radarAnimationFrame >= radarSlideAnimationLength)
			{
				RectangleF mapRect = new RectangleF(radarOrigin.X + 9, radarOrigin.Y + (192 - radarMinimapHeight) / 2, 192, radarMinimapHeight);
				world.Minimap.Draw(mapRect, false);
			}
		}

		void DrawPower(World world)
		{
			// Nothing to draw
			if (world.LocalPlayer.PowerProvided == 0 && world.LocalPlayer.PowerDrained == 0)
				return;

			var renderer = Game.chrome.renderer;
			var lineRenderer = Game.chrome.lineRenderer;
			var rgbaRenderer = renderer.RgbaSpriteRenderer;

			// Draw bar horizontally
			var barStart = powerOrigin + radarOrigin;
			var barEnd = barStart + new float2(powerSize.Width, 0);

			float powerScaleBy = 100;
			var maxPower = Math.Max(world.LocalPlayer.PowerProvided, world.LocalPlayer.PowerDrained);
			while (maxPower >= powerScaleBy) powerScaleBy *= 2;

			// Current power supply
			var powerLevelTemp = barStart.X + (barEnd.X - barStart.X) * (world.LocalPlayer.PowerProvided / powerScaleBy);
			lastPowerProvidedPos = float2.Lerp(lastPowerProvidedPos.GetValueOrDefault(powerLevelTemp), powerLevelTemp, .3f);
			float2 powerLevel = new float2(lastPowerProvidedPos.Value, barStart.Y);

			var color = Color.LimeGreen;
			if (world.LocalPlayer.GetPowerState() == PowerState.Low)
				color = Color.Orange;
			if (world.LocalPlayer.GetPowerState() == PowerState.Critical)
				color = Color.Red;

			var colorDark = Graphics.Util.Lerp(0.25f, color, Color.Black);
			for (int i = 0; i < powerSize.Height; i++)
			{
				color = (i - 1 < powerSize.Height / 2) ? color : colorDark;
				float2 leftOffset = new float2(0, i);
				float2 rightOffset = new float2(0, i);
				// Indent corners
				if ((i == 0 || i == powerSize.Height - 1) && powerLevel.X - barStart.X > 1)
				{
					leftOffset.X += 1;
					rightOffset.X -= 1;
				}
				lineRenderer.DrawLine(Game.viewport.Location + barStart + leftOffset, Game.viewport.Location + powerLevel + rightOffset, color, color);
			}
			lineRenderer.Flush();

			// Power usage indicator
			var indicator = ChromeProvider.GetImage(renderer, radarCollection, "power-indicator");
			var powerDrainedTemp = barStart.X + (barEnd.X - barStart.X) * (world.LocalPlayer.PowerDrained / powerScaleBy);
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(powerDrainedTemp), powerDrainedTemp, .3f);
			float2 powerDrainLevel = new float2(lastPowerDrainedPos.Value - indicator.size.X / 2, barStart.Y - 1);

			rgbaRenderer.DrawSprite(indicator, powerDrainLevel, "chrome");
			rgbaRenderer.Flush();
		}
	}
}
