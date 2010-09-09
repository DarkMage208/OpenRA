#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class ProductionQueueInfo : ITraitInfo
	{
		public readonly string Type = null;
		public float BuildSpeed = 0.4f;
		public readonly int LowPowerSlowdown = 3;
		public object Create(ActorInitializer init) { return new ProductionQueue(init.self, this); }
	}

	public class ProductionQueue : IResolveOrder, ITick, ITechTreeElement
	{
		public readonly Actor self;
		public ProductionQueueInfo Info;
		
		// TODO: sync these
		// A list of things we are currently building
		List<ProductionItem> Queue = new List<ProductionItem>();
		
		// A list of things we could possibly build, even if our race doesn't normally get it
		Dictionary<ActorInfo, ProductionState> Produceable = new Dictionary<ActorInfo, ProductionState>();

		public ProductionQueue( Actor self, ProductionQueueInfo info )
		{
			this.self = self;
			this.Info = info;
		}
		
		// Trait initialization bites us when queue lives on PlayerActor; delay init until first tick
		bool initialized = false;
		void Initialize()
		{
			initialized = true;
			var ttc = self.Owner.PlayerActor.Trait<TechTreeCache>();
			foreach (var a in Rules.TechTree.AllBuildables(Info.Type))
			{
				var bi = a.Traits.Get<BuildableInfo>();
				// Can our race build this by satisfying normal prereqs?
				var buildable = bi.Owner.Contains(self.Owner.Country.Race);
				Produceable.Add( a, new ProductionState(){ Visible = buildable && !bi.Hidden } );
				if (buildable)
					ttc.Add( a.Name, a.Traits.Get<BuildableInfo>().Prerequisites.ToList(), this );
			}
		}
		
		public void OverrideProduction(ActorInfo type, bool buildable)
		{
			Produceable[type].Buildable = buildable;
			Produceable[type].Sticky = true;
		}
				
		public void PrerequisitesAvailable(string key)
		{
			var ps = Produceable[ Rules.Info[key] ];
			if (!ps.Sticky)
				ps.Buildable = true;
		}
		
		public void PrerequisitesUnavailable(string key)
		{
			var ps = Produceable[ Rules.Info[key] ];
			if (!ps.Sticky)
				ps.Buildable = false;
		}

		public ProductionItem CurrentItem()
		{
			return Queue.ElementAtOrDefault(0);
		}
		
		public IEnumerable<ProductionItem> AllQueued()
		{
			return Queue;
		}
		
		ActorInfo[] None = new ActorInfo[]{};
		public IEnumerable<ActorInfo> AllItems()
		{
			if (!QueueActive)
				return None;
			
			if (Game.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().AllTech)
				return Produceable.Select(a => a.Key);
			
			return Produceable.Where(a => a.Value.Buildable || a.Value.Visible).Select(a => a.Key);
		}
		
		public IEnumerable<ActorInfo> BuildableItems()
		{
			if (!QueueActive)
				return None;
			
			if (Game.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().AllTech)
				return Produceable.Select(a => a.Key);
			
			return Produceable.Where(a => a.Value.Buildable).Select(a => a.Key);
		}
		
		public bool CanBuild(ActorInfo actor)
		{
			var buildings = Rules.TechTree.GatherBuildings( self.Owner );
			return Rules.TechTree.CanBuild(actor, self.Owner, buildings);
		}
		
		[Sync] bool QueueActive = true;
		public void Tick( Actor self )
		{			
			if (!initialized)
				Initialize();
			
			if (self == self.Owner.PlayerActor)
				QueueActive = self.World.Queries.OwnedBy[self.Owner].WithTrait<Production>()
					.Where(x => x.Trait.Info.Produces.Contains(Info.Type))
					.Any();
			
			while( Queue.Count > 0 && !BuildableItems().Any(b => b.Name == Queue[ 0 ].Item) )
			{
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(Queue[0].TotalCost - Queue[0].RemainingCost); // refund what's been paid so far.
				FinishProduction();
			}
			if( Queue.Count > 0 )
				Queue[ 0 ].Tick( self.Owner );
		}

		public void ResolveOrder( Actor self, Order order )
		{
			switch( order.OrderString )
			{
			case "StartProduction":
				{
					var unit = Rules.Info[order.TargetString];
					var bi = unit.Traits.Get<BuildableInfo>();
					if (bi.Queue != Info.Type)
						return; /* Not built by this queue */
				
					var cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
					var time = GetBuildTime(order.TargetString);

					if (!BuildableItems().Any(b => b.Name == order.TargetString))
						return;	/* you can't build that!! */
				
					bool hasPlayedSound = false;
					
					for (var n = 0; n < order.TargetLocation.X; n++)	// repeat count
					{
						BeginProduction(new ProductionItem(this, order.TargetString, (int)time, cost,
								() => self.World.AddFrameEndTask(
									_ =>
									{
										var isBuilding = unit.Traits.Contains<BuildingInfo>();
										if (!hasPlayedSound)
										{
											var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
											Sound.PlayToPlayer(order.Player, isBuilding ? eva.BuildingReadyAudio : eva.UnitReadyAudio);
											hasPlayedSound = true;
										}
										if (!isBuilding)
											BuildUnit(order.TargetString);
									})));
					}
					break;
				}
			case "PauseProduction":
				{
					if( Queue.Count > 0 && Queue[0].Item == order.TargetString )
						Queue[0].Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					CancelProduction(order.TargetString);
					break;
				}
			}
		}
		
		public int GetBuildTime(String unitString)
		{
			var unit = Rules.Info[unitString];
			if (unit == null || ! unit.Traits.Contains<BuildableInfo>())
				return 0;
			
			if (Game.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().FastBuild) return 0;
			var cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
			var time = cost
				* Info.BuildSpeed
				* (25 * 60) /* frames per min */				/* todo: build acceleration, if we do that */
				 / 1000;
			return (int) time;
		}

		void CancelProduction( string itemName )
		{			
			if (Queue.Count == 0)
				return; // Nothing to do here
			
			var lastIndex = Queue.FindLastIndex( a => a.Item == itemName );
			if (lastIndex > 0)
				Queue.RemoveAt(lastIndex);
			else if( lastIndex == 0 )
			{
				var item = Queue[0];
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(item.TotalCost - item.RemainingCost); // refund what's been paid so far.
				FinishProduction();
			}
		}

		public void FinishProduction()
		{
			if (Queue.Count == 0) return;
			Queue.RemoveAt(0);
		}

		void BeginProduction( ProductionItem item )
		{
			Queue.Add(item);
		}

		static bool IsDisabledBuilding(Actor a)
		{
			var building = a.TraitOrDefault<Building>();
			return building != null && building.Disabled;
		}

		void BuildUnit( string name )
		{			
			if (self == self.Owner.PlayerActor)
			{
				// original ra behavior; queue lives on PlayerActor, need to find a production structure
				var producers = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<Production>()
				.Where(x => x.Trait.Info.Produces.Contains(Info.Type))
				.OrderByDescending(x => x.Actor.IsPrimaryBuilding() ? 1 : 0 ) // prioritize the primary.
				.ToArray();

				if (producers.Length == 0)
				{
					CancelProduction(name);
					return;
				}
				
				foreach (var p in producers)
				{
					if (IsDisabledBuilding(p.Actor)) continue;
	
					if (p.Trait.Produce(p.Actor, Rules.Info[ name ]))
					{
						FinishProduction();
						break;
					}
				}
			}
			else
			{
				// queue lives on actor; is produced at same actor
				var sp = self.TraitsImplementing<Production>().Where(p => p.Info.Produces.Contains(Info.Type)).FirstOrDefault();
				if (sp != null && !IsDisabledBuilding(self) && sp.Produce(self, Rules.Info[ name ]))
						FinishProduction();
			}
		}
	}

	public class ProductionState
	{
		public bool Visible = false;
		public bool Buildable = false;
		public bool Sticky = false;
	}
	
	public class ProductionItem
	{
		public readonly string Item;
		public readonly ProductionQueue Queue;
		public readonly int TotalTime;
		public readonly int TotalCost;
		public int RemainingTime { get; private set; }
		public int RemainingCost { get; private set; }

		public bool Paused = false, Done = false;
		public Action OnComplete;

		int slowdown = 0;

		public ProductionItem(ProductionQueue queue, string item, int time, int cost, Action onComplete)
		{
			if (time <= 0) time = 1;
			Item = item;
			RemainingTime = TotalTime = time;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
			Queue = queue;

			Log.Write("debug", "new ProductionItem: {0} time={1} cost={2}", item, time, cost);
		}

		public void Tick(Player player)
		{
			if (Done)
			{
				if (OnComplete != null) OnComplete();
				return;
			}

			if (Paused) return;

			if (player.PlayerActor.Trait<PlayerResources>().GetPowerState() != PowerState.Normal)
			{
				if (--slowdown <= 0)
					slowdown = Queue.Info.LowPowerSlowdown; 
				else
					return;
			}

			var costThisFrame = RemainingCost / RemainingTime;
			if (costThisFrame != 0 && !player.PlayerActor.Trait<PlayerResources>().TakeCash(costThisFrame)) return;
			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if (RemainingTime > 0) return;

			Done = true;
		}
	}
}
