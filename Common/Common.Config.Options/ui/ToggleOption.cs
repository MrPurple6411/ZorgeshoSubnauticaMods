﻿using System;
using Nautilus.Options;

namespace Common.Configuration
{
	using Reflection;

	partial class Options
	{
		partial class Factory
		{
			class ToggleOptionCreator: ICreator
			{
				public ModOption create(Config.Field cfgField)
				{
					if (cfgField.type != typeof(bool))
						return null;

					return new ToggleOption(cfgField, cfgField.getAttr<FieldAttribute>()?.label);
				}
			}
		}


		public class ToggleOption: ModOption
		{
			public ToggleOption(Config.Field cfgField, string label): base(cfgField, label) {}

			public override void addOption(Options options)
			{
				options.AddItem(ModToggleOption.Create(id, label, cfgField.value.convert<bool>()));
			}

			public override void onValueChange(EventArgs e)
			{
				cfgField.value = (e as ToggleChangedEventArgs)?.Value;
			}
		}
	}
}