﻿using UnityEngine;
using Nautilus.Crafting;
using Nautilus.Handlers;

using Common.Crafting;

namespace OxygenRefill
{
	[CraftHelper.PatchFirst]
	class OxygenRefillStation: PoolCraftableObject
	{
		public static ModCraftTreeRoot treeRootNode;
		CraftTree.Type treeType;

		protected override TechInfo getTechInfo() => new
		(
			new (TechType.Titanium, 2),
			new (TechType.CopperWire, 2),
			new (TechType.AdvancedWiringKit, 1)
		);

		public override void patch()
		{
			register(L10n.ids_OxygenStation, L10n.ids_OxygenStationDesc, TechType.Workbench);
			treeType = EnumHandler.AddEntry<CraftTree.Type>(ClassID).CreateCraftTreeRoot(out treeRootNode);

			addToGroup(TechGroup.InteriorModules, TechCategory.InteriorModule, TechType.Workbench);
			unlockOnStart();
		}

		protected override void initPrefabPool() => addPrefabToPool(TechType.Workbench);

		protected override GameObject getGameObject(GameObject prefab)
		{
			GhostCrafter crafter = prefab.GetComponent<Workbench>();
			crafter.craftTree = treeType;
			crafter.handOverText = L10n.str("ids_UseStation");

			prefab.GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.cyan;

			return prefab;
		}
	}

	abstract class TankRefill: PoolCraftableObject
	{
		// used for fill tank after creating at refilling station (we can't just change it in prefab)
		class RefillOxygen: MonoBehaviour
		{
			void Awake()
			{
				float capacity = Main.config.getTankCapacity(gameObject);

				if (capacity > 0)
				{
					Oxygen oxygen = gameObject.GetComponent<Oxygen>();
					oxygen.oxygenAvailable = oxygen.oxygenCapacity = capacity;
				}

				Destroy(this);
			}
		}

		readonly float craftingTime;
		readonly TechType tankType;

		public TankRefill(TechType tankType, float craftingTime): base($"{tankType}_Refill")
		{
			this.tankType = tankType;
			this.craftingTime = craftingTime;
		}

		protected override TechInfo getTechInfo() => new (new TechInfo.Ing(tankType));

		protected override void initPrefabPool() => addPrefabToPool(tankType);

		protected override GameObject getGameObject(GameObject prefab)
		{
			prefab.AddComponent<RefillOxygen>();

			return prefab; // using this as exact prefab, so no need in LinkedItems
		}

		public override void patch()
		{
			register(L10n.ids_RefillOxygen, L10n.ids_RefillOxygenDesc, tankType);

			addCraftingNodeTo(OxygenRefillStation.treeRootNode);
			setTechTypeForUnlock(tankType);
			setCraftingTime(craftingTime);
			useExactPrefab();
		}
	}

	// for CraftHelper patchAll
	class T1: TankRefill { public T1(): base(TechType.Tank, 5f) {} }
	class T2: TankRefill { public T2(): base(TechType.DoubleTank, 10f) {} }
	class T3: TankRefill { public T3(): base(TechType.PlasteelTank, 10f) {} }
	class T4: TankRefill { public T4(): base(TechType.HighCapacityTank, 15f) {} }
#if BELOWZERO
	class T5: TankRefill { public T5(): base(TechType.SuitBoosterTank, 10f) {} }
#endif
}