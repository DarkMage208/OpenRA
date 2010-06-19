using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int InitialCash = 10000;
		public readonly int InitialOre = 0;
		public readonly int AdviceInterval = 250;

		public object Create(ActorInitializer init) { return new PlayerResources(init.self); }
	}

	public class PlayerResources : ITick
	{
		Player Owner;
		int AdviceInterval;
		public PlayerResources(Actor self)
		{
			Owner = self.Owner;
			Cash = self.Info.Traits.Get<PlayerResourcesInfo>().InitialCash;
			Ore = self.Info.Traits.Get<PlayerResourcesInfo>().InitialOre;
			AdviceInterval = self.Info.Traits.Get<PlayerResourcesInfo>().AdviceInterval;
		}

		[Sync]
		public int Cash;
		[Sync]
		public int DisplayCash;
		
		[Sync]
		public int Ore;
		[Sync]
		public int OreCapacity;
		[Sync]
		public int DisplayOre;
		
		[Sync]
		public int PowerProvided;
		[Sync]
		public int PowerDrained;
		
		const float displayCashFracPerFrame = .07f;
		const int displayCashDeltaPerFrame = 37;
		int nextSiloAdviceTime = 0;
		void TickOre(Actor self)
		{
			OreCapacity = self.World.Queries.OwnedBy[Owner].WithTrait<StoresOre>()
				.Sum(a => a.Actor.Info.Traits.Get<StoresOreInfo>().Capacity);
			
			if (Ore > OreCapacity)
				Ore = OreCapacity;
			
			if (--nextSiloAdviceTime <= 0)
			{
				if (Ore > 0.8*OreCapacity)
					Owner.GiveAdvice(Owner.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>().SilosNeeded);
				
				nextSiloAdviceTime = AdviceInterval;
			}
			
			var diff = Math.Abs(Cash - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			if (DisplayCash < Cash)
			{
				DisplayCash += move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
			}
			else if (DisplayCash > Cash)
			{
				DisplayCash -= move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
			}
			
			diff = Math.Abs(Ore - DisplayOre);
			move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			if (DisplayOre < Ore)
			{
				DisplayOre += move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
			}
			else if (DisplayOre > Ore)
			{
				DisplayOre -= move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
			}
		}
		
		int nextPowerAdviceTime = 0;
		void TickPower()
		{
			var oldBalance = PowerProvided - PowerDrained;

			PowerProvided = 0;
			PowerDrained = 0;

			var myBuildings = Owner.World.Queries.OwnedBy[Owner].WithTrait<Building>();

			foreach (var a in myBuildings)
			{
				var q = a.Trait.GetPowerUsage();
				if (q > 0)
					PowerProvided += q;
				else
					PowerDrained -= q;
			}

			if (PowerProvided - PowerDrained < 0)
				if (PowerProvided - PowerDrained != oldBalance)
					nextPowerAdviceTime = 0;
			
			if (--nextPowerAdviceTime <= 0)
			{
				if (PowerProvided - PowerDrained < 0)
					Owner.GiveAdvice(Rules.Info["world"].Traits.Get<EvaAlertsInfo>().LowPower);
				
				nextPowerAdviceTime = AdviceInterval;
			}
		}

		public PowerState GetPowerState()
		{
			if (PowerProvided >= PowerDrained) return PowerState.Normal;
			if (PowerProvided > PowerDrained / 2) return PowerState.Low;
			return PowerState.Critical;
		}

		public float GetSiloFullness() { return (float)Ore / OreCapacity; }

		public void GiveOre(int num)
		{
			Ore += num;
			
			if (Ore > OreCapacity)
			{
				nextSiloAdviceTime = 0;
				Ore = OreCapacity;
			}
		}
		
		public bool TakeOre(int num)
		{
			if (Ore < num) return false;
			Ore -= num;
			
			return true;
		}
		
		public void GiveCash(int num)
		{
			Cash += num;
		}
		
		public bool TakeCash(int num)
		{			
			if (Cash + Ore < num) return false;
			
			// Spend ore before cash
			Ore -= num;
			if (Ore < 0)
			{
				Cash += Ore;
				Ore = 0;	
			}
			
			return true;
		}

		public void Tick(Actor self)
		{
			TickPower();
			TickOre(self);
		}
	}
}
