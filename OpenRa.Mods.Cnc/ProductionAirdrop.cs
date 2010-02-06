﻿using System.Collections.Generic;
using System.Linq;
using OpenRa.GameRules;
using OpenRa.Traits.Activities;
using OpenRa.Traits;
using OpenRa.Mods.RA;

namespace OpenRa.Mods.Cnc
{
	public class ProductionAirdropInfo : ProductionInfo
	{
		public override object Create(Actor self) { return new ProductionAirdrop(self); }
	}
	
	class ProductionAirdrop : Production
	{
		public ProductionAirdrop(Actor self) : base(self) { }
		
		public override bool Produce( Actor self, ActorInfo producee )
		{
			var location = CreationLocation(self, producee);
			var owner = self.Owner;
			
			// Start beyond the edge of the map, to give a finite delay, and ability to land when AFLD is on map edge
			var startPos = new int2(owner.World.Map.XOffset + owner.World.Map.Width+15, self.Location.Y);
			var endPos = new int2(owner.World.Map.XOffset, self.Location.Y);
			var unloadOffset = new int2(1,1);
			var exitOffset = new int2(3,1);
			
			var rp = self.traits.GetOrDefault<RallyPoint>();
			owner.World.AddFrameEndTask(w =>
			{
				var a = w.CreateActor("C17", startPos, owner);
				var cargo = a.traits.Get<Cargo>();

				var newUnit = new Actor(self.World, producee.Name, new int2(0, 0), self.Owner);
				cargo.Load(a, newUnit);
				
				a.CancelActivity();
				
				a.QueueActivity(new Land(self));
				a.QueueActivity(new CallFunc(() => 
				{
					if (self.IsDead)
						return;
					
					var actor = cargo.Unload(self);
					self.World.AddFrameEndTask(ww =>
					{
						ww.Add(actor);
						actor.traits.Get<Mobile>().TeleportTo(actor, self.Location + unloadOffset);
						newUnit.traits.Get<Unit>().Facing = 192;
						actor.CancelActivity();
						actor.QueueActivity(new Move(self.Location + exitOffset, self));
						actor.QueueActivity(new Move(rp.rallyPoint, 0));

						foreach (var t in self.traits.WithInterface<INotifyProduction>())
							t.UnitProduced(self, actor);
					});
				}));
				a.QueueActivity(new Fly(endPos));
				a.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
