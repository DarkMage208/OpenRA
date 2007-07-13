using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class Mcv : Actor
	{
		public Mcv( PointF location )
		{
			this.location = location;
		}

		int GetFacing()
		{
			int x = (Environment.TickCount >> 6) % 64;

			return x < 32 ? x : 63 - x;
		}

		public override SheetRectangle<Sheet>[] CurrentImages
		{
			get { return new SheetRectangle<Sheet>[] { UnitSheetBuilder.McvSheet[ GetFacing() ] }; }
		}
	}
}
