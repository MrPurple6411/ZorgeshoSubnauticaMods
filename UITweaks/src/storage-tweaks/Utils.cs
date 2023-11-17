﻿using UnityEngine;

#if SUBNAUTICA
using System.Collections.Generic;
using HarmonyLib;
using Common.Harmony;

#if !DEBUG
using Common;
#endif
#endif

#if DEBUG
using System;
using System.Linq;

using Common;

#if BELOWZERO
using System.Collections.Generic;
#endif
#endif

namespace UITweaks.StorageTweaks
{
	static class Utils
	{
#if DEBUG
		public static class TechTypeNamesTest
		{
			static readonly bool useTestString = true;
			static readonly bool useAllLanguages = true;

			const string testString = "123456789012345678901234567890123456789012345678901234567890";

			static int nameIndex = 0;
			static List<string> techTypeNames;

			static IEnumerable<string> getTechTypeNames() =>
				Enum.GetValues(typeof(TechType)).OfType<TechType>().Select(Language.main.Get);

			static void init()
			{
				if (techTypeNames != null)
					return;

				if (useAllLanguages)
				{
					techTypeNames = new();

					foreach (var lang in Language.main.GetLanguages())
					{
						try { Language.main.SetCurrentLanguage(lang); }
						catch (Exception) {}

						techTypeNames.AddRange(getTechTypeNames());
					}

					Language.main.SetCurrentLanguage("English");
				}
				else
				{
					techTypeNames = getTechTypeNames().ToList();
				}

				techTypeNames.Sort((str1, str2) => str2.Length - str1.Length);
			}

			// reset index to 0 if 'offset' is 0
			public static void changeIndex(int offset)
			{
				nameIndex = offset == 0? 0: MathUtils.mod(nameIndex + offset, techTypeNames.Count);
			}

			public static string getName()
			{
				if (useTestString)
					return testString;

				init();
				return techTypeNames[nameIndex];
			}
		}
#endif // DEBUG
		public static string getPrefabClassId(MonoBehaviour cmp)
		{
			return cmp.GetComponentInParent<PrefabIdentifier>(true)?.ClassId ?? "";
		}

		public static bool isAllowedToPickUpNonEmpty(this PickupableStorage storage)
		{
#if SUBNAUTICA
			return false;
#elif BELOWZERO
			return storage.allowPickupWhenNonEmpty;
#endif
		}

		public static int getItemSize(TechType techType)
		{
#if SUBNAUTICA
			var size = CraftData.GetItemSize(techType);
#elif BELOWZERO
			var size = TechData.GetItemSize(techType);
#endif
			return size.x * size.y;
		}

		public static int getLineCount(this TMPro.TMP_Text text) => text.textInfo.lineCount;
		public static int getFirstCharIndexAtLine(this TMPro.TMP_Text text, int line) => text.textInfo.lineInfo[line].firstCharacterIndex;

		public static void forceRedraw(this TMPro.TMP_Text text, string str)
		{
			text.text = str; // this actually sets text from the inputField, not 'str' for some reason
			text.ForceMeshUpdate();
		}

#if SUBNAUTICA // code is copied from BZ with some modifications
		static readonly Dictionary<GameInput.Button, string> bindingCache = new();
		static readonly Dictionary<GameInput.Button, Dictionary<string, string>> textCache = new();

		public static string GetText(this HandReticle _, string text, bool translate, GameInput.Button button)
		{
			if (text.isNullOrEmpty())
				return text;

			if (!textCache.TryGetValue(button, out Dictionary<string, string> buttonCache))
				textCache[button] = buttonCache = new();

			if (!buttonCache.TryGetValue(text, out string result))
			{
				result = translate? Language.main.Get(text): text;

				if (!bindingCache.TryGetValue(button, out string buttonBind))
					bindingCache[button] = buttonBind = uGUI.FormatButton(button);

				result = Language.main.GetFormat("HandReticleAddButtonFormat", result, buttonBind);
				buttonCache[text] = result;
			}

			return result;
		}

		[OptionalPatch, PatchClass]
		static class Patches
		{
			static bool prepare() => Main.config.storageTweaks.enabled;

			[HarmonyPostfix]
			[HarmonyPatch(typeof(HandReticle), nameof(HandReticle.OnBindingsChanged))]
			[HarmonyPatch(typeof(HandReticle), nameof(HandReticle.OnLanguageChanged))]
			static void clearCache()
			{
				bindingCache.Clear();
				textCache.Values.forEach(c => c.Clear());
			}
		}
#endif // SUBNAUTICA
	}
}