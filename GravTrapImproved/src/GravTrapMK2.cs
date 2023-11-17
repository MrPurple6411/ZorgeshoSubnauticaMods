﻿using UnityEngine;
using Common.Crafting;

namespace GravTrapImproved
{
	class GravTrapMK2: PoolCraftableObject
	{
		public class Tag: MonoBehaviour
		{
			void Awake()
			{
				void _addDmgMod(DamageType damageType, float mod)
				{
					var dmgMod = gameObject.AddComponent<DamageModifier>();
					dmgMod.damageType = damageType;
					dmgMod.multiplier = mod;
				}

				_addDmgMod(DamageType.Collide, Main.config.mk2.dmgMod);
				_addDmgMod(DamageType.Fire, Main.config.mk2.heatDmgMod);
				_addDmgMod(DamageType.Heat, Main.config.mk2.heatDmgMod);
				_addDmgMod(DamageType.Acid, Main.config.mk2.acidDmgMod);
			}
		}

		public static new TechType TechType { get; private set; } = 0;

		protected override TechInfo getTechInfo() => new
		(
			new (TechType.Gravsphere),
			new (TechType.Titanium, 2),
			new (TechType.PowerCell),
			new (TechType.Aerogel),
			new (TechType.AdvancedWiringKit, 2)
		);

		protected override void initPrefabPool() => addPrefabToPool(TechType.Gravsphere);

		protected override GameObject getGameObject(GameObject prefab)
		{
			PrefabUtils.initVFXFab(prefab, posOffset: new Vector3(0f, 0.2f, 0f), scaleFactor: 0.7f);
			prefab.AddComponent<Tag>();

			return prefab;
		}

		public override void patch()
		{
			if (!Main.config.mk2.enabled)
				return;

			TechType = register(L10n.ids_GravTrapMK2, L10n.ids_GravTrapMK2Description, TechType.Gravsphere);

			addToGroup(TechGroup.Workbench, TechCategory.Workbench);
#if SUBNAUTICA
			CraftNodesCustomOrder.addNode(CraftTree.Type.Workbench, "GravTrap", L10n.ids_GravTrapMenu, "", "FinsMenu", TechType.Gravsphere);
			addCraftingNodeTo(CraftTree.Type.Workbench, "GravTrap", TechType.None);
#elif BELOWZERO
			addCraftingNodeTo(CraftTree.Type.Workbench, "");
#endif
			setItemSize(2, 2);
			setCraftingTime(5f);
			setEquipmentType(EquipmentType.Hand, QuickSlotType.Selectable);

			if (Main.config.mk2.fragmentCountToUnlock > 0)
				setFragmentToUnlock(TechType.GravSphereFragment, Main.config.mk2.fragmentCountToUnlock, 5f);
			else
				setTechTypeForUnlock(TechType.Gravsphere);
		}
	}
}