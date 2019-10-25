using Newtonsoft.Json;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{
    //TODO make preview sound when clicking some button in the UI
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
            "Click on 'Play Sound' to play the current sound" + "\n" +
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
