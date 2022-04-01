﻿using System;
using System.Text;
using System.Linq;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

using Common;
using Common.Harmony;
using Common.Crafting;

namespace MiscPatches
{
	// adjusting whirlpool torpedo punch force
	[HarmonyPatch(typeof(SeamothTorpedoWhirlpool), "Awake")]
	static class SeamothTorpedoWhirlpool_Awake_Patch
	{
		static bool Prepare() => Main.config.gameplayPatches;

		static void Postfix(SeamothTorpedoWhirlpool __instance) => __instance.punchForce = Main.config.torpedoPunchForce;
	}

	// change flares burn time and intensity
	[HarmonyPatch(typeof(Flare), "Awake")]
	static class Flare_Awake_Patch
	{
		static bool Prepare() => Main.config.gameplayPatches;

		static void Postfix(Flare __instance)
		{
			if (__instance.energyLeft == 1800)
				__instance.energyLeft = Main.config.flareBurnTime;

			__instance.originalIntensity = Main.config.flareIntensity;
		}
	}

	// flare in inventory shows whether it is lighted
	[HarmonyPatch(typeof(TooltipFactory), "ItemCommons")]
	static class TooltipFactory_ItemCommons_Patch
	{
		static bool Prepare() => Main.config.gameplayPatches;

		static void Postfix(StringBuilder sb, TechType techType, GameObject obj)
		{
			if (techType == TechType.Flare && obj.GetComponent<Flare>().hasBeenThrown)
				TooltipFactory.WriteDescription(sb, "[lighted]");
		}
	}

	// Stop dead creatures twitching animations (stop any animations, to be clear)
	[HarmonyPatch(typeof(CreatureDeath), "OnKill")]
	static class CreatureDeath_OnKill_Patch
	{
		const float timeToStopAnimator = 5f;

		static bool Prepare() => Main.config.gameplayPatches;

		static void Postfix(CreatureDeath __instance)
		{
			__instance.gameObject.callAfterDelay(timeToStopAnimator, new UnityAction(() =>
			{
				if (__instance.gameObject.GetComponentInChildren<Animator>() is Animator animator)
					animator.enabled = false;
			}));
		}
	}

	// we can kill HangingStingers now
	[HarmonyPatch(typeof(HangingStinger), "Start")]
	static class HangingStinger_Start_Patch
	{
		static GameObject deathEffect = null;
		const float maxHealth = 10f;

		static bool Prepare() => Main.config.gameplayPatches;

		static void Postfix(HangingStinger __instance)
		{
			LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();

			if (!deathEffect)
				UWE.CoroutineHost.StartCoroutine(_deathEffect());

			// can't change it just once, stingers use three LiveMixinData (short, middle, long)
			liveMixin.data.destroyOnDeath = true;
#if GAME_SN
			liveMixin.data.explodeOnDestroy = false;
#endif
			liveMixin.data.deathEffect = deathEffect;
			liveMixin.data.maxHealth = maxHealth;

			liveMixin.health = liveMixin.data.maxHealth;

			static IEnumerator _deathEffect() // multiple ?
			{
				var task = PrefabUtils.getPrefabAsync(TechType.BlueCluster);
				yield return task;
				deathEffect = task.GetResult().GetComponent<LiveMixin>().data.deathEffect;
			}
		}
	}

	// disable first use animations for tools and escape pod hatch cinematics
	[OptionalPatch, PatchClass]
	static class FirstAnimationsPatch
	{
		static bool prepare() => !Main.config.firstAnimations;

		[HarmonyPrefix, HarmonyPatch(typeof(Player), "AddUsedTool")]
		static bool Player_AddUsedTool_Prefix(ref bool __result) => __result = false;

#if GAME_SN
		[HarmonyPrefix, HarmonyPatch(typeof(EscapePod), "Awake")]
		static void EscapePod_Awake_Prefix(EscapePod __instance) => __instance.bottomHatchUsed = __instance.topHatchUsed = true;
#endif
	}

	// For adding propulsion/repulsion cannon immunity to some objects
	// for now: <BrainCoral> <Drillable>
	[PatchClass]
	static class PropRepCannonImmunity
	{
		class ImmuneToPropRepCannon: MonoBehaviour {}

		static bool prepare() => Main.config.additionalPropRepImmunity;

		static bool isObjectImmune(GameObject go)
		{
			if (!go || go.GetComponent<ImmuneToPropRepCannon>())
				return true;

			if (go.GetComponent<BrainCoral>() || go.GetComponent<Drillable>()) // maybe I'll add some more
			{
				go.AddComponent<ImmuneToPropRepCannon>();
				return true;
			}

			return false;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(PropulsionCannon), "ValidateNewObject")]
		static bool PropulsionCannon_ValidateNewObject_Prefix(GameObject go, ref bool __result) =>
			__result = !isObjectImmune(go);

		[HarmonyTranspiler, HarmonyPatch(typeof(RepulsionCannon), "OnToolUseAnim")]
		static IEnumerable<CodeInstruction> RepulsionCannon_OnToolUseAnim_Transpiler(IEnumerable<CodeInstruction> cins)
		{
			var list = cins.ToList();

			int[] i = list.ciFindIndexes(ci => ci.isOp(OpCodes.Brfalse),
										 ci => ci.isOpLoc(OpCodes.Ldloc_S, 11), // Rigidbody component = gameObject.GetComponent<Rigidbody>();
										 ci => ci.isOp(OpCodes.Brfalse));
			return i == null? cins:
				list.ciInsert(i[1],
					OpCodes.Ldloc_S, 11,
					CIHelper.emitCall<Func<GameObject, bool>>(isObjectImmune),
					OpCodes.Brtrue, list[i[2]].operand);
		}
	}

	[PatchClass]
	static class ChangeChargersSpeed
	{
		static bool prepare() => Main.config.changeChargersSpeed;

		[HarmonyPostfix, HarmonyPatch(typeof(BatteryCharger), "Initialize")] // BatteryCharger speed
		static void BatteryCharger_Initialize_Postfix(Charger __instance)
		{
			__instance.chargeSpeed = Main.config.batteryChargerSpeed * (Main.config.chargersAbsoluteSpeed? 100f: 1f);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(PowerCellCharger), "Initialize")] // PowerCellCharger speed
		static void PowerCellCharger_Initialize_Postfix(Charger __instance)
		{
			__instance.chargeSpeed = Main.config.powerCellChargerSpeed * (Main.config.chargersAbsoluteSpeed? 200f: 1f);
		}

		// chargers speed is not linked to battery capacity
		[HarmonyPatch(typeof(Charger), "Update")]
		static class Charger_Update_Patch
		{
			static bool Prepare() => prepare() && Main.config.chargersAbsoluteSpeed;

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins) =>
				cins.ciRemove(new CIHelper.MemberMatch(OpCodes.Ldfld, nameof(Charger.chargeSpeed)), +2, 2); // remove "*capacity"
		}
	}

	[OptionalPatch, HarmonyPatch(typeof(WaterscapeVolume), "RenderImage")] // from ExtraOptions mod
	static class FogFixPatch
	{
		static bool Prepare() => Main.config.fixFog;
		static void Prefix(ref bool cameraInside) => cameraInside = false;
	}

#if GAME_BZ
	[OptionalPatch, HarmonyPatch(typeof(Builder), "Begin")]
	static class BuidlerRepeatPatch
	{
		static bool Prepare() => !Main.config.builderRepeat;
		static void Postfix() => Builder.ResetLast();
	}
#endif

	// unlock achievements even if console was used
	[OptionalPatch, HarmonyPatch(typeof(GameAchievements), "Unlock")]
	static class GameAchievements_Unlock_Patch
	{
		static bool Prepare() => Main.config.ignoreConsoleForAchievements;

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins) =>
			cins.ciInsert(new CIHelper.MemberMatch(nameof(DevConsole.HasUsedConsole)), +1, 0, OpCodes.Pop, OpCodes.Ldc_I4_0);
	}

	// change ranges for camera drones (noise range and max range)
	[OptionalPatch, HarmonyPatch(typeof(uGUI_CameraDrone), "LateUpdate")]
	static class uGUICameraDrone_LateUpdate_Patch
	{
		static bool Prepare() => Main.config.cameraDroneNoiseRange != 250f || Main.config.cameraDroneNoiseRange != 620f;

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins)
		{
			var list = cins.ToList();

			CIHelper.constToCfgVar(list, 250f, nameof(Main.config.cameraDroneNoiseRange));
			CIHelper.constToCfgVar(list, 250f, nameof(Main.config.cameraDroneNoiseRange));

			list.ciReplace(ci => ci.isLDC(520f),
				CIHelper._codeForCfgVar(nameof(Main.config.cameraDroneMaxRange)),
				OpCodes.Ldc_R4, 100f,
				OpCodes.Sub);

			return list;
		}
	}

	// don't use items on pickup
	[OptionalPatch, HarmonyPatch(typeof(QuickSlots), "OnAddItem")]
	static class QuickSlots_OnAddItem_Patch
	{
		static bool Prepare() => !Main.config.useItemsOnPickup;

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins) =>
			cins.ciRemove(new CIHelper.MemberMatch(nameof(QuickSlots.Select)), -2, 3);
	}
#if GAME_BZ
	// disable various eat sounds
	[OptionalPatch, HarmonyPatch(typeof(TechSoundData), "GetUseSound")]
	static class TechSoundData_GetUseSound_Patch
	{
		static bool Prepare() => !Main.config.useEatSounds;

		static readonly TechData.SoundType[] restrictedSounds = new[]
		{
			TechData.SoundType.Fish,
			TechData.SoundType.Food,
			TechData.SoundType.Meat,
			TechData.SoundType.Bleach,
			TechData.SoundType.Liquid,
			TechData.SoundType.FilteredWater,
			TechData.SoundType.BigWaterBottle,
			TechData.SoundType.DisinfectedWater,
		};

		static void Prefix(ref TechData.SoundType type)
		{																					$"Trying to play use sound: {type}".logDbg();
			if (restrictedSounds.contains(type))
				type = TechData.SoundType.Default;
		}
	}

	// be able to pick up non-empty storages
	[OptionalPatch, HarmonyPatch(typeof(PickupableStorage), "OnHandHover")]
	static class PickupableStorage_OnHandHover_Patch
	{
		static bool Prepare() => Main.config.pickupNonEmptyStorages;
		static void Prefix(PickupableStorage __instance) => __instance.allowPickupWhenNonEmpty = true;
	}

	// fix for glowing constructed objects
	[OptionalPatch, HarmonyPatch(typeof(SkyApplier), "Initialize")]
	static class GlowFixPatch
	{
		static bool Prepare() => Main.config.fixGlow;

		static void Postfix(SkyApplier __instance)
		{
			if (!__instance.initialized)
				return;

			uGUI_BuilderMenu.EnsureTechGroupTechTypeDataInitialized();
			var techType = __instance.GetComponent<Constructable>()?.techType ?? TechType.None;

			if (uGUI_BuilderMenu.groupsTechTypes[1].Contains(techType))
				__instance.OnEnvironmentChanged(null);
		}
	}
#endif

	static class MiscStuff
	{
		public static void init()
		{
#if GAME_SN
			CraftData.useEatSound.Add(TechType.Coffee, "event:/player/drink");
#endif
		}
	}
}