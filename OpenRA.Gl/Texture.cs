﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;
using System.IO;
using System;

namespace OpenRA.GlRenderer
{
	public class Texture : ITexture
	{
		internal int texture;

		public Texture(GraphicsDevice dev, Bitmap bitmap)
		{
			Gl.glGenTextures(1, out texture);
			GraphicsDevice.CheckGlError();
			SetData(bitmap);
		}

		// An array of Color.ToARGB()'s
		public void SetData(int[,] colors)
		{
			int width = colors.GetUpperBound(0) + 1;
			int height = colors.GetUpperBound(1) + 1;
			
			if (!IsPowerOf2(width) || !IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width,height));
			
			IntPtr intPtr;
			unsafe
			{
				fixed (int* ptr = colors)
					intPtr = new IntPtr((void *) ptr);
			}

			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
			GraphicsDevice.CheckGlError();
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_BASE_LEVEL, 0);
			GraphicsDevice.CheckGlError();
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_LEVEL, 0);
			GraphicsDevice.CheckGlError();
			Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, width, height,
				0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, intPtr);
			GraphicsDevice.CheckGlError();
		}
		
		public void SetData(Bitmap bitmap)
		{
			if (!IsPowerOf2(bitmap.Width) || !IsPowerOf2(bitmap.Height))
			{
				//throw new InvalidOperationException( "non-power-of-2-texture" );
				bitmap = new Bitmap(bitmap, new Size(NextPowerOf2(bitmap.Width), NextPowerOf2(bitmap.Height)));
			}

			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
			GraphicsDevice.CheckGlError();

			var bits = bitmap.LockBits(
				new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppArgb);

			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_BASE_LEVEL, 0);
			GraphicsDevice.CheckGlError();
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_LEVEL, 0);
			GraphicsDevice.CheckGlError();
			Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, bits.Width, bits.Height,
				0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bits.Scan0);        // todo: weird strides
			GraphicsDevice.CheckGlError();

			bitmap.UnlockBits(bits);
		}

		bool IsPowerOf2(int v)
		{
			return (v & (v - 1)) == 0;
		}

		int NextPowerOf2(int v)
		{
			--v;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			++v;
			return v;
		}
	}
}
