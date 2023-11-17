using System.Collections;

using HarmonyLib;
using UnityEngine;

using Common;
using Common.Harmony;
using TMPro;

#if SUBNAUTICA
using Text = UnityEngine.UI.Text;
#elif BELOWZERO
	using Text = TMPro.TextMeshProUGUI;
#endif

#if BELOWZERO
using System.Text;
#endif

namespace UITweaks
{
	static class MiscTweaks
	{
		[OptionalPatch, PatchClass]
		static class BuilderMenuHotkeys
		{
			static bool prepare() => Main.config.builderMenuTabHotkeysEnabled;

			[HarmonyPostfix, HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.GetToolbarTooltip))]
			static void modifyTooltip(int index, TooltipData data)
			{
				if (!Main.config.showToolbarHotkeys)
					return;

				string text = $"<size=25><color=#ADF8FFFF>{index + 1}</color> - </size>";
				data.prefix.Insert(0, text);
			}

			[HarmonyPostfix, HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Open))]
			static void openMenu()
			{
				UWE.CoroutineHost.StartCoroutine(_builderMenuTabHotkeys());

				static IEnumerator _builderMenuTabHotkeys()
				{
					while (uGUI_BuilderMenu.singleton.state)
					{
						for (int i = 0; i < 5; i++)
							if (Input.GetKeyDown(KeyCode.Alpha1 + i))
								uGUI_BuilderMenu.singleton.SetCurrentTab(i);

						yield return null;
					}
				}
			}
		}

		// add game slot info to the load buttons
		[OptionalPatch, HarmonyPatch(typeof(MainMenuLoadPanel), nameof(MainMenuLoadPanel.UpdateLoadButtonState))]
		public static class MainMenuLoadPanel_UpdateLoadButtonState_Patch
		{
			const string textPath = (Mod.Consts.isGameSN? "SaveDetails/": "") + "SaveGameLength";

			static bool Prepare() => Main.config.showSaveSlotID;

			static void Postfix(MainMenuLoadButton lb)
			{
				var textGO = lb.load.getChild(textPath);

				if (!textGO)
				{
					"MainMenuLoadPanel_UpdateLoadButtonState_Patch: text not found".logError();
					return;
				}
				var rt = textGO.transform as RectTransform;
				RectTransformExtensions.SetSize(rt, 190f, rt.rect.height);
				if (textGO.TryGetComponent<Text>(out var text))
					text.text += $" | {lb.saveGame}";
				else if (textGO.TryGetComponent<TextMeshProUGUI>(out var text2))
					text2.text += $" | {lb.saveGame}";
			}
		}

		// don't show messages while loading
		[OptionalPatch, HarmonyPatch(typeof(ErrorMessage), nameof(ErrorMessage.AddError))]
		static class ErrorMessage_AddError_Patch
		{
			static bool Prepare() => Main.config.hideMessagesWhileLoading;
			static bool Prefix() => !GameUtils.isLoadingState;
		}

#if BELOWZERO
		[OptionalPatch, PatchClass]
		static class MetalDetectorTargetSwitcher
		{
			static bool prepare() => Main.config.switchMetalDetectorTarget;

			static readonly string buttons = Strings.Mouse.scrollUp + "/" + Strings.Mouse.scrollDown;

			static void changeTarget(MetalDetector md, int dir)
			{
				if (dir != 0)
					md.targetTechTypeIndex = MathUtils.mod(md.targetTechTypeIndex + dir, md.detectableTechTypes.Count);
			}

			static string getCurrentTarget(MetalDetector md)
			{
				bool indexValid = MathUtils.isInRange(md.targetTechTypeIndex, md.detectableTechTypes.Count - 1);
				return !indexValid? "": Language.main.Get(md.detectableTechTypes[md.targetTechTypeIndex].AsString());
			}

			[HarmonyPostfix, HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemCommons))]
			static void TooltipFactory_ItemCommons_Postfix(StringBuilder sb, TechType techType, GameObject obj)
			{
				if (techType != TechType.MetalDetector)
					return;

				if (obj.GetComponent<MetalDetector>() is MetalDetector md && md.energyMixin?.charge > 0)
				{
					changeTarget(md, InputHelper.getMouseWheelDir());
					TooltipFactory.WriteDescription(sb, L10n.str("ids_metalDetectorTarget") + getCurrentTarget(md));
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemActions))]
			static void TooltipFactory_ItemActions_Postfix(StringBuilder sb, InventoryItem item)
			{
				if (item.item.GetTechType() == TechType.MetalDetector && item.item.GetComponent<MetalDetector>()?.energyMixin?.charge > 0)
					TooltipFactory.WriteAction(sb, buttons, L10n.str("ids_metalDetectorSwitchTarget"));
			}
		}
#endif // BELOWZERO
	}
}