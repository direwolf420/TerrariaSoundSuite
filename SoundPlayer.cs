using Terraria;
using Terraria.ModLoader;

namespace TerrariaSoundSuite
{
	internal class SoundPlayer : ModPlayer
	{
		public override void PostUpdate()
		{
			Meth.CountdownEnqueue();
			Meth.RevertAmbientSwap();
		}

		public override void OnEnterWorld(Player player)
		{
			Meth.AmbiguityMessage();
			Meth.ClearSounds();
		}
	}
}
