﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	abstract class RenderSimpleInfo : ITraitInfo
	{
		public readonly string Image = null;

		public abstract object Create(Actor self);
	}

	abstract class RenderSimple : IRender, ITick
	{
		public Dictionary<string, AnimationWithOffset> anims = new Dictionary<string, AnimationWithOffset>();
		public Animation anim { get { return anims[""].Animation; } protected set { anims[""].Animation = value; } }

		public string GetImage(Actor self)
		{
			return self.Info.Traits.Get<RenderSimpleInfo>().Image ?? self.Info.Name;
		}

		public RenderSimple(Actor self)
		{
			anims.Add( "", new Animation( GetImage(self) ) );
		}

		public virtual IEnumerable<Renderable> Render( Actor self )
		{
			foreach( var a in anims.Values )
				if( a.DisableFunc == null || !a.DisableFunc() )
					yield return a.Image( self );
		}

		public virtual void Tick(Actor self)
		{
			foreach( var a in anims.Values )
				a.Animation.Tick();
		}

		public class AnimationWithOffset
		{
			public Animation Animation;
			public Func<float2> OffsetFunc;
			public Func<bool> DisableFunc;
			public int ZOffset;

			public AnimationWithOffset( Animation a )
				: this( a, null, null )
			{
			}

			public AnimationWithOffset( Animation a, Func<float2> o, Func<bool> d )
			{
				this.Animation = a;
				this.OffsetFunc = o;
				this.DisableFunc = d;
			}

			public Renderable Image( Actor self )
			{
				var r = Util.Centered( self, Animation.Image, self.CenterLocation 
					+ (OffsetFunc != null ? OffsetFunc() : float2.Zero) );
				return ZOffset != 0 ? r.WithZOffset(ZOffset) : r;
			}

			public static implicit operator AnimationWithOffset( Animation a )
			{
				return new AnimationWithOffset( a );
			}
		}
	}
}
