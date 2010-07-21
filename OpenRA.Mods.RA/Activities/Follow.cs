﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class Follow : IActivity
	{
		Target Target;
		int Range;

		public Follow(Target target, int range)
		{
			Target = target;
			Range = range;
		}

		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			if (!Target.IsValid)
				return NextActivity;

			var inRange = ( Util.CellContaining( Target.CenterLocation ) - self.Location ).LengthSquared < Range * Range;

			if( !inRange )
				return new Move( Target, Range ) { NextActivity = this };

			return this;
		}

		public void Cancel(Actor self)
		{
			Target = Target.None;
		}
	}
}
