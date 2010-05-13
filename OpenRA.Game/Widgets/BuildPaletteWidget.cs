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

using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Graphics;
using OpenRA.FileFormats;
using OpenRA.Orders;
using System;

namespace OpenRA.Widgets
{
	class BuildPaletteWidget : Widget
	{
		public int Columns = 3;
		public int Rows = 5;
		
		string currentTab = "Building";
		bool paletteOpen = false;
		Dictionary<string, string[]> tabImageNames;
		Dictionary<string, Sprite> tabSprites;
		static float2 paletteOpenOrigin = new float2(Game.viewport.Width - 215, 280);
		static float2 paletteClosedOrigin = new float2(Game.viewport.Width - 16, 280);
		static float2 paletteOrigin = paletteClosedOrigin;
		const int paletteAnimationLength = 7;
		int paletteAnimationFrame = 0;
		bool paletteAnimating = false;
		List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle,Action<MouseInput>>>();
		Animation cantBuild;
		Animation ready;
		Animation clock;
		List<string> visibleTabs = new List<string>();
		
		public BuildPaletteWidget() : base() { }

		public BuildPaletteWidget(Widget other)
			: base(other)
		{
			throw new NotImplementedException("Why are you Cloning BuildPalette?");
		}

		public override Widget Clone() { return new BuildPaletteWidget(this); }
		
		public override void Initialize()
		{
			base.Initialize();
			
			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
			
			tabSprites = Rules.Info.Values
				.Where(u => u.Traits.Contains<BuildableInfo>())
				.ToDictionary(
					u => u.Name,
					u => SpriteSheetBuilder.LoadAllSprites(u.Traits.Get<BuildableInfo>().Icon ?? (u.Name + "icon"))[0]);

			var groups = Rules.Categories();
			
			tabImageNames = groups.Select(
				(g, i) => Pair.New(g,
					OpenRA.Graphics.Util.MakeArray(3,
						n => i.ToString())))
				.ToDictionary(a => a.First, a => a.Second);

		}
		
		public override void Tick(World world)
		{
			visibleTabs.Clear();
			foreach (var q in tabImageNames)
				if (!Rules.TechTree.BuildableItems(world.LocalPlayer, q.Key).Any())
				{
					if (currentTab == q.Key)
						currentTab = null;
				}
				else
					visibleTabs.Add(q.Key);

			if (currentTab == null)
				currentTab = visibleTabs.FirstOrDefault();
			
			TickPaletteAnimation(world);
			
			base.Tick(world);
		}
		
		void TickPaletteAnimation(World world)
		{		
			if (!paletteAnimating)
				return;

			// Increment frame
			if (paletteOpen)
				paletteAnimationFrame++;
			else
				paletteAnimationFrame--;
			
			// Calculate palette position
			if (paletteAnimationFrame <= paletteAnimationLength)
				paletteOrigin = float2.Lerp(paletteClosedOrigin, paletteOpenOrigin, paletteAnimationFrame * 1.0f / paletteAnimationLength);

			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			
			// Play palette-open sound at the start of the activate anim (open)
			if (paletteAnimationFrame == 1 && paletteOpen)
				Sound.Play(eva.BuildPaletteOpen);

			// Play palette-close sound at the start of the activate anim (close)
			if (paletteAnimationFrame == paletteAnimationLength + -1 && !paletteOpen)
				Sound.Play(eva.BuildPaletteClose);

			// Animation is complete
			if ((paletteAnimationFrame == 0 && !paletteOpen)
					|| (paletteAnimationFrame == paletteAnimationLength && paletteOpen))
			{
				paletteAnimating = false;
			}
		}
		
		public void SetCurrentTab(string produces)
		{
			if (!paletteOpen)
				paletteAnimating = true;
			paletteOpen = true;
			currentTab = produces;
		}
		
		public override bool HandleInput(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!IsVisible() || !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
				return base.HandleInput(mi);
			
			if (base.HandleInput(mi))
				return true;
			
			if (mi.Event == MouseInputEvent.Down)
			{
				var action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
					.Select(a => a.Second).FirstOrDefault();
				if (action == null)
					return false;
		
				action(mi);
				return true;
			}
			
			return false;
		}	
		
		public override void Draw (World world)
		{	
			int paletteHeight = DrawPalette(world, currentTab);
			DrawBuildTabs(world, paletteHeight);
		}

		int DrawPalette(World world, string queueName)
		{
			string paletteCollection = "palette-" + world.LocalPlayer.Country.Race;

			if (!Visible)
			{
				base.Draw(world);
				return 0;
			}
			buttons.Clear();


			float2 origin = new float2(paletteOrigin.X + 9, paletteOrigin.Y + 9);

			if (queueName == null) return 0;

			// Collect info

			var x = 0;
			var y = 0;
			var buildableItems = Rules.TechTree.BuildableItems(world.LocalPlayer, queueName).ToArray();
			var allBuildables = Rules.TechTree.AllBuildables(queueName)
				.Where(a => a.Traits.Get<BuildableInfo>().Owner.Contains(world.LocalPlayer.Country.Race))
				.OrderBy(a => a.Traits.Get<BuildableInfo>().BuildPaletteOrder)
				.ToArray();

			var queue = world.LocalPlayer.PlayerActor.traits.Get<ProductionQueue>();

			var overlayBits = new List<Pair<Sprite, float2>>();
			var numActualRows = Math.Max((allBuildables.Length + Columns - 1) / Columns, Rows);

			// Palette Background
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Game.chrome.renderer, paletteCollection, "top"), new float2(origin.X - 9, origin.Y - 9));
			for (var w = 0; w < numActualRows; w++)
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage(Game.chrome.renderer, paletteCollection,
					"bg-" + (w % 4).ToString()),
					new float2(origin.X - 9, origin.Y + 48 * w));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Game.chrome.renderer, paletteCollection, "bottom"),
				new float2(origin.X - 9, origin.Y - 1 + 48 * numActualRows));
			Game.chrome.renderer.RgbaSpriteRenderer.Flush();


			// Icons
			string tooltipItem = null;
			float2 tooltipPos = float2.Zero;
			foreach (var item in allBuildables)
			{
				var rect = new RectangleF(origin.X + x * 64, origin.Y + 48 * y, 64, 48);
				var drawPos = new float2(rect.Location);
				var isBuildingSomething = queue.CurrentItem(queueName) != null;
				WidgetUtils.DrawSHP(tabSprites[item.Name], drawPos);

				var firstOfThis = queue.AllItems(queueName).FirstOrDefault(a => a.Item == item.Name);

				if (rect.Contains(Game.chrome.lastMousePos.ToPoint()))
				{
					tooltipItem = item.Name;
					tooltipPos = drawPos;
				}

				var overlayPos = drawPos + new float2((64 - ready.Image.size.X) / 2, 2);

				if (firstOfThis != null)
				{
					clock.PlayFetchIndex("idle",
						() => (firstOfThis.TotalTime - firstOfThis.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / firstOfThis.TotalTime);
					clock.Tick();
					WidgetUtils.DrawSHP(clock.Image, drawPos);

					if (firstOfThis.Done)
					{
						ready.Play("ready");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}
					else if (firstOfThis.Paused)
					{
						ready.Play("hold");
						overlayBits.Add(Pair.New(ready.Image, overlayPos));
					}

					var repeats = queue.AllItems(queueName).Count(a => a.Item == item.Name);
					if (repeats > 1 || queue.CurrentItem(queueName) != firstOfThis)
					{
						var offset = -22;
						var digits = repeats.ToString();
						foreach (var d in digits)
						{
							ready.PlayFetchIndex("groups", () => d - '0');
							ready.Tick();
							overlayBits.Add(Pair.New(ready.Image, overlayPos + new float2(offset, 0)));
							offset += 6;
						}
					}
				}
				else
					if (!buildableItems.Contains(item.Name) || isBuildingSomething)
						overlayBits.Add(Pair.New(cantBuild.Image, drawPos));

				var closureName = buildableItems.Contains(item.Name) ? item.Name : null;
				buttons.Add(Pair.New(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height), HandleClick(closureName, world)));

				if (++x == Columns) { x = 0; y++; }
			}
			if (x != 0) y++;

			foreach (var ob in overlayBits)
				WidgetUtils.DrawSHP(ob.First, ob.Second);

			Game.chrome.renderer.WorldSpriteRenderer.Flush();

			// Tooltip
			if (tooltipItem != null && !paletteAnimating && paletteOpen)
				DrawProductionTooltip(world, tooltipItem, 
					new float2(Game.viewport.Width, origin.Y + numActualRows * 48 + 9).ToInt2());

			// Palette Dock
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Game.chrome.renderer, paletteCollection, "dock-top"),
				new float2(Game.viewport.Width - 14, origin.Y - 23));

			for (int i = 0; i < numActualRows; i++)
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Game.chrome.renderer, paletteCollection, "dock-" + (i % 4).ToString()),
					new float2(Game.viewport.Width - 14, origin.Y + 48 * i));

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Game.chrome.renderer, paletteCollection, "dock-bottom"),
				new float2(Game.viewport.Width - 14, origin.Y - 1 + 48 * numActualRows));
			Game.chrome.renderer.RgbaSpriteRenderer.Flush();

			return 48 * y + 9;
		}
		
		Action<MouseInput> HandleClick(string name, World world)
		{
			return mi => {
				var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
				Sound.Play(eva.TabClick);
				
				if (name != null)
					HandleBuildPalette(world, name, (mi.Button == MouseButton.Left));
			};
		}

        static void Hotkey(World world, String name)
		{
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			Sound.Play(eva.TabClick);
			
			if (name != null)
			    HandleBuildPalette(world, name, true);
		}
		
		Action<MouseInput> HandleTabClick(string button, World world)
		{
			return mi => {
				if (mi.Button != MouseButton.Left)
					return;
				
				var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
				Sound.Play(eva.TabClick);
				var wasOpen = paletteOpen;
				paletteOpen = (currentTab == button && wasOpen) ? false : true;
				currentTab = button;
				if (wasOpen != paletteOpen)
					paletteAnimating = true;
			};
		}
		
		static string Description( string a )
		{
			if( a[ 0 ] == '@' )
				return "any " + a.Substring( 1 );
			else
				return Rules.Info[ a.ToLowerInvariant() ].Traits.Get<BuildableInfo>().Description;
		}
		
		static void HandleBuildPalette( World world, string item, bool isLmb )
		{
			var player = world.LocalPlayer;
			var unit = Rules.Info[item];
			var queue = player.PlayerActor.traits.Get<Traits.ProductionQueue>();
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var producing = queue.AllItems(unit.Category).FirstOrDefault( a => a.Item == item );

			if (isLmb)
			{
				if (producing != null && producing == queue.CurrentItem(unit.Category))
				{
					if (producing.Done)
					{
						if (unit.Traits.Contains<BuildingInfo>())
							Game.controller.orderGenerator = new PlaceBuildingOrderGenerator(player.PlayerActor, item);
						return;
					}

					if (producing.Paused)
					{
						Game.IssueOrder(Order.PauseProduction(player, item, false));
						return;
					}
				}

				StartProduction(world, item);
			}
			else
			{
				if (producing != null)
				{
					// instant cancel of things we havent really started yet, and things that are finished
					if (producing.Paused || producing.Done || producing.TotalCost == producing.RemainingCost)
					{
						Sound.Play(eva.CancelledAudio);
						Game.IssueOrder(Order.CancelProduction(player, item));
					}
					else
					{
						Sound.Play(eva.OnHoldAudio);
						Game.IssueOrder(Order.PauseProduction(player, item, true));
					}
				}
			}
		}
		
		static void StartProduction( World world, string item )
		{
			var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			var unit = Rules.Info[item];

			Sound.Play(unit.Traits.Contains<BuildingInfo>() ? eva.BuildingSelectAudio : eva.UnitSelectAudio);
			Game.IssueOrder(Order.StartProduction(world.LocalPlayer, item, 
				Game.controller.GetModifiers().HasModifier(Modifiers.Shift) ? 5 : 1));
		}

		static Dictionary<string, string> CategoryNameRemaps = new Dictionary<string, string>
		{
			{ "Building", "Structures" },
			{ "Defense", "Defenses" },
			{ "Plane", "Aircraft" },
			{ "Ship", "Ships" },
			{ "Vehicle", "Vehicles" },
		};
		
		void DrawBuildTabs( World world, int paletteHeight)
		{
			const int tabWidth = 24;
			const int tabHeight = 40;
			var x = paletteOrigin.X - tabWidth;
			var y = paletteOrigin.Y + 9;
			
			var queue = world.LocalPlayer.PlayerActor.traits.Get<Traits.ProductionQueue>();
			
			foreach (var q in tabImageNames)
			{
				var groupName = q.Key;
				if (!visibleTabs.Contains(groupName))
					continue;
				
				string[] tabKeys = { "normal", "ready", "selected" };
				var producing = queue.CurrentItem(groupName);
				var index = q.Key == currentTab ? 2 : (producing != null && producing.Done) ? 1 : 0;
				var race = world.LocalPlayer.Country.Race;
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Game.chrome.renderer,"tabs-"+tabKeys[index], race+"-"+q.Key), new float2(x, y));
				
				var rect = new Rectangle((int)x,(int)y,(int)tabWidth,(int)tabHeight);
				buttons.Add(Pair.New(rect, HandleTabClick(groupName, world)));

				if (rect.Contains(Game.chrome.lastMousePos.ToPoint()))
				{
					var text = CategoryNameRemaps.ContainsKey(groupName) ? CategoryNameRemaps[groupName] : groupName;
					var sz = Game.chrome.renderer.BoldFont.Measure(text);
					WidgetUtils.DrawPanelPartial("dialog4",
						Rectangle.FromLTRB((int)rect.Left - sz.X - 30, (int)rect.Top, (int)rect.Left - 5, (int)rect.Bottom),
						PanelSides.All);

					Game.chrome.renderer.BoldFont.DrawText(text, 
						new float2(rect.Left - sz.X - 20, rect.Top + 12), Color.White);
				}

				y += tabHeight;
			}
			
			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}

		void DrawRightAligned(string text, int2 pos, Color c)
		{
			Game.chrome.renderer.BoldFont.DrawText(text, 
				pos - new int2(Game.chrome.renderer.BoldFont.Measure(text).X, 0), c);
		}

		void DrawProductionTooltip(World world, string unit, int2 pos)
		{
			pos.Y += 15;
			var chromeCollection = "chrome-" + world.LocalPlayer.Country.Race;

			var p = pos.ToFloat2() - new float2(297, -3);

			var info = Rules.Info[unit];
			var buildable = info.Traits.Get<BuildableInfo>();

			var buildings = Rules.TechTree.GatherBuildings( world.LocalPlayer );
			var canBuildThis = Rules.TechTree.CanBuild(info, world.LocalPlayer, buildings);

			var longDescSize = Game.chrome.renderer.RegularFont.Measure(buildable.LongDesc.Replace("\\n", "\n")).Y;
			if (!canBuildThis) longDescSize += 8;

			WidgetUtils.DrawPanel("dialog4", new Rectangle(Game.viewport.Width - 300, pos.Y, 300, longDescSize + 50));

			Game.chrome.renderer.BoldFont.DrawText(buildable.Description, p.ToInt2() + new int2(5, 5), Color.White);

			DrawRightAligned("${0}".F(buildable.Cost), pos + new int2(-5, 5), Color.White);

            if (buildable.Hotkey != null)
                DrawRightAligned("{0}".F(buildable.Hotkey.ToUpper()), pos + new int2(-5, 35),Color.White);

			var bi = info.Traits.GetOrDefault<BuildingInfo>();
			if (bi != null)
				DrawRightAligned("{1}{0}".F(bi.Power, bi.Power > 0 ? "+" : ""), pos + new int2(-5, 20), Color.White);

			
			p += new int2(5, 20);
			if (!canBuildThis)
			{
				var prereqs = buildable.Prerequisites
					.Select( a => Description( a ) );
				Game.chrome.renderer.RegularFont.DrawText(
					"Requires {0}".F(string.Join(", ", prereqs.ToArray())), 
					p.ToInt2(),
					Color.White);

				p += new int2(0, 8);
			}

			p += new int2(0, 15);
			Game.chrome.renderer.RegularFont.DrawText(buildable.LongDesc.Replace("\\n", "\n"), 
				p.ToInt2(), Color.White);

			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}

        public static void DoBuildingHotkey(char c, World world)
        {
            if (Game.world.LocalPlayer == null) return;

            var buildable = Rules.TechTree.BuildableItems(Game.world.LocalPlayer, Chrome.rootWidget.GetWidget<BuildPaletteWidget>("INGAME_BUILD_PALETTE").currentTab);

            var toBuild = buildable.FirstOrDefault(b => Rules.Info[b.ToLowerInvariant()].Traits.Get<BuildableInfo>().Hotkey == c.ToString());

            if (toBuild != null) Hotkey(world, toBuild);

        }
        public static void TabChange(bool shift)
        {
            var p = Chrome.rootWidget.GetWidget<BuildPaletteWidget>("INGAME_BUILD_PALETTE");
            int size = p.visibleTabs.Count();
            if (size > 0)
            {
                string last = p.visibleTabs.Last();
                string first = p.visibleTabs.First();
                int current = p.visibleTabs.IndexOf(p.currentTab);
                if (!shift)
                {
                    if (current + 1 >= size)
                        p.SetCurrentTab(p.visibleTabs.FirstOrDefault());
                    else
                        p.SetCurrentTab(p.visibleTabs[current + 1]);
                }
                else
                {
                    if (current - 1 < 0)
                        p.SetCurrentTab(p.visibleTabs.LastOrDefault());
                    else
                        p.SetCurrentTab(p.visibleTabs[current - 1]);
                }
            }
        }
	}
}