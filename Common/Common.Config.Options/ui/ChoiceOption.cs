﻿using System;
using System.Linq;
using System.Collections.Generic;

using HarmonyLib;
using Nautilus.Options;

namespace Common.Configuration
{
	using Harmony;
	using Reflection;

	partial class Options
	{
		partial class Factory
		{
			class ChoiceOptionCreator: ICreator
			{
				public ModOption create(Config.Field cfgField)
				{
					if (cfgField.type.IsEnum) // add choice option for enum
					{
						var names  = Enum.GetNames(cfgField.type).Select(name => name.Replace('_', ' ')).ToArray();
						var values = Enum.GetValues(cfgField.type).OfType<object>().ToArray();

						return create(cfgField, cfgField.getAttr<FieldAttribute>()?.label, names, values);
					}

					if (cfgField.type == typeof(float) || cfgField.type == typeof(int)) // creating ChoiceOption if we also have choice attribute
					{
						if (cfgField.getAttr<ChoiceAttribute>() is ChoiceAttribute choice && choice.choices.Length > 0)
							return create(cfgField, cfgField.getAttr<FieldAttribute>()?.label, choice.choices, choice.values);
					}

					return null;
				}

				static ChoiceOption create(Config.Field cfgField, string label, string[] choices, object[] values)
				{
					if (cfgField.checkAttr<ChoiceMasterAttribute>())
						return new ChoiceMasterOption(cfgField, label, choices, values);
					else
						return new ChoiceOption(cfgField, label, choices, values);
				}
			}
		}


		public class ChoiceOption: ModOption
		{
			readonly string[] choices;
			readonly object[] values;

			public ChoiceOption(Config.Field cfgField, string label, string[] choices, object[] values = null): base(cfgField, label)
			{
				this.choices = choices;
				this.values  = values;

				// adds choice labels to LanguageHandler, changing array in the process
				for (int i = 0; i < choices.Length; i++)
					registerLabel($"{id}.{i}", ref choices[i]);

				if (id.IndexOf('.') != -1)
					ValidatorPatch.patcher.patch();
			}

			public override void addOption(Options options)
			{
				int defaultIndex = values?.findIndex(val => val.Equals(cfgField.value) || val.Equals(cfgField.value.convert<int>())) ?? cfgField.value.convert<int>();
				options.AddItem(ModChoiceOption<string>.Create(id, label, choices, defaultIndex < 0? 0: defaultIndex));
			}

			public override void onValueChange(EventArgs e)
			{
				int? index = (e as ChoiceChangedEventArgs<string>)?.Index;
				cfgField.value = values?[index ?? 0] ?? index;
			}


			// for some reason SMLHelper doesn't allow periods in ChoiceOption's id and we need them for nested classes
			static class ValidatorPatch
			{
				public static readonly HarmonyHelper.LazyPatcher patcher = new();

				[HarmonyPrefix]
				[HarmonyHelper.Patch(HarmonyHelper.PatchOptions.PatchOnce)]
				[HarmonyHelper.Patch("Nautilus.Options.Utility.Validator, SMLHelper", "ValidateID", typeof(string))]
				static bool validatorPrefix(string id) => id.IndexOf('.') == -1;
			}
		}

		/// choice option that can change other fields along with its own field
		/// values for other fields are added in <see cref="ChoiceMasterAttribute"/see>
		/// for now, works only with fields in the same class as option
		public class ChoiceMasterOption: ChoiceOption
		{
			// key - choice's value, value - list of pairs of field and field's new value for key value
			readonly Dictionary<object, List<(Config.Field, object)>> dependants = new();

			public ChoiceMasterOption(Config.Field cfgField, string label, string[] choices, object[] values = null): base(cfgField, label, choices, values)
			{
				cfgField.getAttrs<ChoiceMasterAttribute>().forEach(attr => dependants[attr.choiceValue] = convert(attr.dependants));
			}

			public override void onValueChange(EventArgs e)
			{
				base.onValueChange(e);

				if (dependants.TryGetValue(cfgField.value, out var fields))
					fields.ForEach(field => field.Item1.value = field.Item2);
			}

			List<(Config.Field, object)> convert(List<(string, object)> list)
			{
				Debug.assert(validate(list));
				return list.Select(tuple => (new Config.Field(cfgField.parent, tuple.Item1, cfgField.rootConfig), tuple.Item2)).ToList();
			}

			bool validate(List<(string, object)> list)
			{
				foreach (var tuple in list)
					Debug.assert(cfgField.parent.getFieldValue(tuple.Item1) != null, $"ChoiceMasterOption ({id}): invalid field '{tuple.Item1}'");

				return true;
			}
		}
	}
}