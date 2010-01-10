﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenRa.FileFormats
{
	public class MiniYaml
	{
		public string Value;
		public Dictionary<string, MiniYaml> Nodes = new Dictionary<string,MiniYaml>();

		public MiniYaml( string value ) : this( value, new Dictionary<string, MiniYaml>() ) { }

		public MiniYaml( string value, Dictionary<string, MiniYaml> nodes )
		{
			Value = value;
			Nodes = nodes;
		}

		public static Dictionary<string, MiniYaml> FromFile( string path )
		{
			var lines = File.ReadAllLines( path );

			var levels = new List<Dictionary<string, MiniYaml>>();
			levels.Add( new Dictionary<string, MiniYaml>() );

			foreach( var line in lines )
			{
				var t = line.TrimStart( ' ', '\t' );
				if( t.Length == 0 || t[ 0 ] == '#' )
					continue;
				var level = line.Length - t.Length;

				if( levels.Count <= level )
					throw new InvalidOperationException( "Bad indent in miniyaml" );
				while( levels.Count > level + 1 )
					levels.RemoveAt( levels.Count - 1 );

				var colon = t.IndexOf( ':' );
				var d = new Dictionary<string, MiniYaml>();

				if( colon == -1 )
					levels[ level ].Add( t.Trim(), new MiniYaml( null, d ) );
				else
				{
					var value = t.Substring( colon + 1 ).Trim();
					if( value.Length == 0 )
						value = null;
					levels[ level ].Add( t.Substring( 0, colon ).Trim(), new MiniYaml( value, d ) );
				}
				levels.Add( d );
			}
			return levels[ 0 ];
		}
	}
}
