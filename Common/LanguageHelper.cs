﻿using System;
using System.Linq;
using System.Collections.Generic;

using HarmonyLib;

namespace Common
{
	using Harmony;
	using Reflection;

	// for allowing to use SMLHelper language override files
	class LanguageHelper
	{
		static bool inited = false;

		// internally using prefix for id strings to avoid conflicts with different mods (all strings are end up in one common list)
		// don't use prefix in mod's code unless you're sure you need it
		static readonly string prefix = Mod.id + ".";

		public static void init()
		{
			if (inited || !(inited = true))
				return;

			// search for any classes that derived from LanguageHelper and add their public static string members to SMLHelper.LanguageHandler
			ReflectionHelper.definedTypes.
				Where(type => type.IsSubclassOf(typeof(LanguageHelper))).
				SelectMany(type => type.fields()).
				Where(field => field.FieldType == typeof(string) && field.IsStatic && field.IsPublic && !field.IsLiteral). // const strings are not added to LanguageHandler
				forEach(field => field.SetValue(null, add(field.Name, field.GetValue(null) as string))); // changing value of string to its name, so we can use it as a string id for 'str' method
		}

		// in BZ 'Language.main' is a property and throws exception when accessed during shutting down
		static Language Language_main => Mod.isShuttingDown? null: Language.main;

		// get string by id from Language.main
		public static string str(string ids) =>
			(ids == null || !Language_main)? ids: (Language.main.TryGet(prefix + ids, out string result)? result: ids);

		// add string to LanguageHandler, use getFullID if you need to get ids with prefix (e.g. for UI labels)
		public static string add(string ids, string str, bool getFullID = false)
		{																							$"LanguageHelper: adding string '{ids}': \"{str}\"".logDbg();
			if (!addString)
				return str;

			string fullID = prefix + ids;
			addString.invoke(fullID, str);

			if (Language_main)
			{
#if DEBUG
				if (Language.main.strings.TryGetValue(fullID, out string currStr) && currStr != str)
					$"LanguageHelper: changing string '{fullID}' (\"{currStr}\" -> \"{str}\")".logDbg();
#endif
				Language.main.strings[fullID] = str;
			}

			return getFullID? fullID: ids;
		}

		// using this to avoid including SMLHelper as a reference to Common project
		static readonly MethodWrapper<Action<string, string>> addString =
			Type.GetType("Nautilus.Handlers.LanguageHandler, SMLHelper")?.method("SetLanguageLine")?.wrap<Action<string, string>>();


		static Dictionary<string, string> substitutedStrings = null; // 'key' string using value of 'value' string

		// use 'substituteStringID' string as value for 'stringID' (for using before Language.main is loaded)
		public static void substituteString(string stringID, string substituteStringID)
		{
			if (substitutedStrings == null)
			{
				substitutedStrings = new Dictionary<string, string>();
				HarmonyHelper.patch();
			}

			substitutedStrings[stringID] = substituteStringID;
		}

		[HarmonyPriority(Priority.Low)]
		[HarmonyPostfix, HarmonyHelper.Patch(typeof(Language), nameof(Language.LoadLanguageFile))]
		static void substituteStrings(Language __instance) =>
			substitutedStrings.forEach(subst => __instance.strings[subst.Key] = __instance.strings[subst.Value]);
	}
}