using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace TerrariaSoundSuite
{

	[TypeConverter(typeof(ToFromStringConverter<CustomSoundValue>))]
	[Label("New Sound")]
	[BackgroundColor(50, 255, 50, 200)]
	public class CustomSoundValue : CustomSound
	{
		internal override void Validate()
		{
			//Auto clamp because property
			base.Validate();
			Volume = Volume;
			Pitch = Pitch;
		}

		[DefaultValue(true)]
		[Tooltip("If this sound rule should be applied")]
		public bool Enabled { get; set; }

		internal const float MIN_VOLUME = 0;
		internal const float MAX_VOLUME = 2f;

		private float _Volume;

		[DefaultValue(1f)]
		[Range(MIN_VOLUME, MAX_VOLUME)]
		[Slider]
		[Tooltip("Volume multiplier of this sound")]
		public float Volume
		{
			get
			{
				return _Volume;
			}
			set
			{
				_Volume = Utils.Clamp(value, MIN_VOLUME, MAX_VOLUME);
			}
		}

		internal const float MIN_PITCH = -1f;
		internal const float MAX_PITCH = 1f;

		private float _Pitch;

		[DefaultValue(0f)]
		[Range(MIN_PITCH, MAX_PITCH)]
		[Slider]
		[Tooltip("Pitch offset of this sound")]
		public float Pitch
		{
			get
			{
				return _Pitch;
			}
			set
			{
				_Pitch = Utils.Clamp(value, MIN_PITCH, MAX_PITCH);
			}
		}

		[Label("Held Item Constraint")]
		[Tooltip("Only apply this sound if player holds this specified item (leave blank to always apply)")]
		public ItemDefinition HeldItemConstraint = new ItemDefinition(ItemID.None);

		public override string ToString()
		{
			return $"{nameof(Enabled)}: {(Enabled ? "Yes" : "No")} {SEPARATOR} {(int)Type} {SEPARATOR} {Style} {SEPARATOR} {ShortPath} {SEPARATOR} {Volume} {SEPARATOR} {Pitch}";
		}
	}
}
