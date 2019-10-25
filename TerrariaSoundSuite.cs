using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;
using Terraria.UI.Chat;

namespace TerrariaSoundSuite
{
    public class TerrariaSoundSuite : Mod
    {
        internal static TerrariaSoundSuite Instance => ModContent.GetInstance<TerrariaSoundSuite>();

        internal static float MaxRangeSQ => Main.screenWidth * Main.screenWidth * 6.25f;
        internal static float MaxRange => Main.screenWidth * 2.5f;

        internal static List<DebugSound> playedSounds;

        internal static int hoverIndex = -1;

        internal static bool loaded = false;

        //From reflection
        internal static Dictionary<SoundType, IDictionary<string, int>> sounds;

        //From reflection
        internal static object modConfig;
        internal static Type UIModConfigType;
        internal static MethodInfo setMessageMethod;

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

        public TerrariaSoundSuite()
        {

        }

        public override void Load()
        {
            On.Terraria.Main.PlaySound_int_int_int_int_float_float += HookPlaySound;
            playedSounds = new List<DebugSound>();
            defaultSoundValue = new CustomSoundValue();
            ReflectSound();
            ReflectConfig();
            loaded = true;
        }

        internal static void ReflectSound()
        {
            if (sounds == null)
            {
                FieldInfo soundsField = typeof(SoundLoader).GetField("sounds", BindingFlags.Static | BindingFlags.NonPublic);
                sounds = (Dictionary<SoundType, IDictionary<string, int>>)soundsField.GetValue(null);
                if (sounds == null) throw new Exception("Reflection failed at getting the sound dictionary, report in the homepage of the mod!");
            }
        }

        internal static void ReflectConfig()
        {
            if (setMessageMethod == null)
            {
                try
                {
                    //Interface.modConfig.SetMessage("Error: " + e.Message, Color.Red);
                    Assembly ModLoaderAssembly = typeof(ModLoader).Assembly;
                    Type Interface = ModLoaderAssembly.GetType("Terraria.ModLoader.UI.Interface");
                    FieldInfo modConfigField = Interface.GetField("modConfig", BindingFlags.Static | BindingFlags.NonPublic);
                    modConfig = modConfigField.GetValue(null);

                    UIModConfigType = ModLoaderAssembly.GetType("Terraria.ModLoader.Config.UI.UIModConfig");
                    setMessageMethod = UIModConfigType.GetMethod("SetMessage", new Type[] { typeof(string), typeof(Color) });
                    if (setMessageMethod == null) throw new NullReferenceException("setMessageMethod is null");

                    Type type = typeof(UIElement);
                    isInitializedField = type.GetField("_isInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
                    IsMouseHoveringField = type.GetField("_isMouseHovering", BindingFlags.Instance | BindingFlags.NonPublic);
                    modField = UIModConfigType.GetField("mod", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                catch (Exception e)
                {
                    Instance.Logger.Info("Failed to reflect SetMessage: " + e);
                }
            }
        }

        internal static FieldInfo isInitializedField;
        internal static FieldInfo IsMouseHoveringField;
        internal static FieldInfo modField;

        internal static bool IsInitialized => (bool)isInitializedField.GetValue(modConfig);
        internal static bool IsMouseHovering => (bool)IsMouseHoveringField.GetValue(modConfig);
        internal static bool IsCurrentMod => ((Mod)modField.GetValue(modConfig)).Name == Instance.Name;

        /// <summary>
        /// Accesses the ModConfig message UI text box to push messages
        /// </summary>
        internal static void SetMessage(string text, Color color)
        {
            if (loaded && setMessageMethod != null)
            {
                try
                {
                    //Order is important
                    if (IsInitialized && IsCurrentMod && IsMouseHovering)
                    {
                        setMessageMethod.Invoke(modConfig, new object[] { text, color });
                    }
                }
                catch (Exception e)
                {
                    Instance.Logger.Info("Failed to reflect UIModConfig.mod: " + e);
                }
            }
        }

        public override void Unload()
        {
            playedSounds = null;
            UIModConfigType = null;
            setMessageMethod = null;
            defaultSoundValue = null;
            loaded = false;
        }

        public override void PreSaveAndQuit()
        {
            playedSounds.Clear();
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.gameMenu || Main.gamePaused || !Config.Instance.Debug.Active) return;
            int InventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (InventoryIndex != -1)
            {
                if (Config.Instance.Debug.DebugMode == DebugMode.Inspect)
                {
                    layers.Insert(InventoryIndex, new LegacyGameInterfaceLayer(
                        $"{Name}: {nameof(Arrow)}",
                        Arrow,
                        InterfaceScaleType.Game
                    ));
                    layers.Insert(InventoryIndex + 2, new LegacyGameInterfaceLayer(
                        $"{Name}: {nameof(Debug)}",
                        Debug,
                        InterfaceScaleType.UI
                    ));
                }
                else
                {
                    layers.Insert(InventoryIndex, new LegacyGameInterfaceLayer(
                        $"{Name}: {nameof(DeafText)}",
                        DeafText,
                        InterfaceScaleType.Game
                    ));
                }
            }
        }

        private static readonly GameInterfaceDrawMethod DeafText = delegate
        {
            int hoverIndex = -1;
            string text = "";
            Color color = Color.White;
            float fade = 0f;
            DebugSound sound;
            List<DebugSound> sounds = new List<DebugSound>();
            for (int i = 0; i < playedSounds.Count; i++)
            {
                sound = playedSounds[i];
                sounds.Add(sound);
                int size = 24;
                if (Utils.CenteredRectangle(sound.worldPos, new Vector2(size)).Contains(new Point((int)Main.MouseWorld.X, (int)Main.MouseWorld.Y)) && sound.Discovered)
                {
                    hoverIndex = i;
                }
            }
            if (hoverIndex != -1)
            {
                sounds.RemoveAt(hoverIndex);
            }

            Vector2 offset = - new Vector2(18 >> 1) - Main.screenPosition;

            for (int i = 0; i < sounds.Count; i++)
            {
                sound = sounds[i];
                float length = Main.LocalPlayer.DistanceSQ(sound.worldPos);
                if (length < 10000f)
                {
                    fade = length / 10000f;
                }
                else
                {
                    fade = 1f;
                }
                sound.HoverTime--;
                string type = sound.Discovered ? sound.typeName : "...";
                text = "(x) " + type;
                fade *= ((float)sound.HoverTime / hoverTimeMax) * 0.3f + 0.7f;
                color = Color.White * fade;
                DrawDebugText(text, sound.worldPos + offset, color, 1f, true);
            }

            if (hoverIndex != -1)
            {
                sound = playedSounds[hoverIndex];
                sound.HoverTime += 2;
                text = "(x) " + sound.typeName + " (" + sound.origin + ")";
                fade = ((float)sound.HoverTime / hoverTimeMax) * 0.3f + 0.7f;
                color = Color.White * fade;
                DrawDebugText(text, sound.worldPos + offset, color, 1f, true);

            }
            return true;
        };

        private static readonly GameInterfaceDrawMethod Debug = delegate
        {
            //Credit to jopojelly (heavily adjusted from SummonersAssociation)
            Player player = Main.LocalPlayer;
            int xPosition;
            int yPosition;
            Color color;
            int buffsPerLine = 11;
            int lineOffset = 0;
            int size = 24;
            for (int b = buffsPerLine; b < player.buffType.Length; ++b)
            {
                if (player.buffType[b] > 0)
                {
                    lineOffset = b / buffsPerLine;
                }
            }
            xPosition = 32;
            yPosition = 76 + 40 + lineOffset * 50 + Main.buffTexture[1].Height;
            if (Main.playerInventory)
            {
                //So it doesn't overlap with inventory and recipe UI
                xPosition += 2 * 47;
                yPosition = 76 + 40 + 4 * 47;
            }

            float fade = enqueueTimer / (float)enqueueTimerMax;
            string text = "[" + "DEBUG" + "] Last Played Sounds:";
            if (playedSounds.Count == 0) text += " None";
            color = FadeBetween(Color.White, Color.Green, fade);
            DrawDebugText(text, new Vector2(xPosition, yPosition), color);
            yPosition += size;

            bool newMouseInterface = false;

            int addedSoundIndex = -1;

            for (int i = 0; i < playedSounds.Count; i++)
            {
                DebugSound sound = playedSounds[i];
                float distance = player.DistanceSQ(sound.worldPos);
                distance = distance > MaxRangeSQ ? MaxRangeSQ : distance;
                float ratio = 1 - distance / MaxRangeSQ;
                ratio = ratio * 0.6f + 0.4f;
                //from 0.4f to 1f
                color = sound.color;
                if (playingDebugIndex == i)
                {
                    fade = playingDebugCounter / (float)playingDebugMax;
                    color = FadeBetween(color, Color.Transparent, fade);
                }
                color *= ratio;
                text = "[" + i + "] : " + sound.ToString();
                if (Config.Instance.Debug.Verbose)
                {
                    text += " |";
                    if (sound.stacktrace != string.Empty) text += " " + sound.stacktrace;
                    if (sound.IsReplacing != null) text += " " + sound.IsReplacing.ToString();
                }
                Vector2 pos = new Vector2(xPosition, yPosition + i * size);
                //The text on the left side of the screen
                DrawDebugText(text, pos, color);

                if (Utils.CenteredRectangle(pos + new Vector2(size >> 1), new Vector2(size)).Contains(new Point(Main.mouseX, Main.mouseY)))
                {
                    if (!player.mouseInterface)
                    {
                        if (Main.mouseRight && Main.mouseRightRelease)
                        {
                            PlayDebugSound(sound.type, -1, -1, sound.Style, sound.volumeScale, sound.pitchOffset, i);
                            //playingDebugStart = true;
                            //playingDebugIndex = i;
                            //int x = -1;
                            //int y = -1;
                            ////if (sound.type == (int)SoundTypeEnum.Waterfall || sound.type == (int)SoundTypeEnum.Lavafall)
                            ////{
                            ////    x = (int)Main.LocalPlayer.Center.X;
                            ////    y = (int)Main.LocalPlayer.Center.Y;
                            ////}
                            //Main.PlaySound(sound.type, x, y, sound.Style, sound.volumeScale, sound.pitchOffset);
                        }
                        else if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            if (!sound.Tracked)
                            {
                                //always space for one
                                if (playedSounds.Count(s => s.Tracked) < Config.Instance.Debug.TrackedSoundsCount - 1)
                                {
                                    sound.color = Main.DiscoColor;
                                    sound.Tracked = true;
                                    addedSoundIndex = i;
                                }
                            }
                            else
                            {
                                sound.color = Color.White;
                                sound.Tracked = false;
                            }
                        }
                    }
                    hoverIndex = i;
                    newMouseInterface = true;
                }
            }

            if (addedSoundIndex != -1)
            {
                //Reorder tracked sounds to the beginning
                DebugSound temp = playedSounds.ElementAt(addedSoundIndex);
                playedSounds.RemoveAt(addedSoundIndex);
                playedSounds.Insert(0, temp);
            }

            if (hoverIndex != -1 && !player.mouseInterface)
            {
                Vector2 vector = Main.ThickMouse ? new Vector2(16) : new Vector2(10);
                vector += Main.MouseScreen;
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, Main.fontMouseText, "Left click to " + (playedSounds[hoverIndex].Tracked ? "un" : "") + "track sound", vector, Color.White, 0f, Vector2.Zero, Vector2.One);
                vector.Y += 28;
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, Main.fontMouseText, "Right click to play sound", vector, Color.White, 0f, Vector2.Zero, Vector2.One);
            }
            if (newMouseInterface) player.mouseInterface = newMouseInterface;
            return true;
        };

        private static readonly GameInterfaceDrawMethod Arrow = delegate
        {
            //Credit to jopojelly (adjusted from Census)
            int size = 24;
            for (int i = 0; i < playedSounds.Count; i++)
            {
                DebugSound sound = playedSounds[i];
                if (i == hoverIndex || sound.Tracked)
                {
                    Color normal = sound.color;
                    Color clicked = Color.Transparent;

                    float fade = 0;
                    if (playingDebugIndex == i) fade = playingDebugCounter / (float)playingDebugMax;
                    Color colorFade = FadeBetween(normal, clicked, fade);
                    int index = i == hoverIndex ? hoverIndex : i;

                    //The position displayed at the sound origin
                    DrawDebugText("[" + index + "]", sound.worldPos - new Vector2(size >> 1) - Main.screenPosition, colorFade, 1.5f, true);

                    Vector2 playerCenter = Main.LocalPlayer.Center + new Vector2(0, Main.LocalPlayer.gfxOffY);
                    Vector2 between = sound.worldPos + new Vector2(size >> 2) - playerCenter;
                    float length = between.Length();
                    if (length > 40)
                    {
                        Vector2 offset = Vector2.Normalize(between) * Math.Min(70, length - 20);
                        float rotation = between.ToRotation() + 3 * MathHelper.PiOver4;
                        Vector2 drawPosition = playerCenter - Main.screenPosition + offset;
                        fade = Math.Min(1f, (length - 20) / 70) * (1 - fade);
                        //from here, length is used as an addidional fade for long distance
                        if (length > MaxRange)
                        {
                            length = Utils.Clamp(length, MaxRange, 8 * MaxRange);
                            length /= 10 * MaxRange;
                            length = 1f - length;
                        }
                        else
                        {
                            length = 1f;
                        }
                        colorFade = normal * fade * length;
                        Main.spriteBatch.Draw(Main.cursorTextures[0], drawPosition, null, Color.Black * fade * length, rotation, Main.cursorTextures[1].Size() / 2, new Vector2(2f), SpriteEffects.None, 0);
                        Main.spriteBatch.Draw(Main.cursorTextures[0], drawPosition, null, colorFade, rotation, Main.cursorTextures[1].Size() / 2, new Vector2(1.5f), SpriteEffects.None, 0);
                    }
                }
            }
            hoverIndex = -1;
            return true;
        };

        private static Color FadeBetween(Color c0, Color c1, float fade) => fade == 0f ? c0 : new Color(c0.ToVector4() * (1f - fade) + c1.ToVector4() * fade);

        private SoundEffectInstance HookPlaySound(On.Terraria.Main.orig_PlaySound_int_int_int_int_float_float orig, int type, int x, int y, int Style, float volumeScale, float pitchOffset)
        {
            if (playingDebugStart)
            {
                playingDebugCounter = playingDebugMax;
                playingDebugStart = false;
                AmbientSwap(type);
                return orig(type, x, y, Style, volumeScale, pitchOffset);
            }

            if (Main.gameMenu || type == SoundID.Waterfall || type == SoundID.Lavafall)
            {
                return orig(type, x, y, Style, volumeScale, pitchOffset);
            }

            DebugSound debug = new DebugSound(type, x, y, Style, volumeScale, pitchOffset);
            //Now the debug.Style is based on the constraints set by ValidStyles

            //Check if it matches any filters

            //TODO Can probably make this more compact
            CustomSoundValue custom = null;
            if (Config.Instance.Item.Active && debug.origin.Valid(SoundType.Item))
            {
                ItemDefinition nothing = new ItemDefinition(0);
                ItemDefinition item = new ItemDefinition(debug.origin.ThingID);
                //A set rule takes priority over the nothing rule
                if (Config.Instance.Item.Rule.ContainsKey(item))
                {
                    custom = Config.Instance.Item.Rule[item];
                }
                else if (Config.Instance.Item.Rule.ContainsKey(nothing))
                {
                    custom = Config.Instance.Item.Rule[nothing];
                }
            }
            else if (Config.Instance.NPCHit.Active && debug.origin.Valid(SoundType.NPCHit))
            {
                NPCDefinition nothing = new NPCDefinition(0);
                NPCDefinition npc = new NPCDefinition(debug.origin.ThingID);
                if (Config.Instance.NPCHit.Rule.ContainsKey(npc))
                {
                    custom = Config.Instance.NPCHit.Rule[npc];
                }
                else if (Config.Instance.NPCHit.Rule.ContainsKey(nothing))
                {
                    custom = Config.Instance.NPCHit.Rule[nothing];
                }
            }
            else if (Config.Instance.NPCKilled.Active && debug.origin.Valid(SoundType.NPCKilled))
            {
                NPCDefinition nothing = new NPCDefinition(0);
                NPCDefinition npc = new NPCDefinition(debug.origin.ThingID);
                if (Config.Instance.NPCKilled.Rule.ContainsKey(npc))
                {
                    custom = Config.Instance.NPCKilled.Rule[npc];
                }
                else if (Config.Instance.NPCKilled.Rule.ContainsKey(nothing))
                {
                    custom = Config.Instance.NPCKilled.Rule[nothing];
                }
            }

            if (custom != null && custom.Enabled)
            {
                custom.Validate();
                if (custom.Type == SoundTypeEnum.None) return null;
                //Only if not a "default" and if sound exists (mod associated with it loaded)
                else if (!custom.Equals(debug) && custom.Exists().exists) ModifySound(custom, ref type, ref Style, ref volumeScale, ref pitchOffset, ref debug);
            }

            //Check global filter
            if (!debug.replaced && Config.Instance.General.Active)
            {
                //Check if the currently playing sound exists

                CustomSound customKey = debug.ToCustomSound();
                CustomSound customNothingKey = new CustomSound(SoundTypeEnum.None);
                custom = null;
                var keys = Config.Instance.General.Rule.Keys;
                foreach (var key in keys)
                {
                    if (customKey.Equals(key))
                    {
                        custom = Config.Instance.General.Rule[key];
                        customKey = key;
                        break;
                    }
                }

                if (custom == null && Config.Instance.General.Rule.ContainsKey(customNothingKey))
                {
                    custom = Config.Instance.General.Rule[customNothingKey];
                }

                if (custom != null)
                {
                    if (custom.Enabled)
                    {
                        custom.Validate();
                        if (customKey.Type == SoundTypeEnum.None) return null;
                        //Only if not a "default" and if sound exists (mod associated with it loaded)
                        else if (custom != defaultSoundValue && custom.Exists().exists) ModifySound(custom, ref type, ref Style, ref volumeScale, ref pitchOffset, ref debug);
                    }
                }
            }

            var instance = orig(type, x, y, Style, volumeScale, pitchOffset);
            if (instance != null)
            {
                if (Config.Instance.Debug.Contains(debug)) return instance;
                AddDebugSound(debug);
            }
            RevertAmbientSwap();
            return instance;
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

        private void ModifySound(CustomSoundValue custom, ref int type, ref int Style, ref float volumeScale, ref float pitchOffset, ref DebugSound debug)
        {
            type = (int)custom.Type;
            volumeScale *= custom.Volume;
            volumeScale = Math.Min(volumeScale, CustomSoundValue.MAX_VOLUME); //Errors happen if it's above limit

            FixVolume(ref volumeScale);

            pitchOffset = custom.Pitch;
            DebugSound old = debug;
            debug = new DebugSound(custom, debug, replaced: true)
            {
                IsReplacing = old
            };

            Style = FixStyle(custom);

            AmbientSwap(type);
        }

        private void AddDebugSound(DebugSound sound)
        {
            bool anywhere = sound.X == -1 || sound.Y == -1;
            if (anywhere || Main.LocalPlayer.DistanceSQ(sound.worldPos) < MaxRangeSQ)
            {
                if (anywhere)
                {
                    sound.worldPos = Main.LocalPlayer.Center;
                }
                if (playedSounds.Count >= Config.Instance.Debug.TrackedSoundsCount)
                {
                    int index = playedSounds.FindIndex(s => !s.Tracked);
                    if (index != -1) playedSounds.RemoveAt(index);
                }
                enqueueTimer = enqueueTimerMax;
                playedSounds.Add(sound);
            }
        }

        /// <summary>
        /// Because sounds that are ambient use this to scale volume instead
        /// </summary>
        private void AmbientSwap(int type)
        {
            if ((type >= 30 && type <= 35) || type == 39)
            {
                oldAmbientVolume = Main.ambientVolume;
                Main.ambientVolume = Main.soundVolume;
                revertVolumeSwap = true;
            }
        }

        internal static void FixVolume(ref float volumeScale)
        {
            //Cap for volume because vanilla is retarded
            if (Main.soundVolume * volumeScale > 1f)
            {
                volumeScale = Main.soundVolume / volumeScale;
            }
        }

        internal static void RevertAmbientSwap()
        {
            if (revertVolumeSwap)
            {
                revertVolumeSwap = false;
                Main.ambientVolume = oldAmbientVolume;
            }
        }

        internal static bool PlayDebugSound(int type, int x = -1, int y = -1, int Style = 1, float volumeScale = 1, float pitchOffset = 0, int index = -1)
        {
            if (!PlayingDebugSound)
            {
                playingDebugStart = true;
                playingDebugIndex = index;
                FixVolume(ref volumeScale);
                Style = FixStyle(type, Style);
                Main.PlaySound(type, x, y, Style, volumeScale, pitchOffset);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void DrawDebugText(string text, Vector2 pos, Color color, float scale = 0.8f, bool shadow = false)
        {
            if (shadow)
            {
                for (int i = 0; i < ChatManager.ShadowDirections.Length; i++)
                {
                    Main.spriteBatch.DrawString(Main.fontItemStack, text, pos + ChatManager.ShadowDirections[i] * 2, Color.Black * (color.A / 255f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            Main.spriteBatch.DrawString(Main.fontItemStack, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
