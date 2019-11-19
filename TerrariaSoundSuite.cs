using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerrariaSoundSuite
{
    public class TerrariaSoundSuite : Mod
    {
        internal static bool loaded = false;

        public TerrariaSoundSuite()
        {

        }

        public override void Load()
        {
            Hooks.Load();
            Data.Load();
            Reflections.Load();
            loaded = true;
        }
        public override void Unload()
        {
            Data.Unload();
            Reflections.Unload();
            loaded = false;
        }

        public override void PreSaveAndQuit() => Meth.ClearSounds();

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) => InterfaceLayers.ModifyInterfaceLayers(layers);
    }
}
