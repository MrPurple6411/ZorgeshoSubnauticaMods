﻿using System;
using SMLHelper.V2.Options;

namespace Common.Configuration
{
	partial class Options: ModOptions
	{
		abstract class ModOption
		{
			public readonly string id;
			protected readonly string label;

			protected readonly Config.Field cfgField;

			public ModOption(Config.Field cf, string _label)
			{
				cfgField = cf;
				id = cfgField.name;
				label = _label;
			}

			abstract public void addOption(Options options);

			virtual public void onEvent(EventArgs e)
			{
				mainConfig?.save();
			}
		}
	}
}