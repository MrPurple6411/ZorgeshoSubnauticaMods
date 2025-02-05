﻿using System;
using Nautilus.Options;

namespace Common.Configuration
{
	using Reflection;

	partial class Options
	{
		partial class Factory
		{
			[FactoryPriority(FactoryPriority.LowerThanNormal)]
			class SliderOptionCreator: ICreator
			{
				public ModOption create(Config.Field cfgField)
				{
					// creating SliderOption if we have range for the field (from RangeAttribute or SliderAttribute)
					if (cfgField.type != typeof(float) && cfgField.type != typeof(int))
						return null;

					var rangeAttr  = cfgField.getAttr<Config.Field.RangeAttribute>();
					var sliderAttr = cfgField.getAttr<SliderAttribute>();

					// slider range can't be wider than field range
					float min = Math.Max(rangeAttr?.min ?? float.MinValue, sliderAttr?.minValue ?? float.MinValue);
					float max = Math.Min(rangeAttr?.max ?? float.MaxValue, sliderAttr?.maxValue ?? float.MaxValue);

					if (min == float.MinValue || max == float.MaxValue) // we need to have both bounds for creating slider
						return null;

					// in case of custom value type we add valueFormat in that component instead of SliderOption
					string valueFormat = sliderAttr?.customValueType == null? sliderAttr?.valueFormat: null;

					string label = cfgField.getAttr<FieldAttribute>()?.label;
					ModOption option = new SliderOption(cfgField, label, min, max, sliderAttr?.defaultValue, valueFormat);

					if (sliderAttr?.customValueType != null)
						option.addHandler(new Components.SliderValue.Add(sliderAttr.customValueType, sliderAttr.valueFormat));

					return option;
				}
			}
		}


		public class SliderOption: ModOption
		{
			readonly float  min, max;
			readonly float? defaultValue;
			readonly string valueFormat;

			public SliderOption(Config.Field cfgField, string label, float min, float max, float? defaultValue = null, string valueFormat = null): base(cfgField, label)
			{
				this.min = min;
				this.max = max;

				this.defaultValue = defaultValue;
				this.valueFormat  = valueFormat;
			}

			public override void addOption(Options options)
			{
				float value = cfgField.value.convert<float>();
				string format = Mod.Consts.isGameSN? valueFormat: (valueFormat ?? "{0:F0}");

				options.AddItem(ModSliderOption.Create(id, label, Math.Min(min, value), Math.Max(max, value), value, defaultValue, format, 0.001f));
			}

			public override void onValueChange(EventArgs e)
			{
				cfgField.value = (e as SliderChangedEventArgs)?.Value;
			}
#if BELOWZERO // fix for bug with tooltips on sliders
			static readonly Type textMeshProUGUI = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
			static readonly PropertyWrapper raycastTarget = textMeshProUGUI.property("raycastTarget").wrap();

			public override void onGameObjectChange(UnityEngine.GameObject go)
			{
				raycastTarget.set(go.getChild("Slider/Caption").GetComponent(textMeshProUGUI), true);
				base.onGameObjectChange(go);
			}
#endif
		}
	}
}