﻿using UnityEngine;
using Nautilus.Handlers;

using Common;
using Common.Crafting;

namespace MiscObjects
{
	class StorageLabCounter: PoolCraftableObject
	{
		class L10n: LanguageHelper
		{
			public const string ids_CounterItem		= "Counter";
			public const string ids_CounterItemDesc	= "Counter with drawers.";

			public static string ids_DrawersInv	 = "DRAWERS";
			public static string ids_OpenDrawers = "Open drawers";
		}

		protected override TechInfo getTechInfo() => new (new TechInfo.Ing(TechType.Titanium, 4));

		public override void patch()
		{
			register(L10n.ids_CounterItem, L10n.ids_CounterItemDesc, TechType.LabCounter);

			addToGroup(TechGroup.Miscellaneous, TechCategory.Misc, TechType.LabCounter);

			if (Main.config.removeVanillaCounter)
				CraftDataHandler.RemoveFromGroup(TechGroup.Miscellaneous, TechCategory.Misc, TechType.LabCounter);

			setTechTypeForUnlock(TechType.LabCounter);
		}

		protected override void initPrefabPool()
		{
			addPrefabToPool(TechType.LabCounter);
			addPrefabToPool(TechType.SmallLocker, false);
		}

		protected override GameObject getGameObject(GameObject[] prefabs)
		{
			var prefab = prefabs[0];
			prefab.AddComponent<TechTag>(); // just in case

			Utils.addStorageToPrefab(prefab, prefabs[1]);
			PrefabUtils.initStorage(prefab, 7, 4, L10n.str("ids_OpenDrawers"), L10n.str("ids_DrawersInv"));

			return prefab;
		}
	}
}