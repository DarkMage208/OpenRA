﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class WithShadow : IRenderModifier
	{
		public WithShadow(Actor self) {}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			var unit = self.traits.Get<Unit>();

			var shadowSprites = r.Select(a => a.WithPalette(8));
			var flyingSprites = (unit.Altitude <= 0) ? r 
				: r.Select(a => a.WithPos(a.Pos - new float2(0, unit.Altitude)).WithZOffset(3));

			return shadowSprites.Concat(flyingSprites);
		}
	}
}
