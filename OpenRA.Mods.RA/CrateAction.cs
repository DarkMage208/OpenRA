#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CrateActionInfo : ITraitInfo
	{
		public int SelectionShares = 10;
		public string Effect = null;
		public string Notification = null;

		public virtual object Create(ActorInitializer init) { return new CrateAction(init.self, this); }
	}

	public class CrateAction
	{
		public Actor self;
		public CrateActionInfo info;
		
		public CrateAction(Actor self, CrateActionInfo info)
		{
			this.self = self;
			this.info = info;
		}
		
		public virtual int GetSelectionShares(Actor collector)
		{
			return info.SelectionShares;
		}
		
		public virtual void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, info.Notification);
			
			collector.World.AddFrameEndTask(w => 
			{
				if (info.Effect != null)
					w.Add(new CrateEffect(collector, info.Effect));
			});
		}
	}
}
