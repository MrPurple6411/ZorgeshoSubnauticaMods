﻿using System;
using System.Linq;

using Common;
using Common.Crafting;
using Common.Reflection;
using TMPro;

#if SUBNAUTICA
using UnityEngine;
using UnityEngine.UI;
#endif

namespace UITweaks
{
	static partial class BulkCraftingTooltip
	{
#if SUBNAUTICA
		static TextMeshProUGUI text;
		static float textPosX;
#endif
		static TechType currentTechType;
		static TechInfo currentTechInfo, originalTechInfo;
		static int currentCraftAmount, currentCraftAmountMax;

		// originalTechInfo.craftAmount can be zero in case we using only linked items
		static int originalCraftAmount =>
			originalTechInfo == null? 0: (originalTechInfo.craftAmount == 0? 1: originalTechInfo.craftAmount);

		static PowerRelay currentPowerRelay;

		static string _writeAction(string key) =>
			$"\n<size=20><color=#ffffffff>{key}</color> - <color=#00ffffff>{L10n.str(L10n.ids_bulkCraftChangeAmount)}</color></size>";

		static readonly string[] actions =
		{
			"",
			_writeAction(Strings.Mouse.scrollUp),
			_writeAction(Strings.Mouse.scrollDown),
			_writeAction(Strings.Mouse.scrollUp + "/" + Strings.Mouse.scrollDown)
		};

		enum AmountActionHint { None = 0, Increase = 1, Decrease = 2, Both = 3 } // used as index for actions array
		static void init(TechType techType)
		{
			currentCraftAmount = 0;
			TechInfo techInfo = TechInfoUtils.getTechInfo(techType);
			currentCraftAmountMax = getMaxAmount(techInfo);

			if (currentCraftAmountMax == 0)
				return;

			currentCraftAmount = 1;
			currentTechType = techType;
			originalTechInfo = techInfo;
			currentTechInfo = new TechInfo(techInfo);
		}

		static bool isAmountChanged(TechType techType) => techType == currentTechType && currentCraftAmount > 1;

		// if EasyCraft mod is installed we will use it to get count of available ingredients
		static readonly MethodWrapper<Func<TechType, int>> EasyCraft_GetPickupCount =
			Type.GetType($"EasyCraft.ClosestItemContainers, EasyCraft{(Mod.Consts.isGameBZ? "_BZ": "")}")?.method("GetPickupCount").wrap<Func<TechType, int>>();

		static int getCountAvailable(TechType techType) => EasyCraft_GetPickupCount?.invoke(techType) ?? Inventory.main.GetPickupCount(techType);

		static int getMaxAmount(TechInfo techInfo)
		{
			int maxAmount = int.MaxValue;

			if (isCraftingRequiresResources())
			{
				foreach (var ing in techInfo.ingredients)
					maxAmount = Math.Min(maxAmount, getCountAvailable(ing.techType) / ing.amount);

				if (currentPowerRelay?.GetPower() > 5f)
					maxAmount = Math.Min(maxAmount, (int)(currentPowerRelay.GetPower() / 5f - 1f));
			}

			return maxAmount;
		}

		static void reset()
		{
			if (originalTechInfo != null)
				TechInfoUtils.setTechInfo(currentTechType, originalTechInfo);

			currentTechType = TechType.None;
			originalTechInfo = currentTechInfo = null;
		}

		static AmountActionHint getActionHint()
		{
			if		(currentCraftAmountMax <= 1)				  return AmountActionHint.None;
			else if (currentCraftAmount == 1)					  return AmountActionHint.Increase;
			else if (currentCraftAmount == currentCraftAmountMax) return AmountActionHint.Decrease;
			else												  return AmountActionHint.Both;
		}

		static string getActionText() => actions[(int)getActionHint()];

		static void changeAmount(int delta)
		{
			if (delta == 0 || currentCraftAmount == 0)
				return;

			if ((currentCraftAmount == 1 && delta == -1) || (currentCraftAmount == currentCraftAmountMax && delta == 1))
				return;

			currentCraftAmount += delta;

			TechInfo.Ing[] ingsCurrent = originalTechInfo.ingredients.Select(ing => new TechInfo.Ing(ing.techType, ing.amount * currentCraftAmount)).ToArray();

			currentTechInfo = new (ingsCurrent)
			{
				craftAmount = originalCraftAmount * currentCraftAmount,
				linkedItems = new (originalTechInfo.linkedItems)
			};

			TechInfoUtils.setTechInfo(currentTechType, currentTechInfo);
		}
	}
}