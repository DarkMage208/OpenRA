﻿using System;
using System.Collections.Generic;
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
		public Animation anim { get { return anims[ "" ].Animation; } }

		public RenderSimple(Actor self)
		{
			anims.Add( "", new Animation( self.LegacyInfo.Image ?? self.LegacyInfo.Name ) );
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
