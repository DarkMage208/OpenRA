﻿using System;
using OpenRa.Effects;

namespace OpenRa.Traits
{
	class RenderBuildingInfo : RenderSimpleInfo
	{
		public override object Create(Actor self) { return new RenderBuilding(self); }
	}

	class RenderBuilding : RenderSimple, INotifyDamage, INotifySold
	{
		const int SmallBibStart = 1;
		const int LargeBibStart = 5;

		public RenderBuilding(Actor self)
			: base(self)
		{
			if( Game.skipMakeAnims )
				Complete( self );
			else
				anim.PlayThen( "make", () => self.World.AddFrameEndTask( _ => Complete( self ) ) );

			DoBib(self, false);
		}

		void Complete( Actor self )
		{
			anim.PlayRepeating( GetPrefix(self) + "idle" );
			foreach( var x in self.traits.WithInterface<INotifyBuildComplete>() )
				x.BuildingComplete( self );
		}

		void DoBib(Actor self, bool isRemove)
		{
			var buildingInfo = self.Info.Traits.Get<BuildingInfo>();
			if (buildingInfo.Bib)
			{
				var size = buildingInfo.Dimensions.X;
				var bibOffset = buildingInfo.Dimensions.Y - 1;
				var startIndex = (size == 2) ? SmallBibStart : LargeBibStart;

				for (int i = 0; i < 2 * size; i++)
				{
					var p = self.Location + new int2(i % size, i / size + bibOffset);
					if (isRemove)
					{
						if (self.World.Map.MapTiles[p.X, p.Y].smudge == (byte)(i + startIndex))
							self.World.Map.MapTiles[ p.X, p.Y ].smudge = 0;
					}
					else
						self.World.Map.MapTiles[p.X, p.Y].smudge = (byte)(i + startIndex);
				}
			}
		}

		protected string GetPrefix(Actor self)
		{
			return self.GetDamageState() == DamageState.Half ? "damaged-" : "";
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			anim.PlayThen(GetPrefix(self) + name, 
				() => anim.PlayRepeating(GetPrefix(self) + "idle"));
		}

		public void PlayCustomAnimBackwards(Actor self, string name, Action a)
		{
			anim.PlayBackwardsThen(GetPrefix(self) + name,
				() => { anim.PlayRepeating(GetPrefix(self) + "idle"); a(); });
		}

		public virtual void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged)
				return;

			switch( e.DamageState )
			{
				case DamageState.Normal:
					anim.ReplaceAnim("idle");
					break;
				case DamageState.Half:
					anim.ReplaceAnim("damaged-idle");
					Sound.Play("kaboom1.aud");
					break;
				case DamageState.Dead:
					DoBib(self, true);
					self.World.AddFrameEndTask(w => w.Add(new Explosion(self.CenterLocation.ToInt2(), 7, false)));
					break;
			}
		}

		public void Selling( Actor self )
		{
			anim.PlayBackwardsThen( "make", null );
			Sound.Play("cashturn.aud");
		}

		public void Sold(Actor self) { DoBib(self, true); }
	}
}
