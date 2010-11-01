﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats.Graphics;

namespace OpenRA.Renderer.Null
{
	class NullVertexBuffer<T> : IVertexBuffer<T>
	{
		public void Bind() { }
		public void SetData(T[] vertices, int length) { }
	}
}
