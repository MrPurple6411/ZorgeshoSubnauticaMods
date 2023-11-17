﻿#if SUBNAUTICA
//#define CFG_UPDATE_V110
//#define CFG_UPDATE_V120
#define CFG_UPDATE_V131
#endif

using System;

using Common;
using Common.Configuration;
using Common.Configuration.Actions;

#if CFG_UPDATE_V120
using System.Linq;
#endif

#if CFG_UPDATE_V110 || CFG_UPDATE_V120 || CFG_UPDATE_V131
using Common.Reflection;
#endif

namespace DayNightSpeed
{
#if DEBUG
	[Options.CustomOrder("QM")]
#endif
	[Field.BindConsole("dns")] // warning: "dns" is used in daynightspeed command
	class ModConfig: Config
	{
		const float dayNightSecs = 1200f;

		[Slider_0_100, Field.Range(0f, 100f)] // for UI minimum is 0.01f
		[Field.Action(typeof(DayNightSpeedControl.SettingChanged))]
		[Options.Field("Day/night speed", tooltipType: typeof(Tooltips.DayNightSpeed))]
		public readonly float dayNightSpeed = 1.0f;

		[Options.Field("Use additional multipliers")]
		[Field.Action(typeof(SpeedsHider))]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		public readonly bool useAuxSpeeds = false;

		[Options.Field("Hunger/thirst", tooltipType: typeof(Tooltips.HungerThirst))]
		[Slider_0_100, Range_001_100, HideableSpeed]
		public readonly float speedHungerThirst = 1.0f;
		public float auxSpeedHungerThirst => useAuxSpeeds? speedHungerThirst: 1.0f;

		[Options.Field("Plants growth", tooltipType: typeof(Tooltips.Plants))]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		[Slider_0_100, Range_001_100, HideableSpeed]
		public readonly float speedPlantsGrow = 1.0f;

		[Options.Field("Eggs hatching", tooltipType: typeof(Tooltips.Eggs))]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		[Slider_0_100, Range_001_100, HideableSpeed]
		public readonly float speedEggsHatching = 1.0f;

		[Options.Field("Creatures growth", tooltipType: typeof(Tooltips.Creatures))]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		[Slider_0_100, Range_001_100, HideableSpeed]
		public readonly float speedCreaturesGrow = 1.0f;
#if SUBNAUTICA
		[Options.Field("Medkit fabrication", tooltipType: typeof(Tooltips.Medkit))]
		[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
		[Slider_0_100, Range_001_100, HideableSpeed]
		public readonly float speedMedkitInterval = 1.0f;
#endif
		[Options.Field("Charging/generating power", tooltipType: typeof(Tooltips.PowerCharge))]
		[Slider_0_100, Range_001_100, HideableSpeed]
		public readonly float speedPowerCharge = 1.0f;
		public float auxSpeedPowerCharge => useAuxSpeeds? speedPowerCharge: 1.0f;

		[Range_001_100]
		public readonly float speedPowerConsume = 1.0f;
		public float auxSpeedPowerConsume => useAuxSpeeds? speedPowerConsume: 1.0f;

		[Range_001_100, Field.Reloadable]
		[Field.Action(typeof(UpdateOptionalPatches))]
		public readonly float speedFoodDecay = 1.0f;

		[Range_001_100, Field.Reloadable]
		[Field.Action(typeof(UpdateOptionalPatches))]
		public readonly float speedFiltrationMachine = 1.0f;

		[Range_001_100, Field.Reloadable]
		public readonly float speedStillsuitWater = 1.0f;

		#region aux speeds hider
		class SpeedsHider: Options.Components.Hider.Simple
		{ public SpeedsHider(): base("speeds", () => Main.config.useAuxSpeeds) {} }

		class HideableSpeed: Options.HideableAttribute
		{ public HideableSpeed(): base(typeof(SpeedsHider), "speeds") {} }
		#endregion

		#region nonlinear slider
		class SliderValue_0_100: Options.Components.SliderValue.Nonlinear
		{
			SliderValue_0_100()
			{
				addValueInfo(0.5f, 1.0f, "{0:F2}", "{0:F1}");
				addValueInfo(0.7f, 3.0f);
				addValueInfo(0.8f, 10.0f, "{0:F1}", "{0:F0}");
			}

			public override float ConvertToDisplayValue(float value) => (float)Math.Round(base.ConvertToDisplayValue(value), 4);
		}

		class Range_001_100: Field.RangeAttribute
		{ public Range_001_100(): base(0.01f, 100f) {} }

		class Slider_0_100: Options.SliderAttribute
		{ public Slider_0_100(): base(defaultValue: 1.0f, minValue: 0.01f, customValueType: typeof(SliderValue_0_100)) {} }
		#endregion

		static class Tooltips
		{
			partial class L10n: LanguageHelper
			{
				public static readonly string ids_tooltipDays  = "{0}d";
				public static readonly string ids_tooltipHours = "{1}h";
				public static readonly string ids_tooltipMins  = "{2}min";
				public static readonly string ids_tooltipSecs  = "{3}sec";

				public static readonly string ids_tooltipTimePair  = "ingame <b>{0}</b> / realtime <b>{1}</b>";


				const string tagsTitle = "<size=" + (Mod.Consts.isGameSN? "22": "19") + "><color=#ffffffff>";
				const string tagsTitleEnd = "</color></size>";
				const string tagsSubtitle = "<b><color=#ffffffff>";
				const string tagsSubtitleEnd = "</color></b>";
				const string tagsLine = "<size=" + (Mod.Consts.isGameSN? "18": "15") + "><color=#dddedeff>";
				const string tagsLineEnd = "</color></size>";

				static string title(string str) => $"{tagsTitle}{str}{tagsTitleEnd}{Environment.NewLine}";
				static string subtitle(string str) => $"{tagsSubtitle}{str}: {tagsSubtitleEnd}";
				static string line(string str, bool last = false) => $"{tagsLine}{str}{tagsLineEnd}{(last? "": Environment.NewLine)}";

				public static readonly string ids_restartWarning = Environment.NewLine +
					"<color=yellow>restart to main menu to apply changes properly</color>";

				public static string toString(TimeSpan timeSpan)
				{
					string duration = "";
					if (timeSpan.Days > 0)	  duration += str(ids_tooltipDays);
					if (timeSpan.Hours > 0)	  duration += (duration == ""? "": " ") + str(ids_tooltipHours);
					if (timeSpan.Minutes > 0) duration += (duration == ""? "": " ") + str(ids_tooltipMins);

					if (duration == "")		  duration = str(ids_tooltipSecs);

					return duration.format(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
				}

				public static string toString(TimeSpan ingameTime, TimeSpan realTime)
				{
					return str(ids_tooltipTimePair).format(toString(ingameTime), toString(realTime));
				}
			}

			// converts realtime secs and speed to "ingame/realtime" string
			static string getDuration(float timeSecs, float speed)
			{
				return L10n.toString(TimeSpan.FromDays(timeSecs / dayNightSecs / speed),
									 TimeSpan.FromSeconds(timeSecs / Main.config.dayNightSpeed / speed));
			}

			#region tooltip: dayNightSpeed
			partial class L10n
			{
				public static readonly string ids_tooltipDayNightSpeed =
					title("Day/night cycle duration:") +
					line("ingame <b>24h</b> = realtime <b>{0}</b>", true);
			}

			public class DayNightSpeed: Options.Components.TooltipCached<float>
			{
				protected override bool needUpdate => isParamsChanged(Main.config.dayNightSpeed);

				public override string tooltip
				{
					get
					{
						double speed = Math.Round(Main.config.dayNightSpeed, Main.config.dayNightSpeed < 1.0f? 4: 1);
						TimeSpan timeSpan = TimeSpan.FromSeconds(dayNightSecs / speed);

						return L10n.str(L10n.ids_tooltipDayNightSpeed).format(L10n.toString(timeSpan));
					}
				}
			}
			#endregion

			public abstract class TooltipSpeed: Options.Components.TooltipCached<float, float>
			{
				protected bool isSpeedChanged(float speed) => isParamsChanged(Main.config.dayNightSpeed, speed);
			}

			#region tooltip: speedHungerThirst
			partial class L10n
			{
				public static readonly string ids_tooltipFoodWater =
					title("Food/water levels 100 to 0 time:") +
					line(subtitle("food") + "{0}") +
					line(subtitle("water") + "{1}", true);
			}

			public class HungerThirst: TooltipSpeed
			{
				const float foodTimeSecs  = 2520f;
				const float waterTimeSecs = 1800f;

				protected override bool needUpdate => isSpeedChanged(Main.config.speedHungerThirst);

				public override string tooltip =>
					L10n.str(L10n.ids_tooltipFoodWater).
						format(	getDuration(foodTimeSecs, Main.config.speedHungerThirst),
								getDuration(waterTimeSecs, Main.config.speedHungerThirst));
			}
			#endregion

			#region tooltip: speedPlantsGrow
			partial class L10n
			{
				public static readonly string ids_tooltipPlants =
					title("Plants growth time (e.g.):") +
					line(subtitle("mushrooms") + "{0}") +
					line(subtitle("marblemelon") + "{1}") +
					line(subtitle("creepvine") + "{2}") +
					line(subtitle(Mod.Consts.isGameSN? "bulbo tree": "horseshoe shrub") + "{3}", true);
			}

			public class Plants: TooltipSpeed
			{
				const float growMushrooms = Mod.Consts.isGameSN? 400f: 600f;
				const float growMelon     = 800f;
				const float growCreepvine = 1200f;
				const float growBulboTreeOrShrub = Mod.Consts.isGameSN? 1600f: 2000f;

				protected override bool needUpdate => isSpeedChanged(Main.config.speedPlantsGrow);

				public override string tooltip =>
					L10n.str(L10n.ids_tooltipPlants).
						format(	getDuration(growMushrooms, Main.config.speedPlantsGrow),
								getDuration(growMelon, Main.config.speedPlantsGrow),
								getDuration(growCreepvine, Main.config.speedPlantsGrow),
								getDuration(growBulboTreeOrShrub, Main.config.speedPlantsGrow));
			}
			#endregion

			#region tooltip: speedEggsHatching
			partial class L10n
			{
				public static readonly string ids_tooltipEggs =
					title("Eggs hatching time:") +
					line(subtitle("small creatures") + "{0}") +
					line(subtitle("big creatures") + "{1}", true);
			}

			public class Eggs: TooltipSpeed
			{
				const float hatchDaysSmallCreatures = 1f;
				const float hatchDaysBigCreatures = 1.5f;

				protected override bool needUpdate => isSpeedChanged(Main.config.speedEggsHatching);

				public override string tooltip =>
					L10n.str(L10n.ids_tooltipEggs).
						format(	getDuration(hatchDaysSmallCreatures * dayNightSecs, Main.config.speedEggsHatching),
								getDuration(hatchDaysBigCreatures * dayNightSecs, Main.config.speedEggsHatching));
			}
			#endregion

			#region tooltip: speedCreaturesGrow
			partial class L10n
			{
				public static readonly string ids_tooltipCreatures =
					title("Creatures growth time:") +
					line(subtitle("small creatures") + "{0}") +
					line(subtitle("big creatures") + "{1}", true);
			}

			public class Creatures: TooltipSpeed
			{
				const float growthDaysSmallCreatures = 1f;
				const float growthDaysBigCreatures = 1.5f;

				protected override bool needUpdate => isSpeedChanged(Main.config.speedCreaturesGrow);

				public override string tooltip =>
					L10n.str(L10n.ids_tooltipCreatures).
						format(	getDuration(growthDaysSmallCreatures * dayNightSecs, Main.config.speedCreaturesGrow),
								getDuration(growthDaysBigCreatures * dayNightSecs, Main.config.speedCreaturesGrow));
			}
			#endregion
#if SUBNAUTICA
			#region tooltip: speedMedkitInterval
			partial class L10n
			{
				public static readonly string ids_tooltipMedkit =
					title("Medkit fabrication time:") +
					line("{0}", true);
			}

			public class Medkit: TooltipSpeed
			{
				const float medkitSpawnIntervalSecs = 600f;

				protected override bool needUpdate => isSpeedChanged(Main.config.speedMedkitInterval);

				public override string tooltip =>
					L10n.str(L10n.ids_tooltipMedkit).
						format(	getDuration(medkitSpawnIntervalSecs, Main.config.speedMedkitInterval)) +
									(Options.mode == Options.Mode.IngameMenu? L10n.str(L10n.ids_restartWarning): "");
			}
			#endregion
#endif
			#region tooltip: speedPowerCharge
			partial class L10n
			{
				public static readonly string ids_tooltipPowerCharge =
					title("Time for generating 100 energy units (e.g.):") +
					line(subtitle("bioreactor") + "{0}") +
					line(subtitle("nuclear reactor") + "{1}", true);
			}

			public class PowerCharge: TooltipSpeed
			{
				const float gen100EnergyBReactorSecs = 100f / (1000f / dayNightSecs);
				const float gen100EnergyNReactorSecs = 100f / (5000f / dayNightSecs);

				protected override bool needUpdate => isSpeedChanged(Main.config.speedPowerCharge);

				static string _getDuration(float timeSecs) =>
					L10n.toString(TimeSpan.FromDays(timeSecs / dayNightSecs * Math.Min(Main.config.dayNightSpeed, 1f) / Main.config.speedPowerCharge),
								  TimeSpan.FromSeconds(timeSecs / Math.Max(Main.config.dayNightSpeed, 1f) / Main.config.speedPowerCharge));

				public override string tooltip =>
					L10n.str(L10n.ids_tooltipPowerCharge).
						format(	_getDuration(gen100EnergyBReactorSecs),
								_getDuration(gen100EnergyNReactorSecs));
			}
			#endregion
		}

		#region version updates
#if CFG_UPDATE_V110 || CFG_UPDATE_V120 || CFG_UPDATE_V131
		int __cfgVer = 0;
#endif
		protected override void onLoad()
		{
#if CFG_UPDATE_V110
			_updateTo110();
#endif
#if CFG_UPDATE_V120
			_updateTo120();
#endif
#if CFG_UPDATE_V131
			_updateTo131();
#endif
		}

		#region update to v1.1.0
#if CFG_UPDATE_V110
#pragma warning disable CS0414 // unused field
		// obsolete inverted multipliers (v1.0.0)
		[Field.LoadOnly, Field.Range(min:0.01f)] readonly float multHungerThrist   = 1.0f;
		[Field.LoadOnly, Field.Range(min:0.01f)] readonly float multPlantsGrow     = 1.0f;
		[Field.LoadOnly, Field.Range(min:0.01f)] readonly float multEggsHatching   = 1.0f;
		[Field.LoadOnly, Field.Range(min:0.01f)] readonly float multCreaturesGrow  = 1.0f;
		[Field.LoadOnly, Field.Range(min:0.01f)] readonly float multMedkitInterval = 1.0f;
#pragma warning restore CS0414

		// variables are renamed (mult* -> speed*) and inverted (new = 1.0f/old)
		void _updateTo110()
		{
			if (__cfgVer >= 110)
				return;

			__cfgVer = 110;

			try
			{
				// using reflection to avoid copy/paste and keep new params readonly
				foreach (var varName in new[] { "HungerThrist", "PlantsGrow", "EggsHatching", "CreaturesGrow", "MedkitInterval" })
				{
					float val = this.getFieldValue<float>("mult" + varName);

					if (val != 1.0f)
						this.setFieldValue("speed" + varName, 1.0f / val);
				}
			}
			catch (Exception e) { Log.msg(e); }
		}
#endif // CFG_UPDATE_V110
		#endregion

		#region update to v1.2.0
#if CFG_UPDATE_V120
		// if we loading this config for the first time and some speed variable is not default then we enabling useAuxSpeeds
		void _updateTo120()
		{
			if (__cfgVer >= 120)
				return;

			__cfgVer = 120;

			try
			{
				if (GetType().fields().Where(field => field.Name.StartsWith("speed") && !field.GetValue(this).Equals(1.0f)).Count() > 0)
					this.setFieldValue(nameof(useAuxSpeeds) , true);
			}
			catch (Exception e) { Log.msg(e); }
		}
#endif // CFG_UPDATE_V120
		#endregion

		#region update to v1.3.1
#if CFG_UPDATE_V131
#pragma warning disable CS0414 // unused field
		[Field.LoadOnly, Field.Range(0.01f, 100f)] readonly float speedHungerThrist = 1.0f;
#pragma warning restore CS0414

		// typo fixed (thrist => thirst)
		void _updateTo131()
		{
			if (__cfgVer >= 131)
				return;

			__cfgVer = 131;

			try
			{
				this.setFieldValue("speedHungerThirst", speedHungerThrist);
			}
			catch (Exception e) { Log.msg(e); }
		}
#endif // CFG_UPDATE_V131
		#endregion
		#endregion

		#region debug config
#if DEBUG
		[Options.Field]
		[Options.Hideable(typeof(Hider), "dbg")]
		public class DbgCfg
		{
			class Hider: Options.Components.Hider.Simple { public Hider(): base("dbg", () => Main.config.dbgCfg.enabled) {} }

			[Field.Action(typeof(Hider))]
			[Options.FinalizeAction(typeof(UpdateOptionalPatches))]
			[Options.Hideable(typeof(Options.Components.Hider.Ignore), "")]
			public readonly bool enabled = false;

			public readonly bool showGoals = false;
			public readonly bool ignoreGoals = false;
			public readonly bool showSurvivalStats = false;
			public readonly bool showToggleLightStats = false;
			public readonly bool showWaterParkCreatures = false;
		}
		public readonly DbgCfg dbgCfg = new();
#endif
		#endregion
	}
}