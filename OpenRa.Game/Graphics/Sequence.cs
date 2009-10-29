using System.Xml;
using Ijw.DirectX;

namespace OpenRa.Game.Graphics
{
	class Sequence
	{
		readonly int start, length;

		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }

		public Sequence(string unit, XmlElement e)
		{
			string srcOverride = e.GetAttribute("src");

			Range<int> src = UnitSheetBuilder.GetUnit(
				string.IsNullOrEmpty(srcOverride) ? unit : srcOverride);

			start = src.Start + int.Parse(e.GetAttribute("start"));

			if (e.GetAttribute("length") == "*" || e.GetAttribute("end") == "*")
				length = src.End - src.Start + 1;
			else if (e.HasAttribute("length"))
				length = int.Parse(e.GetAttribute("length"));
			else if (e.HasAttribute("end"))
				length = int.Parse(e.GetAttribute("end")) - int.Parse(e.GetAttribute("start"));
			else
				length = 1;
		}

		public Sprite GetSprite(int frame)
		{
			return UnitSheetBuilder.sprites[ ( frame % length ) + start ];
		}
	}
}
