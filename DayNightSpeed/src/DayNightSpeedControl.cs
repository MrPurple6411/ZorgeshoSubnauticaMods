﻿using System;

using HarmonyLib;
using UnityEngine;

using Common;
using Common.Harmony;
using Common.Configuration;

#if DEBUG
using System.Collections.Generic;
#endif

namespace DayNightSpeed
{
	using Debug = Common.Debug;

	[PatchClass]
	static partial class DayNightSpeedControl
	{
		static GameObject gameObject;

		// need to force normal speed during most story events
		public static bool forcedNormalSpeed
		{
			get => _forcedNormalSpeed;

			set
			{
				Debug.assert(DayNightCycle.main);

				if (_forcedNormalSpeed == value || DayNightCycle.main.IsInSkipTimeMode() || DayNightCycle.main._dayNightSpeed == 0)
					return;

				_forcedNormalSpeed = value;																	$"forcedNormalSpeed: {_forcedNormalSpeed} current speed:{DayNightCycle.main._dayNightSpeed}".logDbg();
				DayNightCycle.main._dayNightSpeed = getActualSpeed();
			}
		}
		static bool _forcedNormalSpeed = false;

		[HarmonyPostfix, HarmonyPatch(typeof(DayNightCycle), nameof(DayNightCycle.Awake))]
		static void initDayNightCycle(DayNightCycle __instance)
		{
			__instance._dayNightSpeed = Main.config.dayNightSpeed;

			StoryGoalsListener.load(); // need load that after DayNightCycle is created

			// unregistering vanilla daynightspeed console command, replacing it with ours in DayNightSpeedControl
			DevConsole.commands.Remove("daynightspeed");
		}

		public static float getActualSpeed()
		{
			return forcedNormalSpeed? 1.0f: Main.config.dayNightSpeed;
		}

		public static float getSpeedClamped01()
		{
			Debug.assert(DayNightCycle.main);

			float speed = getActualSpeed();
			return (speed > float.Epsilon && speed < 1.0f)? speed: 1.0f;
		}

		// called after dayNightSpeed changed via options menu or console
		public class SettingChanged: Config.Field.IAction
		{
			public void action()
			{
				if (forcedNormalSpeed || !DayNightCycle.main)
					return;

				DayNightCycle.main._dayNightSpeed = Main.config.dayNightSpeed;
				DayNightCycle.main.skipTimeMode = false;
			}
		}

		class DayNightSpeedCommand: PersistentConsoleCommands
		{
			public void daynightspeed(float speed = 0f)
			{
				if (speed > 0f)
					DevConsole.SendConsoleCommand($"setcfgvar dns.{nameof(Main.config.dayNightSpeed)} {speed}");

				if (DayNightCycle.main)
					$"Day/night speed is {DayNightCycle.main.dayNightSpeed}".onScreen();
			}
		}

#if DEBUG
		// for debugging
		class DayNightSpeedWatch: MonoBehaviour
		{
			readonly HashSet<string> goals = new(); // for ignoring duplicates

			void Update()
			{
				if (!DayNightCycle.main || !Main.config.dbgCfg.enabled)
					return;

				string clr = forcedNormalSpeed? "<color=#00FF00FF>": "<color=#CCCCCCFF>";
				$"{clr}game:</color>{DayNightCycle.main.dayNightSpeed} <color=#CCCCCCFF>cfg:</color>{Main.config.dayNightSpeed}".onScreen("day/night speed");
				$"{DayNightCycle.main.timePassed:#.###}".onScreen("time passed");
				$"{DayNightCycle.ToGameDateTime(DayNightCycle.main.timePassedAsFloat)}".onScreen("date/time");

				if (!Main.config.dbgCfg.showGoals)
					return;

				// show current goals
				goals.Clear();
				foreach (var goal in Story.StoryGoalScheduler.main.schedule)
				{
					if (!goals.Add(goal.goalKey))
						continue;

					bool completed = StoryGoalsListener.isGoalCompleted(goal.goalKey);
					string goalName = $"<color={(completed? "#999900CC": "#FFFF00FF")}>{goal.goalKey}</color>";
					$"{goal.timeExecute - DayNightCycle.main.timePassed:#.###}".onScreen(goalName);
				}
			}
		}
#endif
		static bool inited = false;

		public static void init()
		{
			if (inited || !(inited = true))
				return;

			PersistentConsoleCommands.register<DayNightSpeedCommand>();
			gameObject = UnityHelper.createPersistentGameObject<StoryGoalsListener>("DayNightSpeedControl");
#if DEBUG
			gameObject.AddComponent<DayNightSpeedWatch>();
#endif
		}
	}

	// helper for transpilers
	static class CIUtils
	{
		public static CodeInstruction speed => CIHelper.emitCall<Func<float>>(DayNightSpeedControl.getActualSpeed);
		public static CodeInstruction speedClamped01 => CIHelper.emitCall<Func<float>>(DayNightSpeedControl.getSpeedClamped01);
	}
}