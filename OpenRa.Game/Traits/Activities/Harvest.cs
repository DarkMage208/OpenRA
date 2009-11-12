﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Harvest : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isHarvesting = false;

		public IActivity Tick( Actor self, Mobile mobile )
		{
			if( isHarvesting ) return null;

			if( NextActivity != null )
				return NextActivity;

			var harv = self.traits.Get<Harvester>();

			if( harv.IsFull )
				return new DeliverOre { NextActivity = NextActivity };

			var isGem = false;
			if( Game.map.ContainsResource( self.Location ) &&
				Game.map.Harvest( self.Location, out isGem ) )
			{
				var harvestAnim = "harvest" + Util.QuantizeFacing( mobile.facing, 8 );
				var renderUnit = self.traits.WithInterface<RenderUnit>().First();	/* better have one of these! */
				if( harvestAnim != renderUnit.anim.CurrentSequence.Name )
				{
					isHarvesting = true;
					renderUnit.PlayCustomAnimation( self, harvestAnim, () => isHarvesting = false );
				}
				harv.AcceptResource( isGem );
				return null;
			}
			else
			{
				mobile.QueueActivity( new Move(
					() =>
					{
						var search = new PathSearch
						{
							heuristic = loc => ( Game.map.ContainsResource( loc ) ? 0 : 1 ),
							umt = UnitMovementType.Wheel,
							checkForBlocked = true
						};
						search.AddInitialCell( self.Location );
						return Game.PathFinder.FindPath( search );
					} ) );
				mobile.QueueActivity( new Harvest() );
				return NextActivity;
			}
		}

		public void Cancel(Actor self, Mobile mobile) { }
	}
}
