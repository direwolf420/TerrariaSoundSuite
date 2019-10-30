﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TerrariaSoundSuite
{
    internal class SoundPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (TerrariaSoundSuite.enqueueTimer > 0) TerrariaSoundSuite.enqueueTimer--;
            TerrariaSoundSuite.RevertAmbientSwap();
        }

        public override void OnEnterWorld(Player player)
        {
            if (ModLoader.GetMod("AnnoyingSoundReplacer") != null)
            {
                Main.NewText("It seems you have 'AnnoyingSoundReplacer' aswell as 'Terraria Sound Suite' enabled. The former is now outdated, you should disable it", Color.Orange);
            }
            TerrariaSoundSuite.playedSounds.Clear();
        }
    }
}
