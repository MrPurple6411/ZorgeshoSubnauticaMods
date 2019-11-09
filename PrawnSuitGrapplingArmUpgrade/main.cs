﻿using Common;
using Common.Crafting;
using Common.Configuration;

namespace PrawnSuitGrapplingArmUpgrade
{
	public static class Main
	{
		internal static ModConfig config = Config.tryLoad<ModConfig>();

		public static void patch()
		{
			HarmonyHelper.patchAll();
			Options.init();

			CraftHelper.patchAll();
		}
	}
}