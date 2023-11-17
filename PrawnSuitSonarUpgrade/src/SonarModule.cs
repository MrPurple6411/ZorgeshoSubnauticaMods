﻿using Common;
using Common.Crafting;

namespace PrawnSuitSonarUpgrade
{
	class PrawnSonarModule: PoolCraftableObject
	{
		public static new TechType TechType { get; private set; } = 0;

		protected override TechInfo getTechInfo() => new
		(
#if SUBNAUTICA
			new (TechType.SeamothSonarModule),
			new (TechType.WiringKit),
			new (TechType.ComputerChip)
#elif BELOWZERO
			new (TechType.CopperWire),
			new (TechType.Magnetite, 2)
#endif
		);

		protected override void initPrefabPool() => addPrefabToPool(TechType.ExosuitJetUpgradeModule);

		public override void patch()
		{
			TechType = register("Prawn suit sonar", Mod.Consts.isGameSN? "Seamoth sonar modified to use on prawn suit.": "Prawn suit sonar");
#if SUBNAUTICA
			addToGroup(TechGroup.Workbench, TechCategory.Workbench);
			addCraftingNodeTo(CraftTree.Type.Workbench, "ExosuitMenu");
			setTechTypeForUnlock(TechType.SeamothSonarModule);
#elif BELOWZERO
			addToGroup(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades, TechType.ExosuitJetUpgradeModule);
			addCraftingNodeTo(CraftTree.Type.Fabricator, "Upgrades/ExosuitUpgrades", TechType.ExosuitJetUpgradeModule);
			unlockOnStart();
#endif
			setEquipmentType(EquipmentType.ExosuitModule, QuickSlotType.SelectableChargeable);
		}
	}
}