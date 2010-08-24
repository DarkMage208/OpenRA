#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SpawnMapActorsInfo : TraitInfo<SpawnMapActors> { }

	public class SpawnMapActors : IGameStarted
	{
		public Dictionary<string, Actor> Actors = new Dictionary<string, Actor>();

		public void GameStarted(World world)
		{
			foreach( var actorReference in world.Map.Actors )
			{
				var initDict = actorReference.Value.InitDict;
				initDict.Add( new SkipMakeAnimsInit() );
				Actors[ actorReference.Key ] = world.CreateActor( actorReference.Value.Type, initDict );
			}
		}
	}

	public class SkipMakeAnimsInit : IActorInit {}
}
