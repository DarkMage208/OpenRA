﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class McvDeploy : IOrder
	{
		public McvDeploy(Actor self) { }

		public Order Order(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (lmb) return null;

			if (xy == self.Location)
				return OpenRa.Game.Order.DeployMcv(self, !Game.CanPlaceBuilding("fact", xy - new int2(1,1), self, false));

			return null;
		}
	}
}
