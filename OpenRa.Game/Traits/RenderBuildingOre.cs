﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class RenderBuildingOre : RenderBuilding
	{
		public RenderBuildingOre(Actor self)
			: base(self)
		{
			anim.PlayThen("make", () => anim.PlayFetchIndex("idle", 
				() => (int)(5 * self.Owner.GetSiloFullness())));
		}
	}
}
