using Newtonsoft.Json;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{
    //TODO screenshot guide how to set up sounds and shit
    //For both item/npc, and for global
    //What debug mode does and shows
    //TODO explain you can change style of an unloaded, modded sound, but it won't play at all until you switch out the type to something else, and save
    //TODO explain that if you have an "empty" rule, you can't add more rules to it
    //TODO if you have sound type set to "None" as the "new sound", it will mute that thing
    //TODO explain deaf mode (mouseover, close range for clutter etc)
    //TODO can't favorite last remaining unfavorited sound
    //TODO default blacklist explanation
    public class Config : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public static Config Instance => ModContent.GetInstance<Config>();

        [Header("Specific filters")]

        [Tooltip("Adjust what sound items make when they are used")]
        [Label("[i: 1] Items")]
        public ItemPage Item = new ItemPage()
        {
            Active = true
        };

        [Tooltip("Adjust what sound NPCs make when they are hit")]
        [Label("[i: 2493] NPC Hit")]
        public NPCHitPage NPCHit = new NPCHitPage()
        {
            Active = true
        };

        [Tooltip("Adjust what sound NPCs make when they die")]
        [Label("[i: 1281] NPC Killed")]
        public NPCKilledPage NPCKilled = new NPCKilledPage()
        {
            Active = true
        };

        [Header("General")]

        [Tooltip("Adjust a specific sound")]
        [Label("[i:75] All Sounds")]
        public GeneralPage General = new GeneralPage()
        {
            Active = true
        };

        [Tooltip("Adjust and toggle Debugging")]
        [Label("[i:509] Debug Settings")]
        public DebugPage Debug = new DebugPage()
        {
            Active = false,
            TrackedSoundsCount = 10,
            Blacklist = new List<CustomSound>()
            {
                new CustomSound(SoundTypeEnum.MenuOpen),
                new CustomSound(SoundTypeEnum.MenuClose),
                new CustomSound(SoundTypeEnum.MenuTick),
                new CustomSound(SoundTypeEnum.Tink),
                new CustomSound(SoundTypeEnum.Run),
                new CustomSound(SoundTypeEnum.CoinPickup)
                //new CustomSound(SoundTypeEnum.Grab)
            }
        };

        [Header(
            "****USAGE MANUAL****" + "\n" +
            "Click on one of the categories you wish to edit." + "\n" +
            "To add a new rule, click the '+'." + "\n" +
            "In the 'key' entry, click the empty icon that says 'nothing'." + "\n" +
            "Pick a thing you want the sound to replace of." + "\n" +
            "In the 'New Sound' entry, pick a sound type, its style, volume and pitch." + "\n" +
            "" + "\n" +
            "If you want to replace ALL sounds of a given type (Item, NPC), leave the 'key' blank." + "\n" +
            "If you want to mute a sound of the given key, simply set the sound type to 'None'." + "\n" +
            "To find out what numbers to use, check the 'Debug Settings'." + "\n" +
            "If you want to know what each style means and more, visit the homepage."
            )]
        [Label("Dummy variable for manual")]
        [JsonIgnore]
        public bool Useless => true;

        //TODO presets?
    }
}
