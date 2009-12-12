using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenRa.FileFormats;
using System;

namespace OpenRa.Game.Graphics
{
	static class SequenceProvider
	{
		static Dictionary<string, Dictionary<string, Sequence>> units =
			new Dictionary<string, Dictionary<string, Sequence>>();

		static Dictionary<string, CursorSequence> cursors = new Dictionary<string, CursorSequence>();

		public static void Initialize( bool useAftermath )
		{
			LoadSequenceSource("sequences.xml");
			if (useAftermath)
				LoadSequenceSource("sequences-aftermath.xml");
		}

		static void LoadSequenceSource(string filename)
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open(filename));

			foreach (XmlElement eUnit in document.SelectNodes("/sequences/unit"))
				LoadSequencesForUnit(eUnit);

			foreach (XmlElement eCursor in document.SelectNodes("/sequences/cursor"))
				LoadSequencesForCursor(eCursor);
		}

		static void LoadSequencesForCursor(XmlElement eCursor)
		{
			string cursorSrc = eCursor.GetAttribute("src");

			foreach (XmlElement eSequence in eCursor.SelectNodes("./sequence"))
				cursors.Add(eSequence.GetAttribute("name"), new CursorSequence(cursorSrc, eSequence));

			Log.Write("* LoadSequencesForCursor() done");
		}

		public static void ForcePrecache() { }	// force static ctor to run

		static void LoadSequencesForUnit(XmlElement eUnit)
		{
			string unitName = eUnit.GetAttribute("name");

			var sequences = eUnit.SelectNodes("./sequence").OfType<XmlElement>()
				.Select(e => new Sequence(unitName, e))
				.ToDictionary(s => s.Name);

			units.Add(unitName, sequences);
		}

		public static Sequence GetSequence(string unitName, string sequenceName)
		{
			try { return units[unitName][sequenceName]; }
			catch (KeyNotFoundException e)
			{
				throw new InvalidOperationException(
					"Unit `{0}` does not have a sequence `{1}`".F(unitName, sequenceName));
			}
		}

		public static CursorSequence GetCursorSequence(string cursor)
		{
			return cursors[cursor];
		}
	}
}
