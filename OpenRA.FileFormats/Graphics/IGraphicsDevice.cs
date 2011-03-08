﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.IO;

namespace OpenRA.FileFormats.Graphics
{
	[AttributeUsage( AttributeTargets.Assembly )]
	public class RendererAttribute : Attribute
	{
		public readonly Type Type;

		public RendererAttribute( Type graphicsDeviceType )
		{
			if( !typeof( IGraphicsDevice ).IsAssignableFrom( graphicsDeviceType ) )
				throw new InvalidOperationException( "Incorrect type in RendererAttribute" );
			Type = graphicsDeviceType;
		}
	}

	public interface IGraphicsDevice
	{
		IVertexBuffer<Vertex> CreateVertexBuffer( int length );
		IIndexBuffer CreateIndexBuffer( int length );
		ITexture CreateTexture( Bitmap bitmap );
		ITexture CreateTexture();
		IShader CreateShader( string name );

		Size WindowSize { get; }
		int GpuMemoryUsed { get; }

		void Clear( Color color );
		void Present( IInputHandler inputHandler );

		void DrawIndexedPrimitives( PrimitiveType type, Range<int> vertexRange, Range<int> indexRange );
		void DrawIndexedPrimitives( PrimitiveType type, int vertexPool, int numPrimitives );

		void EnableScissor( int left, int top, int width, int height );
		void DisableScissor();
	}

	public interface IVertexBuffer<T>
	{
		void Bind();
		void SetData( T[] vertices, int length );
	}

	public interface IIndexBuffer
	{
		void Bind();
		void SetData( uint[] indices, int length );
	}

	public interface IShader
	{
		void SetValue( string name, float x, float y );
		void SetValue( string param, ITexture texture );
		void Commit();
		void Render( Action a );
	}

	public interface ITexture
	{
		void SetData(Bitmap bitmap);
		void SetData(uint[,] colors);
		void SetData(byte[] colors, int width, int height);
	}

    public enum PrimitiveType
    {
        PointList, 
        LineList, 
        TriangleList,
    }

	public struct Range<T>
	{
		public readonly T Start, End;
		public Range( T start, T end ) { Start = start; End = end; }
	}
	
	public enum WindowMode
	{
		Windowed,
		Fullscreen,
		PseudoFullscreen,
	}
}
