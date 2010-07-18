#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class NukePowerInfo : SupportPowerInfo
	{
		public override object Create(ActorInitializer init) { return new NukePower(init.self, this); }
	}
	
	class NukePower : SupportPower, IResolveOrder
	{
		public NukePower(Actor self, NukePowerInfo info) : base(self, info) { }
	
		protected override void OnActivate()
		{
			Game.controller.orderGenerator =
				new GenericSelectTargetWithBuilding<NukeSilo>(Owner.PlayerActor, "NuclearMissile", "nuke");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsAvailable) return;

			if (order.OrderString == "NuclearMissile")
			{
				var silo = self.World.Queries.OwnedBy[self.Owner]
					.Where(a => a.traits.Contains<NukeSilo>())
					.FirstOrDefault();
				if (silo != null)
					silo.traits.Get<RenderBuilding>().PlayCustomAnim(silo, "active");
				
				// Play to everyone but the current player
				if (Owner != Owner.World.LocalPlayer)
					Sound.Play(Info.LaunchSound);
				
				silo.traits.Get<NukeSilo>().Attack(order.TargetLocation);
				
				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}
	}

	// tag trait for the building
	class NukeSiloInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string MissileWeapon = "";
		public object Create(ActorInitializer init) { return new NukeSilo(init.self); }
	}
	
	class NukeSilo
	{
		Actor self;
		public NukeSilo(Actor self)
		{
			this.self = self;
		}
		
		public void Attack(int2 targetLocation)
		{
			self.traits.Get<RenderBuilding>().PlayCustomAnim(self, "active");
			
			self.World.AddFrameEndTask(w =>
			{
				//FIRE ZE MISSILES
				w.Add(new NukeLaunch(self, self.Info.Traits.Get<NukeSiloInfo>().MissileWeapon, targetLocation));
			});
		}
	}
}
