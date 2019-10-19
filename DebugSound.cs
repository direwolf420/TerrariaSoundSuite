using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaSoundSuite
{
    public class DebugSound
    {
        internal const string UNKNOWN = "Unknown";
        internal const string CUSTOM = "Custom";

        internal int type;
        internal Vector2 worldPos;
        internal int X => (int)worldPos.X;
        internal int Y => (int)worldPos.Y;
        internal int Style;
        internal float volumeScale;
        internal float pitchOffset;

        internal bool modded;
        internal string typeName;

        internal bool replaced;

        internal Origin origin;

        internal Color color;

        private int _HoverTime;
        internal int HoverTime
        {
            get
            {
                return _HoverTime;
            }
            set
            {
                _HoverTime = Utils.Clamp(value, 0, TerrariaSoundSuite.hoverTimeMax);
            }
        }

        internal string path;
        internal string stacktrace;

        internal bool Tracked { get; set; }

        internal DebugSound IsReplacing { get; set; }

        internal DebugSound(CustomSoundValue custom, DebugSound other, bool replaced = false) : this((int)custom.Type, other.X, other.Y, custom.Style, custom.Volume, custom.Pitch, replaced)
        {

        }

        internal DebugSound(int type, int x, int y, int Style, float volumeScale, float pitchOffset, bool replaced = false)
        {
            this.type = type;
            worldPos.X = x;
            worldPos.Y = y;
            SoundTypeEnum soundTypeEnum = (SoundTypeEnum)type;
            ValidStyles validStyles = CustomSound.GetValidStyles(soundTypeEnum);

            //Red is an ass and we won't be working with him again
            if (soundTypeEnum == SoundTypeEnum.ZombieMoan)
            {
                if (Style == NPCID.ThePossessed) Style = validStyles.others[0];
                else if (Style == NPCID.SandShark) Style = validStyles.others[1];
                else Style = validStyles.FirstValidStyle;
            }

            if (validStyles.Always) Style = validStyles.FirstValidStyle;
            if (!validStyles.Contains(Style)) Style = validStyles.FirstValidStyle;

            this.Style = Style;
            this.volumeScale = volumeScale;
            this.pitchOffset = pitchOffset;
            modded = false;
            SoundType soundType = GetModLoaderSoundType(type);
            if (soundType == SoundType.Music) modded = false;
            else if (type == SoundLoader.customSoundType) modded = true;
            else if (Style >= GetNumVanilla(soundType)) modded = true;
            typeName = GetSoundTypeName(type);

            this.replaced = replaced;

            color = Color.White;

            if (Config.Instance.Debug.Verbose && !Config.Instance.Debug.Contains(type, Style))
            {
                //Get path to sound
                //Credit to jopojelly
                if (modded)
                {
                    var kvp = TerrariaSoundSuite.sounds[soundType].FirstOrDefault(s => s.Value == Style);
                    if (kvp.Key != null)
                    {
                        path = kvp.Key;
                    }
                }
                if (!replaced && type != (int)SoundTypeEnum.Waterfall && type != (int)SoundTypeEnum.Lavafall)
                {
                    //Get place where it was called from
                    //Credit to jopojelly
                    var frames = new StackTrace(true).GetFrames();
                    Logging.PrettifyStackTraceSources(frames);
                    int index = 2;
                    while (frames[index].GetMethod().Name.Contains("PlaySound"))
                        index++;
                    var frame = frames[index];
                    var method = frame.GetMethod();
                    int lineNumber = frame.GetFileLineNumber();
                    stacktrace = method.DeclaringType.FullName + "." + method.Name + (lineNumber == 0 ? "" : ":" + lineNumber);
                }
            }
            if (string.IsNullOrEmpty(path)) path = CustomSound.VANILLA_PATH;
            if (string.IsNullOrEmpty(stacktrace)) stacktrace = string.Empty;

            //Get origin
            Entity ent;
            origin = new Origin(soundType, 0, UNKNOWN);
            if (modded) origin.Name = CUSTOM;
            if (soundType == SoundType.Custom) origin = new Origin(SoundType.Custom, 0, CUSTOM);

            //TODO origin name not being displayed when sound is replaced, also filter by origin properly if it's coming from an item (name != "")
            switch (soundTypeEnum)
            {
                case SoundTypeEnum.Item:
                case SoundTypeEnum.PlayerHit:
                case SoundTypeEnum.FemaleHit:
                case SoundTypeEnum.PlayerKilled:
                case SoundTypeEnum.MaxMana:
                    if (soundTypeEnum != SoundTypeEnum.Item && Main.netMode == NetmodeID.SinglePlayer)
                    {
                        origin.Name = "me";
                        break;
                    }
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        ent = Main.player[i];
                        if (ent.active && ent.Hitbox.Contains(new Point(x, y)) && ent is Player player)
                        {
                            if (soundTypeEnum == SoundTypeEnum.Item)
                            {
                                if (player.HeldItem.type > 0 && player.HeldItem.UseSound != null &&
                                    player.HeldItem.UseSound.Style == Style && player.itemAnimation == player.itemAnimationMax)
                                {
                                    origin.ThingID = player.HeldItem.type;
                                    origin.Name = player.HeldItem.Name;
                                    worldPos += new Vector2(4); //This is to offset other sounds that might occur at the same time (maxMana e.g.)
                                }
                            }
                            else
                            {
                                if (i == Main.myPlayer) origin.Name = "me";
                                else origin.Name = player.name;
                                break;
                            }
                            break;
                        }
                    }
                    break;
                case SoundTypeEnum.NPCHit:
                case SoundTypeEnum.NPCKilled:
                case SoundTypeEnum.Roar:
                case SoundTypeEnum.Zombie:
                case SoundTypeEnum.ZombieMoan:
                case SoundTypeEnum.Mummy:
                case SoundTypeEnum.Pixie:
                case SoundTypeEnum.Duck:
                case SoundTypeEnum.Frog:
                case SoundTypeEnum.Bird:
                case SoundTypeEnum.Critter:
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        ent = Main.npc[i];
                        if (ent.active && ent.Hitbox.Contains(new Point(x, y)) && ent is NPC npc)
                        {
                            if (soundTypeEnum == SoundTypeEnum.NPCHit && npc.HitSound != null && npc.HitSound.Style == Style)
                            {
                                origin.ThingID = npc.type;
                                origin.Name = npc.GivenOrTypeName;
                            }
                            else if (soundTypeEnum == SoundTypeEnum.NPCKilled && npc.DeathSound != null && npc.DeathSound.Style == Style)
                            {
                                origin.ThingID = npc.type;
                                origin.Name = npc.GivenOrTypeName;
                                worldPos += new Vector2(4); //This is to offset the hitsound and deathsound position
                            }
                            else
                            {
                                origin.Name = npc.GivenOrTypeName;
                            }
                            break;
                        }
                    }
                    break;
                case SoundTypeEnum.Tile:
                    Point point = worldPos.ToTileCoordinates();
                    Tile tile = Framing.GetTileSafely(point);
                    if (tile.active())
                    {
                        //Credit to jopojelly (WorldgenPreviewer)
                        string text = Lang._mapLegendCache.FromTile(Main.Map[point.X, point.Y], point.X, point.Y);
                        if (text == string.Empty)
                        {
                            if (tile.type < TileID.Count)
                                text = TileID.Search.GetName(tile.type);
                            else
                                text = TileLoader.GetTile(tile.type).Name;
                        }
                        if (text != string.Empty)
                        {
                            origin.Name = text;
                        }
                    }
                    break;
                //case SoundTypeEnum.Grab: doesn't work cause sound originates from player
                default:
                    break;
            }
        }

        //0 : doesnt matter, random soundInstanceDig[0]
        //1 : doesnt matter, random soundInstancePlayerHit[3]
        //2 : soundInstanceItem.Length
        //3 : soundInstanceNPCHit.Length
        //4 : soundInstanceNPCKilled.Length
        //5+: deoesn't matter
        //14: 542 -> 7 (NPCID.SandShark)
        //--- 469 -> (random) 21 to 23 (NPCID.ThePossessed)
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

        /*			Array.Resize(ref Main.soundItem, nextSound[SoundType.Item]);
                    Array.Resize(ref Main.soundInstanceItem, nextSound[SoundType.Item]);
                    Array.Resize(ref Main.soundNPCHit, nextSound[SoundType.NPCHit]);
                    Array.Resize(ref Main.soundInstanceNPCHit, nextSound[SoundType.NPCHit]);
                    Array.Resize(ref Main.soundNPCKilled, nextSound[SoundType.NPCKilled]);
                    Array.Resize(ref Main.soundInstanceNPCKilled, nextSound[SoundType.NPCKilled]);
                    Array.Resize(ref Main.music, nextSound[SoundType.Music]);
                    Array.Resize(ref Main.musicFade, nextSound[SoundType.Music]);
         */

        internal static string GetSoundTypeName(int type)
        {
            string ret = "Unknown";
            if (Enum.IsDefined(typeof(SoundTypeEnum), type))
            {
                ret = Enum.GetName(typeof(SoundTypeEnum), type);
            }
            return ret;
        }

        internal static int GetNumVanilla(SoundType type)
        {
            switch (type)
            {
                case SoundType.Custom:
                    return 0;
                case SoundType.Item:
                    return Main.maxItemSounds + 1;
                case SoundType.NPCHit:
                    return Main.maxNPCHitSounds + 1;
                case SoundType.NPCKilled:
                    return Main.maxNPCKilledSounds + 1;
                case SoundType.Music:
                    return Main.maxMusic;
            }
            return 0;
        }

        internal SoundType GetModLoaderSoundType(int type)
        {
            switch (type)
            {
                case 2:
                    return SoundType.Item;
                case 3:
                    return SoundType.NPCHit;
                case 4:
                    return SoundType.NPCKilled;
                case SoundLoader.customSoundType:
                    return SoundType.Custom;
                default:
                    return SoundType.Music; //Hack
            }
        }

        internal CustomSound ToCustomSound() => new CustomSound((SoundTypeEnum)type, Style, path);

        public override string ToString()
        {
            string text = "Type: " + type + " (" + typeName + "); Style: " + Style;
            if (origin.Name != UNKNOWN)
            {
                text += "; Origin:";
                if (origin.Name != CUSTOM) text += " " + origin;
            }
            if (modded) text += " (" + (Config.Instance.Debug.Verbose ? path : "Modded") + ")";
            if (replaced) text += " (Replaced)";
            return text;
        }
    }

    internal class Origin
    {
        internal SoundType SoundType;
        internal int ThingID;
        internal string Name;

        internal Origin(SoundType type, int thingID, string name)
        {
            SoundType = type;
            ThingID = thingID;
            Name = name;
        }

        internal bool Valid(SoundType otherType) => SoundType == otherType && ThingID > 0 && Name != DebugSound.UNKNOWN;

        public override string ToString() => Name;
    }

    public enum SoundTypeEnum : int
    {
        None = -1,
        Tile = SoundID.Dig,
        PlayerHit = SoundID.PlayerHit,
        Item = SoundID.Item,
        NPCHit = SoundID.NPCHit,
        NPCKilled = SoundID.NPCKilled,
        PlayerKilled = SoundID.PlayerKilled,
        Grass = SoundID.Grass,
        DoorOpen = SoundID.DoorOpen,
        Grab = SoundID.Grab,
        DoorClosed = SoundID.DoorClosed,
        MenuOpen = SoundID.MenuOpen,
        MenuClose = SoundID.MenuClose,
        MenuTick = SoundID.MenuTick,
        Shatter = SoundID.Shatter,
        /// <summary>
        /// Very special because of 469, 542 -> need to be remapped in Style
        /// </summary>
        ZombieMoan = SoundID.ZombieMoan,
        Roar = SoundID.Roar,
        DoubleJump = SoundID.DoubleJump,
        Run = SoundID.Run,
        Coins = SoundID.Coins,
        Splash = SoundID.Splash,
        FemaleHit = SoundID.FemaleHit,
        Tink = SoundID.Tink,
        Unlock = SoundID.Unlock,
        Drown = SoundID.Drown,
        Chat = SoundID.Chat,
        MaxMana = SoundID.MaxMana,
        Mummy = SoundID.Mummy,
        Pixie = SoundID.Pixie,
        Mech = SoundID.Mech,
        Zombie = SoundID.Zombie,
        Duck = SoundID.Duck,
        Frog = SoundID.Frog,
        Bird = SoundID.Bird,
        Critter = SoundID.Critter,
        Waterfall = SoundID.Waterfall,
        Lavafall = SoundID.Lavafall,
        ForceRoar = SoundID.ForceRoar,
        Meowmere = SoundID.Meowmere,
        CoinPickup = SoundID.CoinPickup,
        Drip = SoundID.Drip,
        Camera = SoundID.Camera,
        MoonLord = SoundID.MoonLord,
        TrackableDD2 = SoundID.Trackable,
        Custom = SoundLoader.customSoundType
    }
}
