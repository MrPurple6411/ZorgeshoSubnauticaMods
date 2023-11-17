﻿using System.Collections;

using UnityEngine;
using Nautilus.Handlers;

using Common;
using Common.Crafting;

namespace DebrisRecycling
{
	class SalvageableDebrisDR: CraftableObject // just for scanner room
	{
		public static new TechType TechType { get; private set; } = 0;

		protected override TechInfo getTechInfo()  => null;
		public override GameObject getGameObject() => null;

		public override void patch() => TechType = register("ids_salvageableDebris", L10n.ids_salvageableDebris, TechType.ScrapMetal);
	}


	[CraftHelper.PatchFirst]
	class ScrapMetalSmall: PoolCraftableObject
	{
		public static new TechType TechType { get; private set; } = 0;

		protected override TechInfo getTechInfo() => null;

		public override void patch()
		{
			TechType = register(L10n.str(L10n.ids_smallScrapName), "", TechType.ScrapMetal);
			useTextFrom(descriptionFrom: TechType.ScrapMetal);
		}

		protected override void initPrefabPool()
		{
			addPrefabToPool(TechType.Titanium);
			addPrefabToPool(TechType.ScrapMetal, false);
		}

		protected override GameObject getGameObject(GameObject[] prefabs)
		{
			var prefab = prefabs[0];

			prefab.destroyComponent<ResourceTracker>();
			prefab.destroyChild("model/Titanium_small");

			int modelType = Random.value < 0.5f? 1: 2;
			var wreckage = prefabs[1].getChild(modelType == 1? "Model/Metal_wreckage_03_11": "Model/Metal_wreckage_03_10");
			prefab.getChild("model").createChild(wreckage, localPos: Vector3.zero, localAngles: new Vector3(-90f, 0f, 0f));

			GameObject collision = prefab.getChild("collision");
			collision.destroyComponent<SphereCollider>();

			var collider = collision.AddComponent<BoxCollider>();
			collider.center = modelType == 1? new Vector3(0f, 0.032f, -0.004f): new Vector3(0.007f, 0.128f, -0.005f);
			collider.size = modelType == 1? new Vector3(0.303f, 0.073f, 0.46f): new Vector3(0.832f, 0.331f, 0.681f);

			return prefab;
		}
	}


	// in case we not using dynamic titanium recipe
	abstract class TitaniumFromScrap: CraftableObject
	{
		readonly TechType sourceTech;
		readonly int sourceCount, resultCount;

		static readonly bool bulkCrafting = Mod.isModEnabled("UITweaks") && !Main.config.craftConfig._noBulk;

		public TitaniumFromScrap(TechType sourceTech, int sourceCount, int resultCount): base(nameof(TitaniumFromScrap) + resultCount)
		{
			this.sourceTech  = sourceTech;
			this.sourceCount = sourceCount;
			this.resultCount = resultCount;
		}

		public override GameObject getGameObject() => PrefabUtils.getPrefabCopy(TechType.Titanium);
		public override IEnumerator getGameObjectAsync(IOut<GameObject> gameObject) => PrefabUtils.getPrefabCopyAsync(TechType.Titanium, gameObject);

		protected override TechInfo getTechInfo()
		{
			TechInfo techInfo = new (new TechInfo.Ing(sourceTech, sourceCount));
			techInfo.linkedItems.add(TechType.Titanium, resultCount);

			return techInfo;
		}

		public override void patch()
		{
			if (Main.config.craftConfig.dynamicTitaniumRecipe || (sourceCount > 1 && bulkCrafting))
				return;

			if (!bulkCrafting)
				initNodes();

			register("Titanium" + (bulkCrafting? "": $" (x{resultCount})"), "", TechType.Titanium);
			useTextFrom(descriptionFrom: TechType.Titanium);

			setCraftingTime(0.7f * resultCount);
			unlockOnStart();

			if (bulkCrafting)
				addCraftingNodeTo(CraftTree.Type.Fabricator, "Resources/BasicMaterials", TechType.Titanium);
			else
				addCraftingNodeTo(CraftTree.Type.Fabricator, "Resources/Titanium");
		}

		static bool nodesInited = false;

		static void initNodes()
		{
			if (nodesInited || !(nodesInited = true))
				return;

			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, "Resources", "BasicMaterials", "Titanium");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "Titanium", "Titanium", SpriteManager.Get(TechType.Titanium), "Resources");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.Titanium, "Resources", "Titanium");
		}
	}

	// for CraftHelper patchAll
	class T1: TitaniumFromScrap  { public T1(): base(ScrapMetalSmall.TechType, 1, 1 * Main.config.craftConfig.titaniumPerSmallScrap) {} }
	class T5: TitaniumFromScrap  { public T5(): base(ScrapMetalSmall.TechType, 5, 5 * Main.config.craftConfig.titaniumPerSmallScrap) {} }
}