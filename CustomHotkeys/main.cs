﻿using Common;
using Common.Harmony;

namespace CustomHotkeys
{
	public static class Main
	{
		internal static readonly ModConfig config = Mod.init<ModConfig>();

		public static void patch()
		{
			HarmonyHelper.patchAll();

			HotkeyHelper.init(config.hotkeys);
			PersistentConsoleCommands.createGameObject<ConsoleCommands>();
		}
	}
}