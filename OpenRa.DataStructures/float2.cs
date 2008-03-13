using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

namespace OpenRa
{
	[StructLayout(LayoutKind.Sequential)]
	public struct float2
	{
		public float X, Y;

		public float2(float x, float y) { X = x; Y = y; }
		public float2(PointF p) { X = p.X; Y = p.Y; }
		public float2(Point p) { X = p.X; Y = p.Y; }
		public float2(Size p) { X = p.Width; Y = p.Height; }
		public float2(SizeF p) { X = p.Width; Y = p.Height; }

		public PointF ToPointF() { return new PointF(X, Y); }

		public static implicit operator float2(int2 src) { return new float2(src.X, src.Y); }

		public static float2 operator +(float2 a, float2 b) { return new float2(a.X + b.X, a.Y + b.Y); }
		public static float2 operator -(float2 a, float2 b) { return new float2(a.X - b.X, a.Y - b.Y); }

		public static float2 operator -(float2 a) { return new float2(-a.X, -a.Y); }

		static float Lerp(float a, float b, float t) { return a + t * (b - a); }

		public static float2 Lerp(float2 a, float2 b, float t)
		{
			return new float2(
				Lerp(a.X, b.X, t),
				Lerp(a.Y, b.Y, t));
		}

		public static float2 Lerp(float2 a, float2 b, float2 t)
		{
			return new float2(
				Lerp(a.X, b.X, t.X),
				Lerp(a.Y, b.Y, t.Y));
		}

		public static float2 FromAngle(float a) { return new float2((float)Math.Sin(a), (float)Math.Cos(a)); }

		static float Constrain(float x, float a, float b) { return x < a ? a : x > b ? b : x; }

		public float2 Constrain(float2 min, float2 max)
		{
			return new float2(
				Constrain(X, min.X, max.X),
				Constrain(Y, min.Y, max.Y));
		}

		public static float2 operator *(float a, float2 b) { return new float2(a * b.X, a * b.Y); }
		public static float2 operator *( float2 a, float2 b ) { return new float2( a.X * b.X, a.Y * b.Y ); }
		public static float2 operator /( float2 a, float2 b ) { return new float2( a.X / b.X, a.Y / b.Y ); }

		public static readonly float2 Zero = new float2(0, 0);

		public static bool WithinEpsilon(float2 a, float2 b, float e)
		{
			float2 d = a - b;
			return Math.Abs(d.X) < e && Math.Abs(d.Y) < e;
		}

		public float2 Sign() { return new float2(Math.Sign(X), Math.Sign(Y)); }
		public static float Dot(float2 a, float2 b) { return a.X * b.X + a.Y * b.Y; }
		public float2 Round() { return new float2((float)Math.Round(X), (float)Math.Round(Y)); }

		public override string ToString() { return string.Format("({0},{1})", X, Y); }
	}
}
