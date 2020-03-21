﻿using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Common
{
	static partial class Debug
	{
		public class Profiler: IDisposable
		{
			public static double lastResult { get; private set; } = 0d;
#if DEBUG
			public static int profilersCount { get; private set; } = 0;

			readonly string message = null;
			readonly string filename = null;
			readonly Stopwatch stopwatch = null;

			static string formatFileName(string filename)
			{
				if (filename.isNullOrEmpty())
					return filename;

				if (Path.GetExtension(filename) == "")
					filename += ".prf";

				return Paths.modRootPath + filename;
			}

			public Profiler(string _message, string _filename)
			{
				profilersCount++;

				message = _message;
				filename = formatFileName(_filename);

				stopwatch = Stopwatch.StartNew();
			}

			public void Dispose()
			{
				stopwatch.Stop();

				profilersCount--;
				assert(profilersCount >= 0);

				lastResult = stopwatch.Elapsed.TotalMilliseconds;

				if (message == null)
					return;

				string result = $"{message}: {lastResult} ms";
				$"PROFILER: {result}".log();

				if (filename != null)
					result.appendToFile(filename);
			}

			public static void _logCompare(double prevResult, string filename = null)
			{
				string res = $"PROFILER: DIFF {(lastResult - prevResult) / prevResult * 100f:F2} %";
				res.log();

				if (filename != null)
					res.appendToFile(formatFileName(filename));
			}
#else
			public Profiler(string _0, string _1) {}
			public void Dispose() {}
#endif
		}

		public static Profiler profiler(string message = null, string filename = null, bool allowNested = true) =>
#if DEBUG
			allowNested || Profiler.profilersCount == 0? new Profiler(message, filename): null;
#else
			null;
#endif
	}


	static partial class Debug
	{
		// based on code from http://www.csharp-examples.net/reflection-callstack/
		public static void logStack(string msg = "")
		{
			StackTrace stackTrace = new StackTrace();
			StackFrame[] stackFrames = stackTrace.GetFrames();

			StringBuilder output = new StringBuilder($"Callstack {msg}:{Environment.NewLine}");

			for (int i = 1; i < stackFrames.Length; i++) // dont print first item, it is "logStack"
			{
				MethodBase method = stackFrames[i].GetMethod();
				output.AppendLine($"\t{method.DeclaringType.Name}.{method.Name}");
			}

			output.ToString().log();
		}

		[Conditional("DEBUG")]
		public static void assert(bool condition, string message = null)
		{
			if (!condition)
			{
				string msg = $"Assertion failed: {message}";

				$"{msg}".logError();
				throw new Exception(msg);
			}
		}
	}
}