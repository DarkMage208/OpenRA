﻿using System;
using System.Linq;
using System.Reflection;

namespace OpenRa.FileFormats
{
	public static class FieldLoader
	{
		public static void Load(object self, IniSection ini)
		{
			foreach (var x in ini)
			{
				var field = self.GetType().GetField(x.Key.Trim());
				if( field != null )
					field.SetValue(self, GetValue(field.FieldType, x.Value.Trim()));
			}
		}

		public static void Load(object self, MiniYaml my)
		{
			foreach (var x in my.Nodes)
			{
				var field = self.GetType().GetField(x.Key.Trim());
				if (field == null)
					throw new NotImplementedException("Missing field `{0}` on `{1}`".F(x.Key.Trim(), self.GetType().Name));
				field.SetValue(self, GetValue(field.FieldType, x.Value.Value));
			}
		}

		static object GetValue( Type fieldType, string x )
		{
			if (x != null) x = x.Trim();
			if( fieldType == typeof( int ) )
				return int.Parse( x );

			else if (fieldType == typeof(float))
				return float.Parse(x.Replace("%","")) * (x.Contains( '%' ) ? 0.01f : 1f);

			else if (fieldType == typeof(string))
				return x;

			else if (fieldType.IsEnum)
				return Enum.Parse(fieldType, x, true);

			else if (fieldType == typeof(bool))
				return ParseYesNo(x);

			else if (fieldType.IsArray)
			{
				if (x == null)
					return Array.CreateInstance(fieldType.GetElementType(), 0);

				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

				var ret = Array.CreateInstance(fieldType.GetElementType(), parts.Length);
				for (int i = 0; i < parts.Length; i++)
					ret.SetValue(GetValue(fieldType.GetElementType(), parts[i].Trim()), i);
				return ret;
			}
			else if (fieldType == typeof(int2))
			{
				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new int2(int.Parse(parts[0]), int.Parse(parts[1]));
			}
			else
				throw new InvalidOperationException("FieldLoader: don't know how to load field of type " + fieldType.ToString());
		}

		static bool ParseYesNo( string p )
		{
			p = p.ToLowerInvariant();
			if( p == "yes" ) return true;
			if( p == "true" ) return true;
			if( p == "no" ) return false;
			if( p == "false" ) return false;
			throw new InvalidOperationException();
		}
	}

	public static class FieldSaver
	{
		public static MiniYaml Save(object o)
		{
			return new MiniYaml(null, o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
				.ToDictionary(
					f => f.Name,
					f => new MiniYaml(FormatValue(o, f))));
		}

		static string FormatValue(object o, FieldInfo f)
		{
			var v = f.GetValue(o);
			return f.FieldType.IsArray
				? string.Join(",", ((Array)v).OfType<object>().Select(a => a.ToString()).ToArray())
				: v.ToString();
		}
	}
}
