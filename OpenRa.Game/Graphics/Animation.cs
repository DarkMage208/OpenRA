using System;

namespace OpenRa.Game.Graphics
{
	class Animation
	{
		readonly string name;
		Sequence currentSequence;
		int frame = 0;
		bool tickAlways;

		public Animation( string name )
		{
			this.name = name;
			Play( "idle" );
		}

		public Sprite[] Images { get { return new Sprite[] { currentSequence.GetSprite( frame ) }; } }
		public float2 Center { get { return 0.25f * new float2(currentSequence.GetSprite(0).bounds.Size); } }

		public void Play( string sequenceName )
		{
			PlayThen(sequenceName, () => { });
		}

		public void PlayRepeating( string sequenceName )
		{
			PlayThen( sequenceName, () => PlayRepeating( sequenceName ) );
		}

		public void PlayThen( string sequenceName, Action after )
		{
			tickAlways = false;
			currentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if( frame >= currentSequence.Length )
				{
					frame = currentSequence.Length - 1;
					tickFunc = () => { };
					after();
				}
			};
		}

		public void PlayFetchIndex( string sequenceName, Func<int> func )
		{
			tickAlways = true;
			currentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = func();
			tickFunc = () => frame = func();
		}

		int timeUntilNextFrame;
		Action tickFunc;

		public void Tick( int t )
		{
			if( tickAlways )
				tickFunc();
			else
			{
				timeUntilNextFrame -= t;
				while( timeUntilNextFrame <= 0 )
				{
					tickFunc();
					timeUntilNextFrame += 40; // 25 fps == 40 ms
				}
			}
		}
	}
}
