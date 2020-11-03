using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{
	[SeparatePage]
	public class DebugPage
	{
		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			//Auto clamp because property
			TrackedSoundsCount = TrackedSoundsCount;
			if (!Enum.IsDefined(DebugMode.GetType(), DebugMode))
			{
				DebugMode = DebugMode.Inspect; //Default fallback
			}
		}

		[DefaultValue(false)]
		[Tooltip("Toggle debugging on/off")]
		public bool Active { get; set; }

		[DefaultValue(false)]
		[Tooltip("Toggle verbose information on/off")]
		[Label("Additional Info")]
		public bool Verbose { get; set; }

		[DefaultValue(DebugMode.Inspect)]
		[Tooltip("Toggle between inspect and deaf mode")]
		[Label("Debug Mode")]
		[DrawTicks]
		public DebugMode DebugMode { get; set; }

		internal const int MIN_TRACKED = 1;
		internal const int MAX_TRACKED = 20;

		//THIS IS 0 IF I DON'T ASSIGN 10 TO IT
		private int _TrackedSoundsCount;

		[DefaultValue(10)]
		[Range(MIN_TRACKED, MAX_TRACKED)]
		[Slider]
		[Tooltip("Change the number of sounds that will be recorded")]
		[Label("Tracked Sound Count")]
		public int TrackedSoundsCount
		{
			get
			{
				return _TrackedSoundsCount;
			}
			set
			{
				_TrackedSoundsCount = Utils.Clamp(value, MIN_TRACKED, MAX_TRACKED);
				if (TerrariaSoundSuite.loaded)
				{
					int oldCount = Data.playedSounds.Count;
					if (oldCount > _TrackedSoundsCount)
					{
						Data.playedSounds.RemoveRange(0, oldCount - _TrackedSoundsCount);
					}
				}
			}
		}

		[Tooltip("Sounds that won't show up on the debug feed")]
		public List<CustomSound> Blacklist { get; set; } = new List<CustomSound>();


		[Header(
			   "****DEBUG USAGE MANUAL****" + "\n" +
			   "Sounds are defined by a type and a style." + "\n" +
			   "If you want to use this mod to its fullest capabilities, enable debugging below." + "\n" +
			   "As sounds play, they will appear on the left in a list." + "\n" +
			   "Left click on the number on the left to 'favorite' it." + "\n" +
			   "Right click on the number on the left to replay the sound." + "\n" +
			   "Favorited sounds don't vanish from the list, the limit can be increased." + "\n" +
			   "'Deaf' debug mode will show sounds ingame as an overlay, mouseover to 'see' the sound."
			   )]
		[Label("Dummy variable for manual")]
		[JsonIgnore]
		public bool Useless => true;

		public override string ToString()
		{
			return $"{nameof(Active)}: {(Active ? "Yes" : "No")}, Mode: {DebugMode}" + (Verbose ? " -V" : "");
		}

		public override bool Equals(object obj)
		{
			if (obj is DebugPage other)
			{
				return Active == other.Active && DebugMode == other.DebugMode &&
					TrackedSoundsCount == other.TrackedSoundsCount && Blacklist.Equals(other.Blacklist) && Verbose == other.Verbose;
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return new { Active, DebugMode, TrackedSoundsCount, Blacklist, Verbose }.GetHashCode();
		}

		public bool Contains(DebugSound debug)
		{
			return Contains(debug.ToCustomSound());
		}

		public bool Contains(CustomSound custom)
		{
			return Blacklist.Contains(custom);
		}

		public bool Contains(int type, int style)
		{
			return Blacklist.Contains(new CustomSound((SoundTypeEnum)type, style));
		}
	}

	public enum DebugMode : int
	{
		Inspect = 0,
		Deaf = 1
	}
}
