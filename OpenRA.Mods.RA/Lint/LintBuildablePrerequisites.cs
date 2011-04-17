﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
    class LintBuildablePrerequisites : ILintPass
    {
        public void Run(Action<string> emitError)
        {
			/* do something intelligent here. */
        }
    }
	
	class CheckAutotargetWiring : ILintPass
	{
		public void Run(Action<string> emitError)
        {
            foreach( var i in Rules.Info )
			{
				if (i.Key.StartsWith("^"))
					continue;
				var attackMove = i.Value.Traits.GetOrDefault<AttackMoveInfo>();
				if (attackMove != null && !attackMove.JustMove &&
				    !i.Value.Traits.Contains<AutoTargetInfo>())
					emitError( "{0} has AttackMove setup without AutoTarget, and will crash when resolving that order.".F(i.Key) );
			}
        }
	}
}
