﻿using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Common;
using Common.Harmony;
using Common.Reflection;
using Common.Configuration;
using UnityEngine.EventSystems;


#if SUBNAUTICA
using UnityEngine.EventSystems;
#endif

namespace CustomHotkeys
{
	using static VFXParticlesPool;
	using CIEnumerable = IEnumerable<CodeInstruction>;

	[OptionalPatch, PatchClass]
	static class DevToolsHotkeysPatch
	{
		static bool prepare() => !Main.config.enableDevToolsHotkeys;

		// disabling F1 and F3 hotkeys for dev tools
		[HarmonyTranspiler, HarmonyPatch(typeof(MainGameController), nameof(MainGameController.Update))]
		static CIEnumerable F1_F3_disabler(CIEnumerable cins)
		{
			var list = cins.ToList();

			CIHelper.MemberMatch match = new (OpCodes.Call, Mod.Consts.isGameSN? "get_isShippingRelease": "get_isConsolePlatform");
			int[] i = list.ciFindIndexes(match, match, ci => ci.isOp(Mod.Consts.isGameSN? OpCodes.Blt: OpCodes.Ret));

			return i == null? cins: list.ciRemoveRange(i[0], i[2]);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GUIController), nameof(GUIController.Update))] // disable F6 (hide gui tool)
		[HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.Update))] // disable Shift+F5 (smoke test)
		static bool hotkeyDisabler() => false;
	}

	[OptionalPatch, PatchClass]
	static class FeedbackCollectorPatch
	{
		static bool prepare() => !Main.config.enableFeedback;

		public class SettingChanged: Config.Field.IAction
		{
			public void action()
			{
				if (uGUI_FeedbackCollector.main)
					uGUI_FeedbackCollector.main.enabled = Main.config.enableFeedback;
			}
		}

		// disable F8 (feedback collector)
		[HarmonyPostfix, HarmonyPatch(typeof(uGUI_FeedbackCollector), nameof(uGUI_FeedbackCollector.Awake))]
		static void uGUIFeedbackCollector_Awake_Postfix(uGUI_FeedbackCollector __instance) => __instance.enabled = Main.config.enableFeedback;

		// remove "Give Feedback" from the ingame menu
		[HarmonyTranspiler, HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Start))]
		static CIEnumerable IngameMenu_Start_Transpiler(CIEnumerable cins) => CIHelper.ciRemove(cins, 0, 3);
	}

	// patches for removing bindings and blocking 'Up' event after binding
	static class BindingPatches
	{
#if SUBNAUTICA
		class BindCheckPointer: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
		{
			public static GameObject hoveredObject { get; private set; }

			public void OnPointerEnter(PointerEventData eventData) => hoveredObject = gameObject;
			public void OnPointerExit(PointerEventData eventData)  => hoveredObject = null;
		}

		// allows to remove bindings from bind options without selecting them first
		// it's enough to just move cursor over the option and press 'Delete'
		// another part of the patch in the 'Update' patch
		[HarmonyPatch(typeof(UIBehaviour), "Start")]
		static class uGUIBinding_Start_Patch
		{
			static bool Prepare(UIBehaviour __instance) => Main.config.easyBindRemove && __instance is uGUI_Binding;

			static void Postfix(UIBehaviour __instance) =>
				__instance.gameObject.ensureComponent<BindCheckPointer>();
		}
#endif
		static int lastBindedIndex = -1;

		[HarmonyPatch(typeof(uGUI_Binding), nameof(uGUI_Binding.Update))]
		static class uGUIBinding_Update_Patch
		{
			static void saveLastBind() => lastBindedIndex = GameInput.lastInputPressed[0]; // for keyboard

			static CIEnumerable Transpiler(CIEnumerable cins)
			{
				var list = cins.ToList();

				// saving binded keycode to check later in GameInput.UpdateKeyInputs patch
				list.ciInsert(new CIHelper.MemberMatch(nameof(GameInput.ClearInput)), CIHelper.emitCall<Action>(saveLastBind));
				return list;
			}
		}

		[HarmonyPatch(typeof(uGUI_Binding), nameof(uGUI_Binding.RefreshValue))]
		static class uGUIBinding_RefreshValue_Patch
		{
			static bool Prefix(uGUI_Binding __instance)
			{
				if (!__instance.gameObject.GetComponent<KeyWModBindOption.Tag>())
					return true;

				__instance.currentText.text = (__instance.active || __instance.value == null)? "": __instance.value;
				__instance.UpdateState();
				return false;
			}
		}

		// if we press key while binding in options menu, ignore its 'Up' & 'Held' events
		[HarmonyPatch(typeof(GameInput), nameof(GameInput.GetInputState))]
		static class GameInput_UpdateKeyState_Patch
		{
			static CIEnumerable Transpiler(CIEnumerable cins, ILGenerator ilg)
			{
				var list = cins.ToList();
				var field_lastBindedIndex = typeof(BindingPatches).field(nameof(lastBindedIndex));
				var cinsCompare = CIHelper.toCIList(OpCodes.Ldarg_1, CIHelper.emitCall<Func<KeyCode>>(_lastBindedKeyCode));

				static KeyCode _lastBindedKeyCode() =>
					lastBindedIndex == -1 || GameInput.inputs.Count == 0? default: GameInput.inputs[lastBindedIndex].keyCode;

				int[] i = list.ciFindIndexes(new CIHelper.MemberMatch("GetKey"),
											 ci => ci.isOp(OpCodes.Call),
											 ci => ci.isOp(OpCodes.Call));
				if (i == null)
					return cins;

				Label lb1 = list[i[2] + 1].operand.cast<Label>();
				Label lb2 = list.ciDefineLabel(i[2] + 2, ilg); // label for 'inputState.flags |= GameInput.InputStateFlags.Up'

				CIHelper.LabelClipboard.__enabled = false;
				list.ciInsert(i[2] + 2,
					cinsCompare,							// compare last binded key with current
					OpCodes.Bne_Un_S, lb2,
					OpCodes.Ldc_I4_M1,						// BindingPatches.lastBindedIndex = -1;
					OpCodes.Stsfld, field_lastBindedIndex,
					OpCodes.Br_S, lb1);						// else inputState.flags |= GameInput.InputStateFlags.Up;

				Label lb0 = list[i[0] + 1].operand.cast<Label>();
				list.ciInsert(i[0] + 2, cinsCompare, OpCodes.Beq_S, lb0);

				return list;
			}
		}
	}

#if SUBNAUTICA // doesn't needed for BZ
	static class GameInput_AutoForward_Patch
	{
#pragma warning disable IDE0052
		static readonly HarmonyHelper.LazyPatcher _ = new (true);
#pragma warning restore IDE0052

		static bool autoforward = false;

		public static void setAutoForward(bool val) => autoforward = val;
		public static void toggleAutoForward() => setAutoForward(!autoforward);

		[HarmonyPostfix, HarmonyPatch(typeof(GameInput), nameof(GameInput.GetMoveDirection))]
		static void patchAutoForward(ref Vector3 __result)
		{
			if (autoforward)
				__result.z = 1f;
		}
	}
#endif

#if BELOWZERO
	// some 'disable IDE0052' pragmas are considered unnecessary by VS for some reason
#pragma warning disable IDE0079 // unnecessary suppression

	static class SeaTruckForcedExit
	{
#pragma warning disable IDE0052
		static readonly HarmonyHelper.LazyPatcher __ = new (true);
#pragma warning restore IDE0052

		public static void exitFrom(SeaTruckMotor truck)
		{
			if (truck)
				SeaTruckMotor_StopPiloting_ReversePatch(truck);
		}

		[HarmonyReversePatch, HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.StopPiloting))]
		static bool SeaTruckMotor_StopPiloting_ReversePatch(SeaTruckMotor truck)
		{
			_ = truck; _ = transpiler(null); // make compiler happy
			return false;

			// no checks for indexes here, can't just return 'cins' anyway
			static CIEnumerable transpiler(CIEnumerable cins)
			{
				var list = cins.ToList();

				// removing all code before first 'IsWalkable' check and putting 'truck.Unsubscribe()' instead
				// now we can ignore 'skipUnsubscribe' parameter
				int i = list.FindIndex(new CIHelper.MemberMatch(nameof(SeaTruckSegment.IsWalkable)));
				list.ciRemoveRange(0, i + 1);
				list.ciInsert(0, OpCodes.Ldarg_0, OpCodes.Call, typeof(SeaTruckMotor).method("Unsubscribe"));

				// removing 'forceStop' parameter check, now we can ignore it
				list.ciRemove(ci => ci.isOp(OpCodes.Ldarg_2), +0, 2);

				// removing code between second and third 'flag' assignment (first assignment is already removed)
				// now we can ignore 'waitForDocking' parameter
				int[] ii = list.ciFindIndexes(OpCodes.Stloc_0, OpCodes.Stloc_0);
				list.ciRemoveRange(ii[0] + 1, ii[1]);

				return list;
			}
		}
	}

	static class SeaTruckDetachModules
	{
#pragma warning disable IDE0052
		static readonly HarmonyHelper.LazyPatcher __ = new (true);
#pragma warning restore IDE0052

		public static void detachFrom(SeaTruckSegment segment)
		{
			if (segment?.rearConnection?.occupied == true)
				SeaTruckSegment_OnClickDetachLever_ReversePatch(segment);
		}

		[HarmonyReversePatch, HarmonyPatch(typeof(SeaTruckSegment), nameof(SeaTruckSegment.OnClickDetachLever))]
		static void SeaTruckSegment_OnClickDetachLever_ReversePatch(SeaTruckSegment segment)
		{
			_ = segment; _ = transpiler(null); // make compiler happy

			static CIEnumerable transpiler(CIEnumerable cins)
			{
				var list = cins.ToList();

				list.ciReplace(new CIHelper.MemberMatch(nameof(SeaTruckSegment.Detach)),
					OpCodes.Ldfld, typeof(SeaTruckSegment).field("rearConnection"),
					OpCodes.Callvirt, typeof(SeaTruckConnection).method("Disconnect"));

				int[] i = list.ciFindIndexes(
					new CIHelper.MemberMatch(nameof(SeaTruckSegment.CanAnimate)),
					ci => ci.isOp(OpCodes.Ret));

				list.ciRemoveRange(i[0] - 1, i[1] - 1);

				return list;
			}
		}
	}

	static class SeaTruckForcedConnect
	{
#pragma warning disable IDE0052
		static readonly HarmonyHelper.LazyPatcher __ = new (true);
#pragma warning restore IDE0052

		public static void connect(SeaTruckSegment rearSegment, SeaTruckSegment looseSegment)
		{
			if (rearSegment?.rearConnection && looseSegment?.frontConnection)
				SeaTruckConnection_OnTriggerEnter_ReversePatch(rearSegment.rearConnection, looseSegment.frontConnection);
		}

		[HarmonyReversePatch, HarmonyPatch(typeof(SeaTruckConnection), nameof(SeaTruckConnection.OnTriggerEnter))]
		static void SeaTruckConnection_OnTriggerEnter_ReversePatch(SeaTruckConnection rearConnection, SeaTruckConnection frontConnection)
		{
			_ = rearConnection; _ = frontConnection; _ = transpiler(null); // make compiler happy

			// using SeaTruckConnection from the parameter, not from the collider
			static CIEnumerable transpiler(CIEnumerable cins) => cins.ciRemove(1, 2);
		}
	}
#pragma warning restore IDE0079
#endif // BELOWZERO
}