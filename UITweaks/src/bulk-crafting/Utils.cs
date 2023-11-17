using Nautilus.Handlers;
using Common.Crafting;

#if BELOWZERO
using System.Linq;
using System.Collections.ObjectModel;

using HarmonyLib;

using Common.Harmony;
#endif

namespace UITweaks
{
	static partial class BulkCraftingTooltip
	{
		static bool isCraftingRequiresResources()
		{
#if SUBNAUTICA
			return GameModeUtils.RequiresIngredients();
#elif BELOWZERO
			return GameModeManager.GetOption<bool>(GameOption.CraftingRequiresResources);
#endif
		}

		static class TechInfoUtils
		{
			public static TechInfo getTechInfo(TechType techType)
			{
				return CraftDataHandler.GetRecipeData(techType);
			}

			public static void setTechInfo(TechType techType, TechInfo techInfo)
			{
#if SUBNAUTICA
				CraftData.techData[techType] = techInfo;
#elif BELOWZERO
				// for BZ we using TechDataPatches below
#endif
			}

#if BELOWZERO
			[OptionalPatch, PatchClass]
			static class TechDataPatches
			{
				static bool prepare() => Main.config.bulkCrafting.enabled;

				[HarmonyPriority(Priority.Low)]
				[HarmonyPrefix, HarmonyHelper.Patch(typeof(TechData), nameof(TechData.GetCraftAmount))]
				static bool TechData_GetCraftAmount_Prefix(TechType techType, ref int __result)
				{
					if (!isAmountChanged(techType))
						return true;

					__result = originalCraftAmount * currentCraftAmount;
					return false;
				}

				[HarmonyPriority(Priority.Low)]
				[HarmonyPrefix, HarmonyHelper.Patch(typeof(TechData), nameof(TechData.GetIngredients))]
				static bool TechData_GetIngredients_Prefix(TechType techType, ref ReadOnlyCollection<Ingredient> __result)
				{
					if (!isAmountChanged(techType))
						return true;

					__result = currentTechInfo.ingredients.Select(ing => new Ingredient(ing.techType, ing.amount)).ToList().AsReadOnly();
					return false;
				}
			}
#endif // BELOWZERO
		}
	}
}