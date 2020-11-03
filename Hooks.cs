using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.ID;

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

            CustomSoundValue custom = null;
            Meth.AssignSoundFromPageIfMatch(Config.Instance.Item, debug, ref custom);
            Meth.AssignSoundFromPageIfMatch(Config.Instance.NPCHit, debug, ref custom);
            Meth.AssignSoundFromPageIfMatch(Config.Instance.NPCKilled, debug, ref custom);

            if (custom != null)
            {
                if (custom.Enabled && Meth.SatisfiesConstraint(custom))
                {
                    custom.Validate();
                    if (custom.Type == SoundTypeEnum.None) return null;
                    //Only if not a "default" and if sound exists (mod associated with it loaded)
                    else if (!custom.Equals(debug) && custom.Exists().exists)
                        Meth.ModifySound(custom, ref type, ref Style, ref volumeScale, ref pitchOffset, ref debug);
                }
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
                    if (custom.Enabled && Meth.SatisfiesConstraint(custom))
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
