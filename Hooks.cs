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
    internal static class Hooks
    {
        internal static void Load()
        {
            On.Terraria.Main.PlaySound_int_int_int_int_float_float += HookPlaySound;
        }

        internal static SoundEffectInstance HookPlaySound(On.Terraria.Main.orig_PlaySound_int_int_int_int_float_float orig, int type, int x, int y, int Style, float volumeScale, float pitchOffset)
        {
            if (Data.playingDebugStart)
            {
                Data.playingDebugStart = false;
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
                else if (!custom.Equals(debug) && custom.Exists().exists) Meth.ModifySound(custom, ref type, ref Style, ref volumeScale, ref pitchOffset, ref debug);
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
                        else if (custom != Data.defaultSoundValue && custom.Exists().exists) Meth.ModifySound(custom, ref type, ref Style, ref volumeScale, ref pitchOffset, ref debug);
                    }
                }
            }

            var instance = orig(type, x, y, Style, volumeScale, pitchOffset);
            if (instance != null)
            {
                if (Config.Instance.Debug.Contains(debug)) return instance;
                Meth.AddDebugSound(debug);
            }
            Meth.RevertAmbientSwap();
            return instance;
        }
    }
}
