using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;

using Common.Harmony;
using Common.Reflection;

namespace DayNightSpeed
{
	using CIEnumerable = IEnumerable<CodeInstruction>;

	[PatchClass]
	static class DayNightCyclePatches
	{
		static readonly MethodInfo deltaTime = typeof(DayNightCycle).method("get_deltaTime");
		static readonly MethodInfo dayNightSpeed = typeof(DayNightCycle).method("get_dayNightSpeed");

		// simple transpiler for changing 1.0 to current value of dayNightSpeed
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.Update))]
		[HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.Resume))]
		[HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.StopSkipTimeMode))]
		[HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.OnConsoleCommand_day))]
		[HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.OnConsoleCommand_night))]
		[HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.OnConsoleCommand_daynight))]
		static CIEnumerable transpiler_dayNightSpeed(CIEnumerable cins) =>
			cins.ciReplace(ci => ci.isLDC(1.0f), CIUtils.speed);

		// transpiler for correcting time if daynightspeed < 1 (with additional multiplier)
		static CIEnumerable transpiler_dnsClamped01(CIEnumerable cins, string multCfgVarName) =>
			cins.ciInsert(ci => ci.isOp(OpCodes.Callvirt, deltaTime) || ci.isOp(OpCodes.Callvirt, dayNightSpeed), +1, 0,
				CIUtils.speedClamped01, OpCodes.Div,
				CIHelper._codeForCfgVar(multCfgVarName), OpCodes.Mul);

		[HarmonyTranspiler] // power charging
		[HarmonyPatch(typeof(Charger), nameof(Charger.Update))]
		[HarmonyPatch(typeof(SolarPanel), nameof(SolarPanel.Update))]
		[HarmonyPatch(typeof(ThermalPlant), nameof(ThermalPlant.AddPower))]
		[HarmonyPatch(typeof(BaseBioReactor), nameof(BaseBioReactor.Update))]
		[HarmonyPatch(typeof(BaseNuclearReactor), nameof(BaseNuclearReactor.Update))]
		static CIEnumerable transpiler_dnsClamped01_charge(CIEnumerable cins) =>
			transpiler_dnsClamped01(cins, nameof(ModConfig.auxSpeedPowerCharge));

		[HarmonyTranspiler] // power consuming
		[HarmonyPatch(typeof(ToggleLights), nameof(ToggleLights.UpdateLightEnergy))]
		[HarmonyPatch(typeof(FiltrationMachine), nameof(FiltrationMachine.UpdateFiltering))]
		static CIEnumerable transpiler_dnsClamped01_consume(CIEnumerable cins) =>
			transpiler_dnsClamped01(cins, nameof(ModConfig.auxSpeedPowerConsume));
	}
}