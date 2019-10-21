using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{
    [TypeConverter(typeof(ToFromStringConverter<CustomSound>))]
    [Label("key")] //So it's consistent with the "key" from the EntityDefinition dicts
    [BackgroundColor(255, 255, 50, 200)]
    public class CustomSound
    {
        //DON'T CHANGE THOSE ONCE RELEASED
        internal const string VANILLA_PATH = "Vanilla";
        internal const char SEPARATOR = '|';

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context) => Validate();

        internal virtual void Validate()
        {
            if (!Enum.IsDefined(Type.GetType(), Type))
            {
                Type = SoundTypeEnum.None; //Default fallback
            }
            //Auto clamp because property
            Style = Style;
            Path = Path;
        }
        
        /// <summary>
        /// Checks if a sound style exists (modded mainly)
        /// </summary>
        internal (bool exists, int style) Exists()
        {
            if (!Modded) return (true, _Style);
            else
            {
                //Path only then not empty if it's a valid SoundType, but safety check anyway
                if (SoundType != SoundType.Music)
                {
                    if (TerrariaSoundSuite.sounds[SoundType].TryGetValue(Path, out int style))
                    {
                        return (true, style);
                    }
                    else
                    {
                        return (false, -1);
                    }
                }
                return (false, _Style);
            }
        }

        public CustomSound()
        {
            Style = ValidStyles.FirstValidStyle;
        }

        public CustomSound(SoundTypeEnum type) : this()
        {
            Type = type;
        }

        public CustomSound(SoundTypeEnum type, int style)
        {
            Type = type;
            Style = style;
        }

        public CustomSound(SoundTypeEnum type, int style, string path) : this(type, style)
        {
            Path = path;
        }

        internal ValidStyles ValidStyles => GetValidStyles(Type);

        private SoundType SoundType => GetModLoaderSoundType();

        private bool Modded => _Style >= DebugSound.GetNumVanilla(SoundType);

        private SoundTypeEnum _Type;

        [DefaultValue(SoundTypeEnum.None)]
        [Tooltip("Type of this sound")]
        public SoundTypeEnum Type
        {
            get
            {
                return _Type;
            }
            set
            {
                if (!Modded)
                {
                    //Reset path if sound changes type to vanilla
                    Path = VANILLA_PATH;

                    //If sound type changes, style takes care of updating Path
                }
                _Type = value;
            }
        }

        internal const int MIN_STYLES = -1;
        ///<summary>
        ///This should be the highest value out of all the styles
        ///</summary>
        internal const int MAX_STYLES = Main.maxItemSounds + 100; //Valid index + buffer up assuming alot of mods add custom item sounds

        private int _Style;

        [DefaultValue(-1)]
        [Range(MIN_STYLES, MAX_STYLES)]
        [Tooltip("Style of this sound")]
        public int Style
        {
            get
            {
                if (Modded && TerrariaSoundSuite.loaded)
                {
                    var (exists, style) = Exists();
                    if (!exists) return _Style;
                    else return style;
                }
                return _Style;
            }
            set
            {
                //bool modItBelongsToUnloaded = false;
                if (Modded && TerrariaSoundSuite.loaded)
                {
                    var kvp = TerrariaSoundSuite.sounds[SoundType].FirstOrDefault(s => s.Key == Path);
                    //Don't reset current path if it doesn't exist (aka mod unloaded that the Path belonged to)
                    if (kvp.Key != null)
                    {
                        //so it resets with the following style change
                        Path = VANILLA_PATH;
                    }
                    else
                    {
                        TerrariaSoundSuite.SetMessage("This specific sound won't play (because its mod is unloaded). Change to different type", Color.Orange);
                    }
                }
                if (!(Modded && ValidStyles.LastValidStyle <= 0))
                {
                    //Style adjustment gets checked in Exists(), and if style isn't valid, nothing happens ingame anyway
                    _Style = Utils.Clamp(value, ValidStyles.FirstValidStyle, ValidStyles.LastValidStyle);
                }
                if (TerrariaSoundSuite.loaded)
                {
                    if (Path == VANILLA_PATH) //Default: assign new path if there is one
                    {
                        var kvp = TerrariaSoundSuite.sounds[SoundType].FirstOrDefault(s => s.Value == _Style);
                        if (kvp.Key != null)
                        {
                            Path = kvp.Key;
                        }
                    }
                }
                if (ValidStyles.Always || !ValidStyles.Contains(_Style))
                {
                    _Style = ValidStyles.FirstValidStyle;
                }
            }
        }

        private string _Path = VANILLA_PATH; //Otherwise it's null

        [DefaultValue(VANILLA_PATH)]
        [Label("Sound path")]
        public string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                //TODO do something about user manually changing path
                if (!Modded)
                {
                    _Path = VANILLA_PATH;
                } 
            }
        }

        [JsonIgnore]
        public string ShortPath => Path.Split(new char[] { '/' }).Last();

        public override bool Equals(object obj)
        {
            if (obj is CustomSound other)
                return Type == other.Type && Style == other.Style && Path == other.Path;
            if (obj is DebugSound other2)
                return (int)Type == other2.type && Style == other2.Style && Path == other2.path;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            //Only those three are relevant for "identifying"
            return new { Type, Style, Path }.GetHashCode();
        }

        public override string ToString()
        {
            return $"{(int)Type} {SEPARATOR} {Style} {SEPARATOR} {Path}";
        }

        /// <summary>
        /// Needed so it converts the stringed key representation into an object again
        /// </summary>
        public static CustomSound FromString(string s)
        {
            string[] vars = s.Split(new char[] { SEPARATOR }, 5, StringSplitOptions.RemoveEmptyEntries);
            CustomSound custom = new CustomSound();
            try
            {
                int type = Convert.ToInt32(vars[0]);
                if (Enum.IsDefined(typeof(SoundTypeEnum), type))
                {
                    custom.Type = (SoundTypeEnum)type;
                    custom.Style = Convert.ToInt32(vars[1]);
                    custom.Path = vars[2].Trim();
                }
                else
                {
                    throw new Exception("Type " + type.ToString() + " is not a valid sound type, pick one between " + (int)SoundTypeEnum.None + " and " + (int)SoundTypeEnum.Custom);
                }
            }
            catch (Exception e)
            {
                TerrariaSoundSuite.Instance.Logger.Info("########");
                TerrariaSoundSuite.Instance.Logger.Info("Couldn't convert config key element: " + e);
                TerrariaSoundSuite.Instance.Logger.Info("String: " + s);
                TerrariaSoundSuite.Instance.Logger.Info("########");
            }
            return custom;
        }

        internal SoundType GetModLoaderSoundType()
        {
            switch (Type)
            {
                case SoundTypeEnum.Item:
                    return SoundType.Item;
                case SoundTypeEnum.NPCHit:
                    return SoundType.NPCHit;
                case SoundTypeEnum.NPCKilled:
                    return SoundType.NPCKilled;
                case SoundTypeEnum.Custom:
                    return SoundType.Custom;
                default:
                    //Stupid ik
                    return SoundType.Music;
            }
        }

        internal static ValidStyles GetValidStyles(SoundTypeEnum type)
        {
            //This is needed cause config loading happens before ModLoader does anything sound related
            //(resizing Item/NPCHit/NPCDeath sounds and initializing sound dict)
            int safeOverflow = TerrariaSoundSuite.loaded ? 0 : 42069;
            switch (type)
            {
                case SoundTypeEnum.Item:
                    return new ValidStyles(0, Main.soundInstanceItem.Length + safeOverflow);
                case SoundTypeEnum.NPCHit:
                    return new ValidStyles(0, Main.soundInstanceNPCHit.Length + safeOverflow);
                case SoundTypeEnum.NPCKilled:
                    return new ValidStyles(0, Main.soundInstanceNPCKilled.Length + safeOverflow);
                case SoundTypeEnum.ZombieMoan:
                    return new ValidStyles(-1, others: new List<int> { /*469*/ 0, /*542*/ 1 });
                case SoundTypeEnum.Roar:
                    return new ValidStyles(0, Main.soundInstanceRoar.Length, new List<int> { 4 });
                case SoundTypeEnum.Splash:
                    return new ValidStyles(0, Main.soundInstanceSplash.Length);
                case SoundTypeEnum.Mech:
                    return new ValidStyles(0, Main.soundInstanceMech.Length); //length of one lol
                case SoundTypeEnum.Zombie:
                    return new ValidStyles(0, Main.soundInstanceZombie.Length);
                case SoundTypeEnum.Bird:
                    return new ValidStyles(14, 5 + 1);
                //case SoundTypeEnum.Waterfall:
                //case SoundTypeEnum.Lavafall:
                //    return new ValidStyles(0, 50 + 1);
                case SoundTypeEnum.ForceRoar:
                    return new ValidStyles(0, Main.soundInstanceRoar.Length, new List<int> { -1 });
                case SoundTypeEnum.Meowmere:
                    return new ValidStyles(5, 5 + 1);
                case SoundTypeEnum.Drip:
                    return new ValidStyles(0, Main.soundInstanceDrip.Length);
                case SoundTypeEnum.TrackableDD2:
                    return new ValidStyles(0, SoundID.TrackableLegacySoundCount);
                case SoundTypeEnum.Custom:
                    TerrariaSoundSuite.ReflectSound();
                    //It sets length to 0 if there's no custom sounds loaded, kinda checked in get of Style
                    return new ValidStyles(0, TerrariaSoundSuite.sounds[SoundType.Custom].Count + safeOverflow);
                //case SoundTypeEnum.Tile:
                //case SoundTypeEnum.PlayerKilled:
                //case SoundTypeEnum.Grass:
                //case SoundTypeEnum.Grab:
                //case SoundTypeEnum.DoorOpen:
                //case SoundTypeEnum.DoorClosed:
                //case SoundTypeEnum.MenuOpen:
                //case SoundTypeEnum.MenuClose:
                //case SoundTypeEnum.MenuTick:
                //case SoundTypeEnum.Shatter:
                //case SoundTypeEnum.DoubleJump:
                //case SoundTypeEnum.Run:
                //case SoundTypeEnum.Coins:
                //case SoundTypeEnum.FemaleHit:
                //case SoundTypeEnum.Tink:
                //case SoundTypeEnum.Unlock:
                //case SoundTypeEnum.Drown:
                //case SoundTypeEnum.Chat:
                //case SoundTypeEnum.MaxMana:
                //case SoundTypeEnum.Mummy:
                //case SoundTypeEnum.Pixie:
                //case SoundTypeEnum.Duck:
                //case SoundTypeEnum.Frog:
                //case SoundTypeEnum.Critter:
                //case SoundTypeEnum.CoinPickup:
                //case SoundTypeEnum.Camera:
                //case SoundTypeEnum.MoonLord:
                //case SoundTypeEnum.None:
                default:
                    return new ValidStyles(-1);
            }

            /*
            //public const int SoundID.Dig = 0;
            //public const int SoundID.PlayerHit = 1;
            //public const int SoundID.Item = 2;
            //public const int SoundID.NPCHit = 3;
            //public const int SoundID.NPCKilled = 4;
            //public const int SoundID.PlayerKilled = 5;
            //public const int SoundID.Grass = 6;
            //public const int SoundID.Grab = 7;
            //public const int SoundID.DoorOpen = 8;
            //public const int SoundID.DoorClosed = 9;
            //public const int SoundID.MenuOpen = 10;
            //public const int SoundID.MenuClose = 11;
            //public const int SoundID.MenuTick = 12;
            //public const int SoundID.Shatter = 13;
            //public const int SoundID.ZombieMoan = 14;
            //public const int SoundID.Roar = 15;
            //public const int SoundID.DoubleJump = 16;
            //public const int SoundID.Run = 17;
            //public const int SoundID.Coins = 18;
            //public const int SoundID.Splash = 19;
            //public const int SoundID.FemaleHit = 20;
            //public const int SoundID.Tink = 21;
            //public const int SoundID.Unlock = 22;
            //public const int SoundID.Drown = 23;
            //public const int SoundID.Chat = 24;
            //public const int SoundID.MaxMana = 25;
            //public const int SoundID.Mummy = 26;
            //public const int SoundID.Pixie = 27;
            //public const int SoundID.Mech = 28;
            //public const int SoundID.Zombie = 29;
            //public const int SoundID.Duck = 30;
            //public const int SoundID.Frog = 31;
            //public const int SoundID.Bird = 32;
            //public const int SoundID.Critter = 33;
            //public const int SoundID.Waterfall = 34;
            //public const int SoundID.Lavafall = 35;
            //public const int SoundID.ForceRoar = 36;
            //public const int SoundID.Meowmere = 37;
            //public const int SoundID.CoinPickup = 38;
            //public const int SoundID.Drip = 39;
            //public const int SoundID.Camera = 40;
            //public const int SoundID.MoonLord = 41;
            //public const int SoundID.Trackable = 42;
            */

            //0 : doesnt matter, random soundInstanceDig[0]
            //1 : doesnt matter, random soundInstancePlayerHit[3]
            //2 : soundInstanceItem.Length
            //3 : soundInstanceNPCHit.Length
            //4 : soundInstanceNPCKilled.Length
            //5+: deoesn't matter
            //14: 542 -> 7
            //--- 469 -> (random) 21 to 23
            //--- else-> (random) 0 to 2 (all in soundInstanceZombie[num])
            //^^^ special snowflakes
            //15 : soundInstanceRoar[3] but 4 is also accepted, turns into 1 with less pitch
            //16+: doesnt matter
            //19 : soundInstanceSplash[2]
            //20 : doesnt matter, random soundInstanceFemaleHit[3]
            //21+: doesnt matter
            //26 : doesnt matter, random 3 or 4, soundInstanceZombie[num]
            //27 : doesnt matter
            //28 : soundInstanceMech[1] (lol, sort of "doesn't matter" since its 0)
            //29 : soundInstanceZombie[106]
            //30 : doesnt matter, (random) 10 or 11, soundInstanceZombie[num]
            //31 : doesnt matter, soundInstanceZombie[13]
            //32 : (pickable) 14 to 19, soundInstanceZombie[num]
            //33 : doesnt matter, uses soundInstanceZombie[15]
            //34 : (pickable) 0 to 50, decides volume
            //35 : (pickable) 0 to 50, decides volume
            //36 : can be -1, uses soundInstanceRoar styles tho
            //37 : style decides volume, (pickable) 5 to 10
            //38 : doesnt matter, (random) 0 to 4
            //39 : soundInstanceDrip[3]
            //40 : doesnt matter
            //41 : doesnt matter
            //42 : (pickable) 0 to SoundID.TrackableLegacySoundCount - 1
        }
    }
}
