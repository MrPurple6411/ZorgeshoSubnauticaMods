﻿using System.Collections.Generic;

using Common.Configuration;
using Common.Configuration.Actions;

namespace MiscPatches
{
#if DEBUG
	[Options.CustomOrder("QM")]
#endif
	[Field.BindConsole("misc")]
	class ModConfig: Config
	{
		[Options.Field("Gameplay patches", "Reload in order to apply")]
		public readonly bool gameplayPatches = false;

		public readonly float torpedoPunchForce = 30; //real default is 70, but in code default is 30

		public readonly float flareBurnTime = 300; // default is 1800
		public readonly float flareIntensity = 3;  // default is 6

		public readonly int maxSlotCountSeamoth = 8;
		public readonly int maxSlotCountPrawnSuit = 4; // and +2 for arms

		public readonly float vehicleLightEnergyPerSec = 0.1f;
		public readonly UnityEngine.KeyCode toggleLightKey = UnityEngine.KeyCode.F;

		public readonly float continuousDamageCheckInterval = 5f;
		public readonly float minHealthPercentForContinuousDamage = 0.3f;
		public readonly float chanceForDamage = 0.3f;
		public readonly float additionalContinuousDamage = 1f; // absolute value

		public readonly bool additionalPropRepImmunity = true; // propulsion/repulsion cannon immunity for some additional objects

		public readonly bool changeChargersSpeed = true;
		public readonly bool chargersAbsoluteSpeed = true; // charge speed is not linked to capacity (default false)
		public readonly float batteryChargerSpeed = 0.0015f; // 0.0015f
		public readonly float powerCellChargerSpeed = 0.0025f; // 0.0025f

		[Field.Action(typeof(UpdateOptionalPatches))]
		public readonly float cameraDroneNoiseRange = 250f;

		[Field.Action(typeof(UpdateOptionalPatches))]
		public readonly float cameraDroneMaxRange = 620f;

		[Field.Range(min: 0f)]
		[Options.Field("Objects movement step", "Use <i>setmovetarget</i> and <i>moveobject</i> console commands for moving constructed objects")]
		[Options.Choice("Moving is disabled", 0f, "0.01", 0.01f, "0.05", 0.05f, "0.1", 0.1f, "0.5", 0.5f, "1", 1f)]
		public readonly float objectsMoveStep = 0.1f;

		[Options.Field("First animations", "First use animations for tools and escape pod hatch cinematics")]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		public readonly bool firstAnimations = false;

		[Options.Field("Fix fog", "Fix fog underwater while in vehicles")]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		public readonly bool fixFog = false;

		[Options.Field("Builder repeat last tech", "Builder reusing last tech after constructing (vanilla behaviour)")]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		public readonly bool builderRepeat = true;

		[Options.Field("Ignore console for achievements", "Console usage disables achievements in vanilla")]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		public readonly bool ignoreConsoleForAchievements = false;

		[Field.Action(typeof(UpdateOptionalPatches))]
		public readonly bool useItemsOnPickup = false;

		[Field.Action(typeof(UpdateOptionalPatches))]
		public readonly bool useEatSounds = false;

#if GAME_BZ
		public readonly bool pickupNonEmptyStorages = true;

		[Field.Action(typeof(UpdateOptionalPatches))]
		public readonly bool fixGlow = true;
#endif
		public class Debug
		{
			public readonly bool buildAnywhere = true;
			public readonly UnityEngine.KeyCode forceBuildAllowKey = UnityEngine.KeyCode.V;

			[Options.Field("Loot reroll")]
			[Options.Choice("None", 0, "x10", 10, "x100", 100)]
			[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
			public readonly int lootSpawnRerollCount = 0;

			[Options.Field("Pause in ingame menu")]
			[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
			public readonly bool ingameMenuPause = true;

			[Options.Field("Override initial equipment", "Used for creative mode only")]
			[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
			public readonly bool overrideInitialEquipment = true;

			// can be changed with console command 'initial_equipment'
			[NoInnerFieldsAttrProcessing]
			public readonly Dictionary<TechType, int> initialEquipment = new();

			[Field.Action(typeof(UpdateOptionalPatches))]
			public readonly bool propulsionCannonIgnoreLimits = false;

			[Field.Action(typeof(UpdateOptionalPatches))]
			[Field.Action(typeof(VFXDestroyAfterSeconds_OnEnable_Patch.Purge))]
			public readonly bool keepParticleSystemsAlive = false;

			[Options.Field("Scanner room cheat")]
			[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
			public readonly bool scannerRoomCheat = false;

			[Options.Field("Show mouse raycast result")]
			[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
			public readonly bool showRaycastResult = false;

			public class FastStart
			{
#if GAME_SN
				class Hider: Options.Components.Hider.Simple
				{ public Hider(): base("dbg.fastStart.loadEscapePod", () => Main.config.dbg.fastStart.enabled) {} }

				[Field.Action(typeof(Hider))]
#endif
				[Options.Field("Fast start")]
				[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
				public readonly bool enabled = false;
#if GAME_SN
				[Options.Field("\tLoad escape pod")]
				[Options.Hideable(typeof(Hider))]
				public readonly bool loadEscapePod = false;
#endif
				public readonly string[] commandsAfterLoad =
				{
#if GAME_BZ
					"day"
#endif
				};
			}
			public readonly FastStart fastStart = new();
		}
		public readonly Debug dbg = new();
	}
}