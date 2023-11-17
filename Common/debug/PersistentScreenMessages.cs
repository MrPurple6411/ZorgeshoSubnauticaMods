﻿using System;
using System.Reflection;

using HarmonyLib;

namespace Common
{
	using Harmony;
	using Reflection;

	static partial class Debug
	{
		/// messages with the same prefix will stay in the same message slot (also <see cref="StringExtensions.onScreen(string, string)"/>)
		public static void addMessage(string message, string prefix)
		{
			PersistentScreenMessages.patcher.patch();
			ErrorMessage.AddDebug($"[{prefix}] {message}");
		}

		static class PersistentScreenMessages
		{
			public static readonly HarmonyHelper.LazyPatcher patcher = new();

			static readonly FieldInfo messageEntry = typeof(ErrorMessage._Message).field("entry");
			static readonly PropertyWrapper text =
				Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro").property("text").wrap();

			[HarmonyPrefix]
			[HarmonyHelper.Patch(typeof(ErrorMessage), nameof(ErrorMessage._AddMessage))]
			[HarmonyHelper.Patch(HarmonyHelper.PatchOptions.PatchOnce)]
			static bool messagePatch(ErrorMessage __instance, string messageText)
			{
				if (messageText.isNullOrEmpty() || messageText[0] != '[')
					return true;

				int prefixEnd = messageText.IndexOf(']');

				if (prefixEnd > 0)
				{
					string prefix = messageText.Substring(0, prefixEnd + 1);
					var msg = __instance.messages.Find(m => m.messageText.startsWith(prefix));

					if (msg != null)
					{
						msg.timeEnd = GameUtils.time + __instance.timeFadeOut + __instance.timeDelay;
						text.set(messageEntry.GetValue(msg), messageText);

						return false;
					}
				}

				return true;
			}
		}
	}
}