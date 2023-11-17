﻿using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using System.Collections.Generic;

using HarmonyLib;

using Common;
using Common.Harmony;
using Common.Reflection;

namespace ConsoleImproved
{
	// patching vanilla console commands that use float.Parse method to use locale independent conversion
	[PatchClass]
	static class CommandsFloatParsePatch
	{
		static readonly MethodInfo floatParse = typeof(float).method("Parse", typeof(string));
		static readonly MethodInfo floatParseCulture = typeof(float).method("Parse", typeof(string), typeof(IFormatProvider));
		static readonly MethodInfo getInvariantCulture = typeof(CultureInfo).method("get_InvariantCulture");

		static bool prepare() => Main.config.fixVanillaCommandsFloatParse;

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(BaseFloodSim), nameof(BaseFloodSim.OnConsoleCommand_baseflood))]
		[HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.OnConsoleCommand_daynightspeed))]
		[HarmonyPatch(typeof(CreateConsoleCommand), nameof(CreateConsoleCommand.OnConsoleCommand_create))]
		[HarmonyPatch(typeof(GameModeConsoleCommands), nameof(GameModeConsoleCommands.OnConsoleCommand_damage))]
		[HarmonyPatch(typeof(PlayerMotor), nameof(PlayerMotor.OnConsoleCommand_swimx))]
		[HarmonyPatch(typeof(SNCameraRoot), nameof(SNCameraRoot.OnConsoleCommand_farplane))]
		[HarmonyPatch(typeof(SNCameraRoot), nameof(SNCameraRoot.OnConsoleCommand_nearplane))]
		[HarmonyPatch(typeof(SpawnConsoleCommand), nameof(SpawnConsoleCommand.OnConsoleCommand_spawn))]
		[HarmonyPatch(typeof(SpeedConsoleCommand), nameof(SpeedConsoleCommand.OnConsoleCommand_speed))]
		[HarmonyPatch(typeof(WaterParkCreature), nameof(WaterParkCreature.OnConsoleCommand_setwpcage))]
		static IEnumerable<CodeInstruction> floatParseFix(IEnumerable<CodeInstruction> cins)
		{
			var list = cins.ciReplace(ci => ci.isOp(OpCodes.Call, floatParse),
				OpCodes.Call, getInvariantCulture,
				OpCodes.Call, floatParseCulture);

			Debug.assert(list.FindIndex(ci => ci.isOp(OpCodes.Call, floatParse)) == -1);

			return list;
		}
	}
}