﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Infantry : ICrushable
	{
		readonly Actor self;
		public Infantry(Actor self)
		{
			this.self = self;
		}

		public bool IsCrushableByFriend()
		{
			// HACK: should be false
			return true;
		}
		public bool IsCrushableByEnemy()
		{
			// HACK: should be based off crushable tag
			return true;
		}

		public void OnCrush(Actor crusher)
		{
			self.InflictDamage(crusher, self.Health, Rules.WarheadInfo["Crush"]);
		}

		public IEnumerable<UnitMovementType> CrushableBy()
		{
			yield return UnitMovementType.Track;
			//yield return UnitMovementType.Wheel; // Can infantry be crushed by wheel?
		}
	}
}
