using HarmonyLib;

using Common;
using Common.Harmony;

#if SUBNAUTICA
using UnityEngine;
using UnityEngine.UI;
#endif

namespace UITweaks
{
	static partial class BulkCraftingTooltip
	{
		[OptionalPatch, PatchClass]
		static class TooltipPatches
		{
			static bool prepare()
			{
				return Main.config.bulkCrafting.enabled;
			}

			static CraftTree.Type currentTreeType;
#if SUBNAUTICA
			// prevents Nautilus from restoring techdata to original state (for modded items)
			[HarmonyPrefix, HarmonyHelper.Patch("Nautilus.Patchers.CraftDataPatcher, Nautilus", "NeedsPatchingCheckPrefix")]
			static bool NautilusPatchCheck(TechType techType) => currentTechType != techType || !CraftData.techData.ContainsKey(techType);
#endif
			[HarmonyPostfix, HarmonyPatch(typeof(uGUI_Tooltip), nameof(uGUI_Tooltip.OnUpdate))]
			static void checkVisible()
			{
				if (!uGUI_Tooltip.visible && currentTechType != TechType.None)
					reset();
			}

			[HarmonyPrefix, HarmonyPatch(typeof(uGUI_CraftingMenu), nameof(uGUI_CraftingMenu.Open))]
			static void openCraftingMenu(CraftTree.Type treeType, ITreeActionReceiver receiver)
			{
				currentTreeType = treeType;
				currentPowerRelay = Main.config.bulkCrafting.changePowerConsumption? (receiver as GhostCrafter)?.powerRelay: null;
			}

			[HarmonyPostfix, HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.CraftRecipe))]
			static void updateRecipe(TechType techType, TooltipData data)
			{
				if (currentTreeType == CraftTree.Type.Constructor)
					return;

				if (techType != currentTechType)
					reset();

				if (currentTechType == TechType.None)
					init(techType);

				changeAmount(InputHelper.getMouseWheelDir());
				string action = getActionText();
				if (action != "")
					data.postfix.AppendLine(action);
			}
//#if SUBNAUTICA
//			[HarmonyPostfix, HarmonyPatch(typeof(uGUI_Tooltip), nameof(uGUI_Tooltip.Rebuild))]
//			static void rebuildTooltip(uGUI_Tooltip __instance, CanvasUpdate executing) // TODO BRANCH_EXP: most of this code is unneeded on exp branch
//			{
//				const float tooltipOffsetX = 30f;

//				if (text.text == "" || executing != CanvasUpdate.Layout)
//					return;

//				float tooltipHeight = -__instance.rectTransform.rect.y;
//				float textHeight = text.rectTransform.sizeDelta.y;
//				__instance.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight + textHeight);

//				float tooltipWidth = __instance.rectTransform.rect.xMax;
//				float textWidth = text.rectTransform.sizeDelta.x + tooltipOffsetX;
//				if (tooltipWidth < textWidth)
//					__instance.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);

//				float textPosY = __instance.iconCanvas.transform.localPosition.y -__instance.iconCanvas.rectTransform.sizeDelta.y;
//				text.rectTransform.localPosition = new Vector2(textPosX, textPosY);
//			}
//#endif
		}
	}
}