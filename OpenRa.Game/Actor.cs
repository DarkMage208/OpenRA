﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game
{
	class Actor
	{
		public readonly TypeDictionary traits = new TypeDictionary();
		public readonly UnitInfo Info;

		public readonly uint ActorID;
		public int2 Location;
		public Player Owner;
		public int Health;
		IActivity currentActivity;

		public Actor( ActorInfo info, int2 location, Player owner )
		{
			ActorID = Game.world.NextAID();
			Info = (UnitInfo)info; // temporary
			Location = location;
			CenterLocation = Traits.Util.CenterOfCell(Location);
			Owner = owner;

			if (Info == null) return;

			Health = Info.Strength;	/* todo: handle cases where this is not true! */

			if( Info.Traits == null )
				throw new InvalidOperationException( "No Actor traits for {0}; add Traits= to units.ini for appropriate unit".F(Info.Name) );

			foreach (var traitName in Info.Traits)
			{		/* todo: a better solution than `the assembly Mobile lives in`, for mod support & sanity. */
				var type = typeof(Mobile).Assembly.GetType(typeof(Mobile).Namespace + "." + traitName, true, false);
				var ctor = type.GetConstructor(new[] { typeof(Actor) });
				if (ctor == null)
					throw new InvalidOperationException("Trait {0} does not have the correct constructor: {0}(Actor self)".F(type.Name));

				traits.Add(type, ctor.Invoke(new object[] { this }));
			}
		}

		public void Tick()
		{
			var nextActivity = currentActivity;
			while( nextActivity != null )
			{
				currentActivity = nextActivity;
				nextActivity = nextActivity.Tick( this );
			}

			foreach (var tick in traits.WithInterface<ITick>())
				tick.Tick(this);
		}

		public float2 CenterLocation;
		public float2 SelectedSize
		{
			get
			{
				var firstSprite = Render().FirstOrDefault();
				if( firstSprite == null )
					return new float2( 0, 0 );
				return firstSprite.a.size;
			}
		}

		public IEnumerable<Tuple<Sprite, float2, int>> Render()
		{
			var mods = traits.WithInterface<IRenderModifier>();
			var sprites = traits.WithInterface<IRender>().SelectMany(x => x.Render(this));
			return mods.Aggregate(sprites, (m, p) => p.ModifyRender(this, m));
		}

		public Order Order( int2 xy, MouseInput mi )
		{
			if (Owner != Game.LocalPlayer)
				return null;

			if (!Rules.Map.IsInMap(xy.X, xy.Y))
				return null;

			var underCursor = Game.UnitInfluence.GetUnitAt( xy ) 
				?? Game.BuildingInfluence.GetBuildingAt( xy );
			if (underCursor != null && !underCursor.Info.Selectable)
				underCursor = null;

			return traits.WithInterface<IOrder>()
				.Select( x => x.IssueOrder( this, xy, mi, underCursor ) )
				.FirstOrDefault( x => x != null );
		}

		public RectangleF Bounds
		{
			get
			{
				var size = SelectedSize;
				var loc = CenterLocation - 0.5f * size;
				return new RectangleF(loc.X, loc.Y, size.X, size.Y);
			}
		}

		public bool IsDead { get { return Health <= 0; } }

		public void InflictDamage(Actor attacker, int damage, WarheadInfo warhead)
		{
			/* todo: auto-retaliate, etc */
			/* todo: death sequence for infantry based on inflictor */

			if (IsDead) return;		/* overkill! don't count extra hits as more kills! */

			Health -= damage;
			if (Health <= 0)
			{
				Health = 0;
				if (attacker.Owner != null)
					attacker.Owner.Kills++;

				Game.world.AddFrameEndTask(w => w.Remove(this));

				if (Owner == Game.LocalPlayer && !traits.Contains<Building>()) 
					Sound.Play("unitlst1.aud");

				if (traits.Contains<Building>())
					Sound.Play("kaboom22.aud");
			}

			var halfStrength = Info.Strength * Rules.General.ConditionYellow;
			if (Health < halfStrength && (Health + damage) >= halfStrength)
			{
				/* we just went below half health! */
				foreach (var nd in traits.WithInterface<INotifyDamage>())
					nd.Damaged(this, DamageState.Half);
			}

			foreach (var ndx in traits.WithInterface<INotifyDamageEx>())
				ndx.Damaged(this, damage, warhead);
		}

		public void QueueActivity( IActivity nextActivity )
		{
			if( currentActivity == null )
			{
				currentActivity = nextActivity;
				return;
			}
			var act = currentActivity;
			while( act.NextActivity != null )
			{
				act = act.NextActivity;
			}
			act.NextActivity = nextActivity;
		}

		public void CancelActivity()
		{
			if( currentActivity != null )
				currentActivity.Cancel( this );
		}

		// For pathdebug, et al
		public IActivity GetCurrentActivity()
		{
			return currentActivity;
		}
	}
}
