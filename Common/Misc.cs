﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#region Suppress messages
[assembly: SuppressMessage("Code Quality", "IDE0051", Scope = "namespaceanddescendants", Target = "Common")]
[assembly: SuppressMessage("Code Quality", "IDE0060", Scope = "namespaceanddescendants", Target = "Common")]
#endregion

namespace Common
{
	static class ObjectExtensions
	{
		static public int toInt(this object obj) => Convert.ToInt32(obj);
		static public bool toBool(this object obj) => Convert.ToBoolean(obj);
		static public float toFloat(this object obj) => Convert.ToSingle(obj);

		static public void setFieldValue(this object obj, FieldInfo field, object value)
		{
			try
			{
				field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
			}
			catch (Exception e)
			{
				Log.msg(e);
			}
		}
	}

	static class MiscExtensions
	{
		public static void forEach<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			if (sequence != null)
			{
				var enumerator = sequence.GetEnumerator();
				while (enumerator.MoveNext())
					action(enumerator.Current);
			}
		}
	}


	static class Debug
	{
		// based on code from http://www.csharp-examples.net/reflection-callstack/
		static public void logStack(string msg = "")
		{
			StackTrace stackTrace = new StackTrace();
			StackFrame[] stackFrames = stackTrace.GetFrames();
			
			string output = $"Callstack {msg}:" + Environment.NewLine;

			for (int i = 1; i < stackFrames.Length; ++i) // dont print first item, it is "printStack"
			{
				MethodBase method = stackFrames[i].GetMethod();
				output += $"\t{method.DeclaringType.Name}.{method.Name}" + Environment.NewLine;
			}

			output.log();
		}
	}
}
