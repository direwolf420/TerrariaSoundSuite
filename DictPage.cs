using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{
    [SeparatePage]
    public class DictPage<T1>
    {
        [DefaultValue(true)]
        [Tooltip("Toggle this category on/off")]
        public bool Active;

        public Dictionary<T1, CustomSoundValue> Rule = new Dictionary<T1, CustomSoundValue>();

        private int EnabledCount => Rule.Values.ToList().Count(v => v.Enabled);

        public override string ToString()
        {
            return $"{ nameof(Active)}: {(Active ? "Yes" : "No")}, #Rules: {Rule.Count}, #Enabled: {EnabledCount}";
        }

        public override bool Equals(object obj)
        {
            if (obj is DictPage<T1> other)
            {
                return Active == other.Active && Rule.Equals(other.Rule);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode() => new { Active , Rule }.GetHashCode();
    }

    public class ItemPage : DictPage<ItemDefinition>
    {

    }

    public class NPCHitPage : DictPage<NPCDefinition>
    {

    }

    public class NPCKilledPage : DictPage<NPCDefinition>
    {

    }

    public class GeneralPage : DictPage<CustomSound>
    {

    }
}
