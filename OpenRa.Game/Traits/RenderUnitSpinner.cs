﻿using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitSpinnerInfo : RenderUnitInfo
	{
		public override object Create(Actor self) { return new RenderUnitSpinner(self); }
	}

	class RenderUnitSpinner : RenderUnit
	{
		public Animation spinnerAnim;

		public RenderUnitSpinner( Actor self )
			: base(self)
		{
			var unit = self.traits.Get<Unit>();

			spinnerAnim = new Animation( self.Info.Name );
			spinnerAnim.PlayRepeating( "spinner" );
			anims.Add( "spinner", new AnimationWithOffset(
				spinnerAnim,
				() => Util.GetTurretPosition( self, unit, self.Info.PrimaryOffset, 0 ),
				null ) );
		}
	}
}
