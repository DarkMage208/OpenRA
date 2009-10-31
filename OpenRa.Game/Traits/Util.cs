﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	static class Util
	{
		public static void TickFacing( ref int facing, int desiredFacing, int rot )
		{
			var leftTurn = ( facing - desiredFacing ) & 0xFF;
			var rightTurn = ( desiredFacing - facing ) & 0xFF;
			if( Math.Min( leftTurn, rightTurn ) < rot )
				facing = desiredFacing & 0xFF;
			else if( rightTurn < leftTurn )
				facing = ( facing + rot ) & 0xFF;
			else
				facing = ( facing - rot ) & 0xFF;
		}

		static float2[] fvecs = Graphics.Util.MakeArray<float2>( 32,
			i => -float2.FromAngle( i / 16.0f * (float)Math.PI ) * new float2( 1f, 1.3f ) );

		public static int GetFacing( float2 d, int currentFacing )
		{
			if( float2.WithinEpsilon( d, float2.Zero, 0.001f ) )
				return currentFacing;

			int highest = -1;
			float highestDot = -1.0f;

			for( int i = 0 ; i < fvecs.Length ; i++ )
			{
				float dot = float2.Dot( fvecs[ i ], d );
				if( dot > highestDot )
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest * 8;
		}

		public static int GetNearestFacing( int facing, int desiredFacing )
		{
			var turn = desiredFacing - facing;
			if( turn > 128 )
				turn -= 256;
			if( turn < -128 )
				turn += 256;

			return facing + turn;
		}

		static float2 RotateVectorByFacing(float2 v, int facing, float ecc)
		{
			var angle = (facing / 256f) * (2 * (float)Math.PI);
			var sinAngle = (float)Math.Sin(angle);
			var cosAngle = (float)Math.Cos(angle);

			return new float2(
				(cosAngle * v.X + sinAngle * v.Y),
				ecc * (cosAngle * v.Y - sinAngle * v.X));
		}

		static float2 GetRecoil(Actor self, float recoil)
		{
			if (self.unitInfo.Recoil == 0) return float2.Zero;
			var rut = self.traits.WithInterface<RenderUnitTurreted>().FirstOrDefault();
			if (rut == null) return float2.Zero;

			var facing = self.traits.Get<Turreted>().turretFacing;
			var quantizedFacing = facing - facing % rut.turretAnim.CurrentSequence.Length;

			return RotateVectorByFacing(new float2(0, recoil * self.unitInfo.Recoil), quantizedFacing, .7f);
		}

		public static float2 GetTurretPosition(Actor self, int[] offset, float recoil)
		{
			var ru = self.traits.WithInterface<RenderUnit>().FirstOrDefault();
			if (ru == null) return int2.Zero;	/* things that don't have a rotating base don't need the turrets repositioned */

			var bodyFacing = self.traits.Get<Mobile>().facing;
			var quantizedFacing = bodyFacing - bodyFacing % ru.anim.CurrentSequence.Length;

			return (RotateVectorByFacing(new float2(offset[0], offset[1]), quantizedFacing, .7f) + GetRecoil(self, recoil));
		}

	}
}
