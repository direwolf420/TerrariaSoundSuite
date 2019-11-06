using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;
using Terraria.UI.Chat;

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
            volumeScale = Math.Min(volumeScale, CustomSoundValue.MAX_VOLUME); //Errors happen if it's above limit

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
