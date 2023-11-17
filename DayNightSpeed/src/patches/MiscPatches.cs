namespace DayNightSpeed;

using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Common.Harmony;

using CIEnumerable = System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>;
using static Common.Harmony.CIHelper;

// fixing hunger/thirst timers
[HarmonyPatch(typeof(Survival), nameof(Survival.UpdateStats))]
static class Survival_UpdateStats_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins) =>
		cins.ciInsert(ci => ci.isLDC(100f), +1, 2,
			CIUtils.speed, OpCodes.Mul,
			_codeForCfgVar(nameof(ModConfig.auxSpeedHungerThirst)), OpCodes.Mul);

#if DEBUG // debug patch
	static void Postfix(Survival __instance)
	{
		if (Main.config.dbgCfg.enabled && Main.config.dbgCfg.showSurvivalStats)
			$"food: {__instance.food} water: {__instance.water}".onScreen("survival stats");
	}
#endif
}

// fixing crafting times
[HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.Craft))]
static class CrafterLogic_Craft_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins)
	{
		int ld = 0;
		return cins.ciInsert(ci => (ci.isOp(OpCodes.Ldarg_2) && ++ld == 2) || ci.isLDC(0.1f), +1, 2,
			CIUtils.speed, OpCodes.Mul);
	}
}

// fixing maproom scan times
[HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.UpdateScanRangeAndInterval))]
static class MapRoomFunctionality_ScanInterval_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins) =>
		cins.ciInsert(new MemberMatch(nameof(MapRoomFunctionality.scanInterval)), 0, 1, CIUtils.speedClamped01, OpCodes.Mul);
}

// we can use object with propulsion cannon after shot in 3 seconds
[HarmonyPatch(typeof(PropulseCannonAmmoHandler), nameof(PropulseCannonAmmoHandler.Update))]
static class PropulseCannonAmmoHandler_Update_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins) =>
		cins.ciInsert(ci => ci.isLDC(3.0f), CIUtils.speedClamped01, OpCodes.Mul);
}

// fixing stillsuit water capture speed
[HarmonyPatch(typeof(Stillsuit), "IEquippable.UpdateEquipped")]
static class Stillsuit_UpdateEquipped_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins) =>
		cins.ciInsert(ci => ci.isLDC(100f),
			CIUtils.speed, OpCodes.Mul,
			_codeForCfgVar(nameof(ModConfig.speedStillsuitWater)), OpCodes.Mul);
}

// fixed lifetime for explosion
[HarmonyPatch(typeof(WorldForces), nameof(WorldForces.AddExplosion))]
static class WorldForces_AddExplosion_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins) =>
		cins.ciInsert(ci => ci.isLDC(500f), CIUtils.speedClamped01, OpCodes.Div);
}

// fixed lifetime for current
[HarmonyPatch(typeof(WorldForces), nameof(WorldForces.AddCurrent), typeof(WorldForces.Current))]
static class WorldForces_AddCurrent_Patch
{
	static void Prefix(WorldForces.Current current)
	{
		if (double.IsPositiveInfinity(current.endTime))
			return;

		double lifeTime = (current.endTime - current.startTime) * DayNightSpeedControl.getSpeedClamped01();
		current.endTime = current.startTime + lifeTime;
	}
}

// fixes for explosions and currents
[HarmonyPatch(typeof(WorldForces), nameof(WorldForces.DoFixedUpdate))]
static class WorldForces_DoFixedUpdate_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins)
	{
		var list = cins.ToList();

		list.ciInsert(ci => ci.isLDC<double>(0.03f), CIUtils.speedClamped01, OpCodes.Mul); // do not change to 0.03d !
		list.ciInsert(ci => ci.isLDC(500f), CIUtils.speedClamped01, OpCodes.Div); // changing only first '500f'

		return list;
	}
}

#if SUBNAUTICA
// peeper enzyme recharging interval, just use speed setting at the moment of start
[HarmonyPatch(typeof(Peeper), nameof(Peeper.Start))]
static class Peeper_Start_Patch
{
	static float rechargeIntervalInitial = 0;
	static void Postfix(Peeper __instance)
	{
		if (rechargeIntervalInitial == 0)
			rechargeIntervalInitial = __instance.rechargeInterval;

		__instance.rechargeInterval = rechargeIntervalInitial * Main.config.dayNightSpeed;
	}
}

// fixing sunbeam counter so it shows realtime seconds regardless of daynightspeed
[HarmonyPatch(typeof(uGUI_SunbeamCountdown), nameof(uGUI_SunbeamCountdown.UpdateInterface))]
static class uGUISunbeamCountdown_UpdateInterface_Patch
{
	static CIEnumerable Transpiler(CIEnumerable cins) =>
		cins.ciInsert(ci => ci.isOp(OpCodes.Sub), CIUtils.speed, OpCodes.Div);
}
#endif
