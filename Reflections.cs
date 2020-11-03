using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerrariaSoundSuite
{
	internal static class Reflections //of a floating world
	{
		internal const BindingFlags BF_STATIC = BindingFlags.Static | BindingFlags.NonPublic;
		internal const BindingFlags BF_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

		internal static object modConfig;
		internal static Type UIModConfigType;
		internal static MethodInfo setMessageMethod;

		internal static FieldInfo isInitializedField;
		internal static FieldInfo IsMouseHoveringField;
		internal static FieldInfo modField;

		internal static bool IsInitialized => (bool)isInitializedField.GetValue(modConfig);
		internal static bool IsMouseHovering => (bool)IsMouseHoveringField.GetValue(modConfig);
		internal static bool IsCurrentMod => ((Mod)modField.GetValue(modConfig)) == Data.Instance;

		internal static void ReflectSound()
		{
			if (Data.sounds == null)
			{
				FieldInfo soundsField = typeof(SoundLoader).GetField("sounds", BF_STATIC);
				Data.sounds = (Dictionary<SoundType, IDictionary<string, int>>)soundsField.GetValue(null);
				if (Data.sounds == null) throw new Exception("Reflection failed at getting the sound dictionary, report in the homepage of the mod!");
			}
		}

		internal static void ReflectConfig()
		{
			if (setMessageMethod == null)
			{
				try
				{
					//Interface.modConfig.SetMessage("Error: " + e.Message, Color.Red);
					Assembly ModLoaderAssembly = typeof(ModLoader).Assembly;
					Type Interface = ModLoaderAssembly.GetType("Terraria.ModLoader.UI.Interface");
					FieldInfo modConfigField = Interface.GetField("modConfig", BF_STATIC);
					modConfig = modConfigField.GetValue(null);

					UIModConfigType = ModLoaderAssembly.GetType("Terraria.ModLoader.Config.UI.UIModConfig");
					setMessageMethod = UIModConfigType.GetMethod("SetMessage", new Type[] { typeof(string), typeof(Color) });
					if (setMessageMethod == null) throw new NullReferenceException("setMessageMethod is null");

					Type type = typeof(UIElement);
					isInitializedField = type.GetField("_isInitialized", BF_INSTANCE);
					IsMouseHoveringField = type.GetField("_isMouseHovering", BF_INSTANCE);
					modField = UIModConfigType.GetField("mod", BF_INSTANCE);
				}
				catch (Exception e)
				{
					Meth.Log("Failed to reflect SetMessage: " + e);
				}
			}
		}

		/// <summary>
		/// Accesses the ModConfig message UI text box to push messages
		/// </summary>
		internal static void SetMessage(string text, Color color)
		{
			if (TerrariaSoundSuite.loaded && setMessageMethod != null)
			{
				try
				{
					//Order is important
					if (IsInitialized && IsCurrentMod && IsMouseHovering)
					{
						setMessageMethod.Invoke(modConfig, new object[] { text, color });
					}
				}
				catch (Exception e)
				{
					Meth.Log("Failed to invoke UIModConfig.SetMessage: " + e);
				}
			}
		}

		internal static void Load()
		{
			ReflectSound();
			ReflectConfig();
		}

		internal static void Unload()
		{
			UIModConfigType = null;
			setMessageMethod = null;
		}
	}
}
