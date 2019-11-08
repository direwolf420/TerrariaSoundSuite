using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace TerrariaSoundSuite
{
    internal static class InterfaceLayers
    {
        internal const int INVENTORY_SIZE = 47;
        internal const int SHOP_SIZE = 40;

        internal static string Name => Data.Instance.Name;

        private static Color FadeBetween(Color c0, Color c1, float fade) => fade == 0f ? c0 : new Color(c0.ToVector4() * (1f - fade) + c1.ToVector4() * fade);

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

        /// <summary>
        /// Also draws the "[DEBUG]"
        /// </summary>
        private static void SetupDrawLocation(Player player, int size, ref int x, ref int y, ref float fade, ref string text)
        {
            if (Main.playerInventory)
            {
                //So it doesn't overlap with inventory and recipe UI
                x += 2 * INVENTORY_SIZE;
                y += 4 * INVENTORY_SIZE;

                if (player.chest != -1 || Main.npcShop != 0)
                {
                    //Y offset when chest or shop open
                    y += 4 * SHOP_SIZE;
                }
            }
            else
            {
                int buffsPerLine = 11;
                int lineOffset = 0;
                for (int b = buffsPerLine; b < player.buffType.Length; b += buffsPerLine)
                {
                    if (player.buffType[b] > 0)
                    {
                        lineOffset = b / buffsPerLine;
                    }
                }
                y += lineOffset * 50 + Main.buffTexture[1].Height;
                if (ModLoader.GetMod("ThoriumMod") != null)
                {
                    //Bard buffs bar
                    y += 16;
                }
            }

            fade = Data.enqueueTimer / (float)Data.enqueueTimerMax;
            text = "[" + "DEBUG" + "] Last Played Sounds:";
            if (Data.playedSounds.Count == 0) text += " None";
            Color color = FadeBetween(Color.White, Color.Green, fade);
            DrawDebugText(text, new Vector2(x, y), color);
            y += size;
        }

        internal static void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.gameMenu || !Config.Instance.Debug.Active) return;
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
            for (int i = 0; i < Data.playedSounds.Count; i++)
            {
                sound = Data.playedSounds[i];
                sounds.Add(sound);
                int size = 24;
                if (sound.Discovered && Utils.CenteredRectangle(sound.worldPos, new Vector2(size)).Contains(new Point((int)Main.MouseWorld.X, (int)Main.MouseWorld.Y)))
                {
                    hoverIndex = i;
                }
            }
            if (hoverIndex != -1)
            {
                sounds.RemoveAt(hoverIndex);
            }

            Vector2 offset = -new Vector2(18 >> 1) - Main.screenPosition;

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
                fade *= ((float)sound.HoverTime / Data.hoverTimeMax) * 0.3f + 0.7f;
                color = Color.White * fade;
                DrawDebugText(text, sound.worldPos + offset, color, 1f, true);
            }

            if (hoverIndex != -1)
            {
                sound = Data.playedSounds[hoverIndex];
                sound.HoverTime += 2;
                text = "(x) " + sound.typeName + " (" + sound.origin + ")";
                fade = ((float)sound.HoverTime / Data.hoverTimeMax) * 0.3f + 0.7f;
                color = Color.White * fade;
                DrawDebugText(text, sound.worldPos + offset, color, 1f, true);

            }
            return true;
        };

        private static readonly GameInterfaceDrawMethod Debug = delegate
        {
            //Credit to jopojelly (heavily adjusted from SummonersAssociation)
            Player player = Main.LocalPlayer;
            int xPosition = 32;
            int yPosition = 76 + SHOP_SIZE;
            int size = 24;
            Color color;
            float fade = 0f;
            string text = "";

            SetupDrawLocation(player, size, ref xPosition, ref yPosition, ref fade, ref text);

            bool newMouseInterface = false;

            int addedSoundIndex = -1;

            for (int i = 0; i < Data.playedSounds.Count; i++)
            {
                DebugSound sound = Data.playedSounds[i];
                float distance = player.DistanceSQ(sound.worldPos);
                distance = distance > Data.MaxRangeSQ ? Data.MaxRangeSQ : distance;
                float ratio = 1 - distance / Data.MaxRangeSQ;
                ratio = ratio * 0.6f + 0.4f;
                //from 0.4f to 1f
                color = sound.color;
                if (Data.playingDebugIndex == i)
                {
                    fade = Data.playingDebugCounter / (float)Data.playingDebugMax;
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
                            Meth.PlayDebugSound(sound.type, -1, -1, sound.Style, sound.volumeScale, sound.pitchOffset, i);
                        }
                        else if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            if (!sound.Tracked)
                            {
                                //always space for one
                                if (Data.playedSounds.Count(s => s.Tracked) < Config.Instance.Debug.TrackedSoundsCount - 1)
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
                    Data.hoverIndex = i;
                    newMouseInterface = true;
                }
            }

            if (addedSoundIndex != -1)
            {
                //Reorder tracked sounds to the beginning
                DebugSound temp = Data.playedSounds.ElementAt(addedSoundIndex);
                Data.playedSounds.RemoveAt(addedSoundIndex);
                Data.playedSounds.Insert(0, temp);
            }

            if (Data.hoverIndex != -1 && !player.mouseInterface)
            {
                DebugSound hover = Data.playedSounds[Data.hoverIndex];
                Vector2 vector = Main.ThickMouse ? new Vector2(16) : new Vector2(10);
                vector += Main.MouseScreen;
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, Main.fontMouseText, "Left click to " + (hover.Tracked ? "un" : "") + "track sound", vector, Color.White, 0f, Vector2.Zero, Vector2.One);
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
            for (int i = 0; i < Data.playedSounds.Count; i++)
            {
                DebugSound sound = Data.playedSounds[i];
                if (i == Data.hoverIndex || sound.Tracked)
                {
                    Color normal = sound.color;
                    Color clicked = Color.Transparent;

                    float fade = 0;
                    if (Data.playingDebugIndex == i) fade = Data.playingDebugCounter / (float)Data.playingDebugMax;
                    Color colorFade = FadeBetween(normal, clicked, fade);
                    int index = i == Data.hoverIndex ? Data.hoverIndex : i;

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
                        if (length > Data.MaxRange)
                        {
                            length = Utils.Clamp(length, Data.MaxRange, 8 * Data.MaxRange);
                            length /= 10 * Data.MaxRange;
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
            Data.hoverIndex = -1;
            return true;
        };
    }
}
