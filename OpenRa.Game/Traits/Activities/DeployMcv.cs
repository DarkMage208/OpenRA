﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class DeployMcv : IActivity
	{
		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			Game.world.AddFrameEndTask( _ =>
			{
				Game.world.Remove( self );
				Game.world.Add( new Actor( "fact", self.Location - new int2( 1, 1 ), self.Owner ) );
			} );
			return null;
		}

		public void Cancel( Actor self )
		{
			// Cancel can't happen between this being moved to the head of the list, and it being Ticked.
			throw new InvalidOperationException( "DeployMcvAction: Cancel() should never occur." );
		}
	}
}
