﻿using Common;
using Common.Harmony;
using Common.Crafting;

namespace MiscObjects
{
	public static class Main
	{
		internal static readonly ModConfig config = Mod.init<ModConfig>();

		public static void patch()
		{
			HarmonyHelper.patchAll(true);
			CraftHelper.patchAll();

			LanguageHelper.init();
		}
	}
}