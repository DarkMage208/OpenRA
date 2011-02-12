﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.RA.Crates
{
	class CloakCrateActionInfo : CrateActionInfo
	{
		public readonly float InitialDelay = .4f;
		public readonly float CloakDelay = 1.2f;
		public readonly string CloakSound = "subshow1.aud";
		public readonly string UncloakSound = "subshow1.aud";

		public override object Create(ActorInitializer init) { return new CloakCrateAction(init.self, this); }
	}

	class CloakCrateAction : CrateAction
	{
		CloakCrateActionInfo Info;
		public CloakCrateAction(Actor self, CloakCrateActionInfo info)
			: base(self, info) { Info = info; }

		public override int GetSelectionShares(Actor collector)
		{
			return collector.HasTrait<Cloak>() 
				? 0 : base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			var cloakInfo = new CloakInfo(Info.InitialDelay, Info.CloakDelay, 
				Info.CloakSound, Info.UncloakSound);

			var cloak = cloakInfo.Create(new ActorInitializer(collector, new TypeDictionary() ));

			collector.World.AddFrameEndTask(w =>
				{
					w.Remove(collector);
					
					collector.AddTrait(cloak);
					if (collector.HasTrait<TargetableUnit<TargetableUnitInfo>>())
						collector.Trait<TargetableUnit<TargetableUnitInfo>>().RecievedCloak(collector);
					
					w.Add(collector);
				});

			base.Activate(collector);
		}
	}
}
