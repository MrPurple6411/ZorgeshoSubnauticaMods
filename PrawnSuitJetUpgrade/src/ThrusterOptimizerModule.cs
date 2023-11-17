﻿using Common.Crafting;

namespace PrawnSuitJetUpgrade
{
	class PrawnThrustersOptimizer: PoolCraftableObject
	{
		public static new TechType TechType { get; private set; } = 0;

		protected override TechInfo getTechInfo() => new
		(
			new (TechType.AdvancedWiringKit),
			new (TechType.Sulphur, 3),
			new (TechType.Aerogel, 2),
			new (TechType.Polyaniline)
		);

		protected override void initPrefabPool() => addPrefabToPool(TechType.ExosuitJetUpgradeModule);

		public override void patch()
		{
			TechType = register(L10n.ids_optimizerName, L10n.ids_optimizerDesc);

			addToGroup(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades, TechType.ExosuitJetUpgradeModule);
			addCraftingNodeTo(CraftTree.Type.SeamothUpgrades, "ExosuitModules", TechType.ExosuitJetUpgradeModule);
#if BELOWZERO
			addCraftingNodeTo(CraftTree.Type.Fabricator, "Upgrades/ExosuitUpgrades", TechType.ExosuitJetUpgradeModule);
#endif
			setEquipmentType(EquipmentType.ExosuitModule, QuickSlotType.Passive);
			setTechTypeForUnlock(TechType.ExosuitJetUpgradeModule);
		}
	}
}