﻿using Common;
using Common.Crafting;

namespace PrawnSuitSonarUpgrade
{
	public static class Main
	{
		public static void patch()
		{
			HarmonyHelper.patchAll(false);

			CraftHelper.patchAll();
		}
	}
}