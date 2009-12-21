﻿using System;
using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class RenderBuilding : RenderSimple, INotifyDamage
	{
		const int SmallBibStart = 1;
		const int LargeBibStart = 5;

		public RenderBuilding(Actor self)
			: base(self)
		{
			if( Game.skipMakeAnims )
				Complete( self );
			else
				anim.PlayThen( "make", () => Complete( self ) );

			DoBib(self, false);
		}

		void Complete( Actor self )
		{
			anim.PlayRepeating( "idle" );
			foreach( var x in self.traits.WithInterface<INotifyBuildComplete>() )
				x.BuildingComplete( self );
		}

		void DoBib(Actor self, bool isRemove)
		{
			var buildingInfo = self.traits.Get<Building>().unitInfo;
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
						if (Rules.Map.MapTiles[p.X, p.Y].smudge == (byte)(i + startIndex))
							Rules.Map.MapTiles[ p.X, p.Y ].smudge = 0;
					}
					else
						Rules.Map.MapTiles[p.X, p.Y].smudge = (byte)(i + startIndex);
				}
			}
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
					Game.world.AddFrameEndTask(w => w.Add(new Explosion(self.CenterLocation.ToInt2(), 7, false)));
					break;
			}
		}
	}
}
