using HarmonyLib;
using UnityEngine;

using Common;

namespace DebrisRecycling
{
	[HarmonyPatch(typeof(BuilderTool), nameof(BuilderTool.Construct))]
	static class BuilderTool_Construct_Patch
	{
		// don't allow construct debris back
		static bool Prefix(Constructable c, bool state) => !(state && c.gameObject.GetComponent<DebrisDeconstructable>());
	}

	[HarmonyPatch(typeof(BuilderTool), nameof(BuilderTool.OnHover), typeof(Constructable))]
	static class BuilderTool_OnHover_Patch
	{
		static bool Prefix(BuilderTool __instance, Constructable constructable)
		{
			if (!constructable.gameObject.GetComponent<DebrisDeconstructable>())
				return true;

			HandReticle hand = HandReticle.main;
			string text = L10n.str("ids_salvageableDebris") + (Mod.Consts.isDevBuild? $" ({constructable.gameObject.name})": "");
			hand.SetText(HandReticle.TextType.Use, text, false, GameInput.Button.Deconstruct);

			if (!constructable.constructed)
			{
				hand.SetProgress(constructable.amount);
				hand.SetIcon(HandReticle.IconType.Progress, 1.5f);
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(BuilderTool), nameof(BuilderTool.HandleInput))]
	static class BuilderTool_HandleInput_Patch
	{
		static void Prefix(BuilderTool __instance)
		{
			if (__instance.isDrawn && !Builder.isPlacing && AvatarInputHandler.main.IsEnabled() && GameUtils.getTarget(10f) is GameObject go)
				DebrisPatcher.processObject(go);
		}
	}
}