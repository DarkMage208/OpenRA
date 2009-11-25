﻿using System;
using System.Collections.Generic;
using System.Linq;
using IjwFramework.Types;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitRotor : RenderUnit
	{
		public Animation rotorAnim, secondRotorAnim;

		public RenderUnitRotor( Actor self )
			: base(self)
		{
			rotorAnim = new Animation(self.unitInfo.Name);
			rotorAnim.PlayRepeating("rotor");

			if (self.unitInfo.SecondaryAnim != null)
			{
				secondRotorAnim = new Animation(self.unitInfo.Name);
				secondRotorAnim.PlayRepeating(self.unitInfo.SecondaryAnim);
			}
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var unit = self.traits.Get<Unit>();

			yield return Util.CenteredShadow(self, anim.Image, self.CenterLocation);
			yield return Util.CenteredShadow(self, rotorAnim.Image, self.CenterLocation
				+ Util.GetTurretPosition(self, unit, self.unitInfo.PrimaryOffset, 0));
			if (self.unitInfo.SecondaryOffset != null)
				yield return Util.CenteredShadow(self, (secondRotorAnim ?? rotorAnim).Image, self.CenterLocation
					+ Util.GetTurretPosition(self, unit, self.unitInfo.SecondaryOffset, 0));

			var heli = self.traits.Get<Helicopter>();
			var p = self.CenterLocation - new float2( 0, heli.altitude );

			yield return Util.Centered(self, anim.Image, p);
			yield return Util.Centered(self, rotorAnim.Image, p
				+ Util.GetTurretPosition( self, unit, self.unitInfo.PrimaryOffset, 0 ) );
			if (self.unitInfo.SecondaryOffset != null)
				yield return Util.Centered(self, (secondRotorAnim ?? rotorAnim).Image, p
					+ Util.GetTurretPosition( self, unit, self.unitInfo.SecondaryOffset, 0 ) );
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			rotorAnim.Tick();
			if (secondRotorAnim != null)
				secondRotorAnim.Tick();

			var heli = self.traits.GetOrDefault<Helicopter>();
			if (heli == null) return;
			
			var isFlying = heli.altitude > 0;

			if (isFlying ^ (rotorAnim.CurrentSequence.Name != "rotor")) 
				return;

			rotorAnim.PlayRepeatingPreservingPosition(isFlying ? "rotor" : "slow-rotor");
			if (secondRotorAnim != null)
				secondRotorAnim.PlayRepeatingPreservingPosition(isFlying ? "rotor2" : "slow-rotor2");
		}
	}
}
