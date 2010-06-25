﻿#region Copyright & License Information
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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	public class ParaDropInfo : TraitInfo<ParaDrop>
	{
		public readonly int LZRange = 4;
		public readonly string ChuteSound = "chute1.aud";
	}

	public class ParaDrop : ITick
	{
		readonly List<int2> droppedAt = new List<int2>();
		int2 lz;
		Actor flare;

		public void SetLZ( int2 lz, Actor flare )
		{
			this.lz = lz;
			this.flare = flare;
			droppedAt.Clear();
		}

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<ParaDropInfo>();
			var r = info.LZRange;

			if ((self.Location - lz).LengthSquared <= r * r && !droppedAt.Contains(self.Location))
			{
				if (!IsSuitableCell(self, self.Location))
					return;

				// unload a dude here
				droppedAt.Add(self.Location);

				var cargo = self.traits.Get<Cargo>();
				if (cargo.IsEmpty(self))
					FinishedDropping(self);
				else
				{
					var a = cargo.Unload(self);
					var rs = a.traits.Get<RenderSimple>();

					self.World.AddFrameEndTask(w => w.Add(
						new Parachute(self.Owner, rs.anim.Name,
							Util.CenterOfCell(Util.CellContaining(self.CenterLocation)),
							self.traits.Get<Unit>().Altitude, a)));

					Sound.Play(info.ChuteSound, self.CenterLocation);
				}
			}
		}

		bool IsSuitableCell(Actor self, int2 p)
		{
			return self.traits.WithInterface<IMove>().FirstOrDefault().CanEnterCell(p);
		}

		void FinishedDropping(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new FlyOffMap { Interruptible = false });
			self.QueueActivity(new RemoveSelf());

			if (flare != null)
			{
				flare.CancelActivity();
				flare.QueueActivity(new Wait(300));
				flare.QueueActivity(new RemoveSelf());
			}
		}
	}
}
