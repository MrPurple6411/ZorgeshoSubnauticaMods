﻿using HarmonyLib;
using Common.Harmony;

namespace PrawnSuitSettings
{
	// don't play propulsion cannon arm 'ready' animation when pointed on pickable object
	[OptionalPatch, HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.UpdateActive))]
	static class PropulsionCannon_UpdateActive_Patch
	{
		static bool Prepare() => !Main.config.readyAnimationForPropulsionCannon;

		static void Postfix(PropulsionCannon __instance)
		{
			if (Player.main.GetVehicle())
				__instance.animator.SetBool("cangrab_propulsioncannon", __instance.grabbedObject != null);
		}
	}
}