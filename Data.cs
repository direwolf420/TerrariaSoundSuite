using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace TerrariaSoundSuite
{
	internal static class Data
	{
		internal static TerrariaSoundSuite Instance => ModContent.GetInstance<TerrariaSoundSuite>();

		internal static float MaxRangeSQ => Main.screenWidth * Main.screenWidth * 6.25f;
		internal static float MaxRange => Main.screenWidth * 2.5f;


		internal static List<DebugSound> playedSounds;

		internal static int hoverIndex = -1;

		//From reflection
		internal static Dictionary<SoundType, IDictionary<string, int>> sounds;

		internal static bool PlayingDebugSound => playingDebugCounter > 0;
		internal static readonly int playingDebugMax = 15;
		internal static int playingDebugCounter = 0;
		internal static int playingDebugIndex = -1;
		internal static bool playingDebugStart = false;

		internal static readonly int enqueueTimerMax = 15;
		internal static int enqueueTimer = 0;

		internal static readonly int hoverTimeMax = 15;

		/// <summary>
		/// Because sounds that are ambient use ambientVolume to scale volume instead, but for replacement purposes we want to use soundVolume
		/// </summary>
		internal static bool revertVolumeSwap = false;
		internal static float oldAmbientVolume = 0f;

		internal static CustomSoundValue defaultSoundValue;

		internal static void Load()
		{
			playedSounds = new List<DebugSound>();
			defaultSoundValue = new CustomSoundValue();
		}

		internal static void Unload()
		{
			playedSounds = null;
			defaultSoundValue = null;
		}
	}
}
