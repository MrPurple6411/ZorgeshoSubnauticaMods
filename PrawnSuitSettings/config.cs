﻿using Common;
using Common.Configuration;

namespace PrawnSuitSettings
{
	[AddToConsole("pss")]
	[Options.Name("Prawn Suit Settings")]
	class ModConfig: Config
	{
		public class CollisionSelfDamageSettings
		{
			class Hider: Field.IAction, Options.Components.Hider.IVisibilityChecker
			{
				public bool visible => Main.config.collisionSelfDamage.enabled;
				public void action() => Options.Components.Hider.setVisible("collision", visible);
			}

			[Options.Field("Damage from collisions", "Damage for Prawn Suit from collisions with terrain and other objects")]
			[HarmonyHelper.UpdatePatchesAction]
			[Field.Action(typeof(Hider))]
			[Field.Action(typeof(CollisionSelfDamage.SettingChanged))]
			public readonly bool enabled = false;

			[Options.Field("\tMinimum speed", "Prawn Suit minimum speed to get self damage from collision")]
			[Field.Range(0f, 50f)]
			[Options.Slider(DefaultValue: 20f)]
			[Options.Hideable(typeof(Hider), "collision")]
			[Field.Action(typeof(CollisionSelfDamage.SettingChanged))]
			public readonly float speedMinimumForDamage = 20f;

			[Options.Field("\tMirrored damage fraction", "Fraction of total inflicted collision damage that goes to self damage")]
			[Field.Range(0f, 1f)]
			[Options.Slider(DefaultValue: 0.1f, ValueFormat: "{0:P0}")]
			[Options.Hideable(typeof(Hider), "collision")]
			[Field.Action(typeof(CollisionSelfDamage.SettingChanged))]
			public readonly float mirroredSelfDamageFraction = 0.1f;
		}
		public readonly CollisionSelfDamageSettings collisionSelfDamage = new CollisionSelfDamageSettings();


		public class ArmsEnergyUsageSettings
		{
			class Hider: Field.IAction, Options.Components.Hider.IVisibilityChecker
			{
				public bool visible => Main.config.armsEnergyUsage.enabled;
				public void action() => Options.Components.Hider.setVisible("arms_energy", visible);
			}

			[Options.Field("Arms additional energy usage", "Energy consuming for drill arm and grappling arm")]
			[HarmonyHelper.UpdatePatchesAction]
			[Field.Action(typeof(Hider))]
			[Field.Action(typeof(ArmsEnergyUsage.SettingChanged))]
			public readonly bool enabled = false;

			[Options.Field("\tDrill arm", "Using drill arm costs that much energy units per second")]
			[Field.Range(0f, 5f)]
			[Options.Slider(DefaultValue: 0.3f, ValueFormat: "{0:F1}")]
			[Options.Hideable(typeof(Hider), "arms_energy")]
			[Field.Action(typeof(ArmsEnergyUsage.SettingChanged))]
			public readonly float drillArm = 0.3f;

			[Options.Field("\tGrappling arm (shoot)", "Shooting grappling hook costs that much energy units")]
			[Field.Range(0f, 5f)]
			[Options.Slider(DefaultValue: 0.5f, ValueFormat: "{0:F1}")]
			[Options.Hideable(typeof(Hider), "arms_energy")]
			[Field.Action(typeof(ArmsEnergyUsage.SettingChanged))]
			public readonly float grapplingArmShoot = 0.5f;

			[Options.Field("\tGrappling arm (pull)", "Using grappling arm to pull Prawn Suit costs that much energy units per second")]
			[Field.Range(0f, 5f)]
			[Options.Slider(DefaultValue: 0.2f, ValueFormat: "{0:F1}")]
			[Options.Hideable(typeof(Hider), "arms_energy")]
			[Field.Action(typeof(ArmsEnergyUsage.SettingChanged))]
			public readonly float grapplingArmPull = 0.2f;

			// vanilla energy usage
			public readonly float torpedoArm = 0f;
			public readonly float clawArm = 0.1f;
		}
		public readonly ArmsEnergyUsageSettings armsEnergyUsage = new ArmsEnergyUsageSettings();

		[Options.Field("Propulsion arm 'ready' animation", "Whether propulsion arm should play animation when pointed to something pickupable")]
		[HarmonyHelper.UpdatePatchesAction]
		public readonly bool readyAnimationForPropulsionCannon = true;

		[Options.Field("Toggleable drill arm", "Whether you need to hold mouse button while using drill arm")]
		[HarmonyHelper.UpdatePatchesAction]
		public readonly bool toggleableDrillArm = false;

		[Options.Field("Auto pickup resources after drilling", "Drilled resources will be added to the Prawn Suit storage automatically")]
		[HarmonyHelper.UpdatePatchesAction]
		public readonly bool autoPickupDrillableResources = true;
	}
}