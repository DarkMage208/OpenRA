using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	class PostGameWidget : Widget
	{
		public PostGameWidget() : base() { }

		public PostGameWidget(Widget other)	: base(other) { }

		public override Widget Clone() { return new PostGameWidget(this); }

		bool AreMutualAllies(Player a, Player b) { return a.Stances[b] == Stance.Ally && b.Stances[a] == Stance.Ally; }
		
		public override void Draw(World world)
		{
			base.Draw(world);

			if (world.LocalPlayer == null) return;

			if (world.players.Count > 2)	/* more than just us + neutral */
			{
				var conds = world.Queries.WithTrait<IVictoryConditions>()
					.Where(c => c.Actor.Owner != world.NeutralPlayer);

				if (conds.Any(c => c.Actor.Owner == world.LocalPlayer && c.Trait.HasLost))
					DrawText("YOU ARE DEFEATED");
				else if (conds.All(c => AreMutualAllies(c.Actor.Owner, world.LocalPlayer) || c.Trait.HasLost))
					DrawText("YOU ARE VICTORIOUS");
			}
		}

		void DrawText(string s)
		{
			var size = Game.chrome.renderer.TitleFont.Measure(s);

			WidgetUtils.DrawPanel("dialog4", new Rectangle(
				(Game.viewport.Width - size.X - 40) / 2,
				(Game.viewport.Height - size.Y - 10) / 2,
				size.X + 40,
				size.Y + 13));

			Game.chrome.renderer.TitleFont.DrawText(s, 
				new float2((Game.viewport.Width - size.X) / 2,
					(Game.viewport.Height - size.Y) / 2 - .2f * size.Y), Color.White);

			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}
	}
}
