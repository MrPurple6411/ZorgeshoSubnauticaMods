﻿//using Common;
using Common.Harmony;

namespace VehicleCameraDrone
{
	public static class Main
	{
		//internal static readonly ModConfig config = Mod.init<ModConfig>();

		public static void patch()
		{
			HarmonyHelper.patchAll();
		}
	}
}