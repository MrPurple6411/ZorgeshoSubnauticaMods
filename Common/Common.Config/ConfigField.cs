﻿using System.Reflection;

namespace Common.Configuration
{
	partial class Config
	{
		public partial class Field
		{
			protected readonly object config;
			protected readonly FieldInfo field;

			protected readonly ICustomAction action;

			public Field(object _config, FieldInfo _field)
			{
				config = _config;
				field = _field;

				action = field.getAttribute<CustomActionAttribute>()?.action;
			}

			public string name
			{
				get => field.Name;
			}

			public object value
			{
				get => field.GetValue(config);

				set => setFieldValue(value);
			}

			protected virtual void setFieldValue(object value)
			{
				config.setFieldValue(field, value);
				action?.customAction();
			}
		}
	}
}