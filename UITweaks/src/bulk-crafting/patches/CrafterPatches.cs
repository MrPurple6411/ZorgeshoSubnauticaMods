using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;

using Common;
using Common.Harmony;
using Common.Crafting;

namespace UITweaks
{
	static partial class BulkCraftingTooltip
	{
		[OptionalPatch, PatchClass]
		static class CrafterPatches
		{
			static bool prepare() => Main.config.bulkCrafting.enabled;

			static readonly Dictionary<CrafterLogic, TechInfo> crafterCache = new();

			[HarmonyPriority(Priority.HigherThanNormal)] // just in case
			[HarmonyPrefix, HarmonyHelper.Patch(typeof(Crafter), nameof(Crafter.Craft))]
			static void craftFixDuration(TechType techType, ref float duration)
			{
				if (Main.config.bulkCrafting.changeCraftDuration && isAmountChanged(techType))
					duration *= currentCraftAmount;
			}

			[HarmonyPrefix, HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.Craft))]
			static void craftUpdateCache(CrafterLogic __instance, TechType techType)
			{
				if (isAmountChanged(techType))
					crafterCache[__instance] = currentTechInfo;
			}

			[HarmonyPostfix, HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.Craft))]
			static void craftFixAmount(CrafterLogic __instance, TechType techType)
			{
				if (isAmountChanged(techType) && originalTechInfo.craftAmount == 0)
					__instance.numCrafted = 0;
			}

			[HarmonyPriority(Priority.HigherThanNormal)]
			[HarmonyPrefix, HarmonyHelper.Patch(typeof(GhostCrafter), nameof(GhostCrafter.Craft))]
			static void craftFixEnergyConsumption(GhostCrafter __instance, TechType techType)
			{
				if (!Main.config.bulkCrafting.changePowerConsumption)
					return;

				if (isAmountChanged(techType))
					CrafterLogic.ConsumeEnergy(__instance.powerRelay, (currentCraftAmount - 1) * 5f); // and 5f also consumed in the vanilla method
			}

			[HarmonyPostfix, HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.ResetCrafter))]
			static void reset(CrafterLogic __instance) => crafterCache.Remove(__instance);

			[HarmonyTranspiler]
			[HarmonyHelper.Patch(typeof(CrafterLogic), nameof(CrafterLogic.TryPickupAsync))]
			[HarmonyHelper.Patch(HarmonyHelper.PatchOptions.PatchIteratorMethod)]
			static IEnumerable<CodeInstruction> fixLinkedItemCount(IEnumerable<CodeInstruction> cins)
			{
				var list = cins.ToList();

				CIHelper.MemberMatch stfld_numCrafted = new (OpCodes.Stfld, nameof(CrafterLogic.numCrafted));
				int index = list.ciFindIndexForLast(stfld_numCrafted, stfld_numCrafted);

				if (index == -1)
				{
					"fixLinkedItemCount: can't find stfld CrafterLogic.numCrafted".logError();

					// Logs all the instructions in the method
					cins.ToList().ForEach(ci => $"{ci}".logError());

					return cins;
				}

				return index == -1? cins:
					list.ciReplace(index - 1,
						OpCodes.Ldloc_1,
						CIHelper.emitCall<Func<CrafterLogic, int>>(_getNumCrafted));

				static int _getNumCrafted(CrafterLogic instance) =>
					crafterCache.TryGetValue(instance, out TechInfo info)? info.craftAmount: 1;
			}
		}
	}
}