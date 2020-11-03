using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{
	internal static class Meth
	{
		private static void StartCountdownDebug() => Main.OnTick += CountdownDebug;

		private static void CountdownDebug()
		{
			if (Data.playingDebugCounter > 0) Data.playingDebugCounter--;
			else if (Data.playingDebugCounter <= 0)
			{
				Data.playingDebugIndex = -1;
				Main.OnTick -= CountdownDebug;
			}
		}

		private static void StartCountdownEnqueue() => Data.enqueueTimer = Data.enqueueTimerMax;

		internal static void CountdownEnqueue()
		{
			if (Data.enqueueTimer > 0) Data.enqueueTimer--;
		}

		internal static void ClearSounds() => Data.playedSounds.Clear();

		internal static void AmbiguityMessage()
		{
			if (ModLoader.GetMod("AnnoyingSoundReplacer") != null)
			{
				Main.NewText("It seems you have 'AnnoyingSoundReplacer' aswell as 'Terraria Sound Suite' enabled. The former is now outdated, you should disable it", Color.Orange);
			}
		}

		internal static void Log(object message) => Data.Instance.Logger.Info(message);

		internal static void SetMessage(string text, Color color) => Reflections.SetMessage(text, color);

		internal static void ModifySound(CustomSoundValue custom, ref int type, ref int Style, ref float volumeScale, ref float pitchOffset, ref DebugSound debug)
		{
			type = (int)custom.Type;
			volumeScale *= custom.Volume;

			volumeScale = FixVolume(type, volumeScale);

			pitchOffset = custom.Pitch;
			DebugSound old = debug;
			debug = new DebugSound(custom, debug)
			{
				IsReplacing = old
			};

			Style = FixStyle(custom);

			AmbientSwap(type);
		}

		internal static void AddDebugSound(DebugSound sound)
		{
			bool anywhere = sound.X == -1 || sound.Y == -1;
			if (anywhere || Main.LocalPlayer.DistanceSQ(sound.worldPos) < Data.MaxRangeSQ)
			{
				if (anywhere)
				{
					sound.worldPos = Main.LocalPlayer.Center;
				}
				if (Data.playedSounds.Count >= Config.Instance.Debug.TrackedSoundsCount)
				{
					int index = Data.playedSounds.FindIndex(s => !s.Tracked);
					if (index != -1) Data.playedSounds.RemoveAt(index);
				}
				StartCountdownEnqueue();
				Data.playedSounds.Add(sound);
			}
		}

		/// <summary>
		/// Because sounds that are ambient use this to scale volume instead
		/// </summary>
		internal static void AmbientSwap(int type)
		{
			if ((type >= 30 && type <= 35) || type == 39)
			{
				Data.oldAmbientVolume = Main.ambientVolume;
				Main.ambientVolume = Main.soundVolume;
				Data.revertVolumeSwap = true;
			}
		}

		internal static float FixVolume(int type, float volumeScale)
		{
			volumeScale = Math.Min(volumeScale, CustomSoundValue.MAX_VOLUME);
			//Cap for volume because vanilla is retarded
			if (Main.soundVolume * volumeScale > 1f)
			{
				volumeScale = Main.soundVolume / volumeScale;
			}
			return volumeScale;
		}

		internal static void RevertAmbientSwap()
		{
			if (Data.revertVolumeSwap)
			{
				Data.revertVolumeSwap = false;
				Main.ambientVolume = Data.oldAmbientVolume;
			}
		}

		internal static bool PlayDebugSound(int type, int x = -1, int y = -1, int Style = 1, float volumeScale = 1, float pitchOffset = 0, int index = -1)
		{
			if (!Data.PlayingDebugSound)
			{
				Data.playingDebugStart = true;
				Data.playingDebugCounter = Data.playingDebugMax;
				StartCountdownDebug();
				Data.playingDebugIndex = index;
				volumeScale = FixVolume(type, volumeScale);
				Style = FixStyle(type, Style);
				AmbientSwap(type);
				Main.PlaySound(type, x, y, Style, volumeScale, pitchOffset);
				return true;
			}
			else
			{
				return false;
			}
		}

		internal static void AssignSoundFromPageIfMatch<T>(DictPage<T> page, DebugSound debug, ref CustomSoundValue custom) where T : EntityDefinition
		{
			if (custom != null) return;

			if (page.Active && debug.origin.Valid(page.AssociatedSoundType))
			{
				T nothing = Activator.CreateInstance<T>();
				T item = (T)Activator.CreateInstance(typeof(T), new object[] { debug.origin.ThingID });
				//A set rule takes priority over the nothing rule
				if (page.Rule.ContainsKey(item))
				{
					custom = page.Rule[item];
				}
				else if (page.Rule.ContainsKey(nothing))
				{
					custom = page.Rule[nothing];
				}
			}
		}

		/// <summary>
		/// Returns true if the constraint is satisfied, or none is specified (or the game is in the menu)
		/// </summary>
		internal static bool SatisfiesConstraint(CustomSoundValue custom)
		{
			if (Main.gameMenu) return true;
			int type = custom.HeldItemConstraint.Type;
			if (type == 0 || type == ItemID.Count) return true;
			return Main.LocalPlayer.HeldItem.type == type;
		}

		#region Red is an ass and we won't be working with him again
		internal static int FixStyle(int type, int style)
		{
			if (type == (int)SoundTypeEnum.ZombieMoan)
			{
				ValidStyles styles = CustomSound.GetValidStyles((SoundTypeEnum)type);
				if (style == styles.others[0]) return NPCID.BloodZombie;
				else if (style == styles.others[1]) return NPCID.SandShark;
			}
			return style;
		}

		internal static int FixStyle(CustomSoundValue custom) => FixStyle((int)custom.Type, custom.Style);

		internal static int RevertFixStyle(int type, int style)
		{
			if (type == (int)SoundTypeEnum.ZombieMoan)
			{
				ValidStyles styles = CustomSound.GetValidStyles((SoundTypeEnum)type);
				if (style == NPCID.BloodZombie) style = styles.others[0];
				else if (style == NPCID.SandShark) style = styles.others[1];
			}
			return style;
		}

		internal static int RevertFixStyle(CustomSoundValue custom) => RevertFixStyle((int)custom.Type, custom.Style);
		#endregion
	}
}
