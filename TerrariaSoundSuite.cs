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

        public override void PreSaveAndQuit()
        {
            Meth.ClearSounds();
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            InterfaceLayers.ModifyInterfaceLayers(layers);
        }
    }
}
