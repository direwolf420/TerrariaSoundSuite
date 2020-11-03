using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{
	[SeparatePage]
	public abstract class DictPage<T>
	{
		[DefaultValue(true)]
		[Tooltip("Toggle this category on/off")]
		public bool Active;

		internal abstract SoundType AssociatedSoundType { get; }

		public Dictionary<T, CustomSoundValue> Rule = new Dictionary<T, CustomSoundValue>();

		private int EnabledCount => Rule.Values.ToList().Count(v => v.Enabled);

		public override string ToString()
		{
			return $"{nameof(Active)}: {(Active ? "Yes" : "No")}, #Rules: {Rule.Count}, #Enabled: {EnabledCount}";
		}

		public override bool Equals(object obj)
		{
			if (obj is DictPage<T> other)
			{
				return Active == other.Active && Rule.Equals(other.Rule);
			}
			return base.Equals(obj);
		}

		public override int GetHashCode() => new { Active, Rule }.GetHashCode();
	}

	public class ItemPage : DictPage<ItemDefinition>
	{
		internal override SoundType AssociatedSoundType => SoundType.Item;
	}

	public class NPCHitPage : DictPage<NPCDefinition>
	{

		internal override SoundType AssociatedSoundType => SoundType.NPCHit;
	}

	public class NPCKilledPage : DictPage<NPCDefinition>
	{

		internal override SoundType AssociatedSoundType => SoundType.NPCKilled;
	}

	public class GeneralPage : DictPage<CustomSound>
	{
		internal override SoundType AssociatedSoundType => SoundType.Music;
	}
}
