#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Traits
{
	class ChoosePaletteOnSelectInfo : TraitInfo<ChoosePaletteOnSelect> { }

	class ChoosePaletteOnSelect : INotifySelection
	{
		public void SelectionChanged()
		{
			var firstItem = Game.controller.selection.Actors.FirstOrDefault(
				a => a.World.LocalPlayer == a.Owner && a.traits.Contains<Production>());

			if (firstItem == null)
				return;

			var produces = firstItem.Info.Traits.Get<ProductionInfo>().Produces.FirstOrDefault();
			if (produces == null)
				return;

			Chrome.rootWidget.GetWidget<BuildPaletteWidget>("INGAME_BUILD_PALETTE")
				.SetCurrentTab(produces);
		}
	}
}
