﻿using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Common;
using Common.Harmony;
using Common.Reflection;
using Common.Configuration;

namespace ConsoleImproved
{
	using Text = TMPro.TextMeshProUGUI;

	[HarmonyPatch(typeof(ErrorMessage), nameof(ErrorMessage.Awake))]
	static class ErrorMessageSettings
	{
		static ModConfig.MessagesSettings defaultSettings;

		public class RefreshSettings: Config.Field.IAction
		{ public void action() => refresh(); }

		public class RefreshTimeDelaySetting: Config.Field.IAction
		{
			public void action()
			{
				ErrorMessage.main.messages.Where(m => m.timeEnd - GameUtils.time < 1e3f).
										   ForEach(msg => msg.timeEnd = GameUtils.time + Main.config.msgsSettings.timeDelay);
			}
		}

		[HarmonyPriority(Priority.High)]
		static void Postfix(ErrorMessage __instance)
		{
			var em = __instance;
			var text = em.prefabMessage.GetComponent<Text>(); // for SUBNAUTICA it's just 'prefabMessage'

			defaultSettings ??= new ModConfig.MessagesSettings()
			{
				offset = em.offset.x,
				messageSpacing = em.ySpacing,
				timeFly = em.timeFlyIn,
				timeDelay = em.timeDelay,
				timeFadeOut = em.timeFadeOut,
				timeInvisible = em.timeInvisible,
				fontSize = (int)text.fontSize, // in BELOWZERO 'fontSize' is float
				textWidth = text.rectTransform.sizeDelta.x,
				textLineSpacing = text.lineSpacing
			};

			if (Main.config.msgsSettings.customize)
				refresh();
		}

		static void refresh()
		{
			var em = ErrorMessage.main;
			var settings = Main.config.msgsSettings.customize? Main.config.msgsSettings: defaultSettings;

			em.offset = new Vector2(settings.offset, settings.offset);
			em.ySpacing = settings.messageSpacing;
			em.timeFlyIn = settings.timeFly;
			em.timeDelay = settings.timeDelay;
			em.timeFadeOut = settings.timeFadeOut;
			em.timeInvisible = settings.timeInvisible;

			List<Text> texts = new (em.pool);
			em.messages.ForEach(message => texts.Add(message.entry));
			texts.Add(em.prefabMessage.GetComponent<Text>());

			if (sampleMessage)
				texts.Add(sampleMessage.GetComponent<Text>());

			foreach (var text in texts)
			{
				text.lineSpacing = settings.textLineSpacing;
				text.rectTransform.sizeDelta = new Vector2(settings.textWidth, 0f);
				text.fontSize = settings.fontSize;
			}
		}

		public static float messageSlotHeight
		{
			get
			{
				if (!ErrorMessage.main)
					return -1f;

				if (!sampleMessage) // using this to get real preferredHeight
				{
					sampleMessage = Object.Instantiate(ErrorMessage.main.prefabMessage.gameObject);
					sampleMessage.GetComponent<Text>().rectTransform.SetParent(ErrorMessage.main.messageCanvas);
					sampleMessage.SetActive(false);
				}

				return sampleMessage.GetComponent<Text>().preferredHeight + ErrorMessage.main.ySpacing;
			}
		}
		static GameObject sampleMessage;

		// get max message slots with current settings
		public static int getSlotCount(bool freeSlots)
		{
			static float _getYPos()
			{
				return ErrorMessage.main.GetYPos(-1, 1.0f);
			}

			if (!ErrorMessage.main)
				return -1;

			float lastMsgPos = freeSlots? _getYPos(): 0f;
			return (int)((ErrorMessage.main.messageCanvas.rect.height + lastMsgPos) / messageSlotHeight);
		}
	}

	// don't clear onscreen messages while console is open
	[OptionalPatch, HarmonyPatch(typeof(ErrorMessage), Mod.Consts.isGameBZ ? "OnUpdate" : "OnLateUpdate")]
	static class ErrorMessage_OnUpdate_Patch
	{
		static bool Prepare() => Main.config.keepMessagesOnScreen;

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins, ILGenerator ilg)
		{
			var list = cins.ToList();

			// is console visible
			void _injectStateCheck(int indexToInject, int indexToJump)
			{
				var label = list.ciDefineLabel(indexToJump, ilg);

				list.ciInsert(indexToInject,
					OpCodes.Ldsfld, typeof(DevConsole).field(nameof(DevConsole.instance)),
					OpCodes.Ldfld,  typeof(DevConsole).field(nameof(DevConsole.state)),
					OpCodes.Brtrue_S, label);
			}

			int[] i = list.ciFindIndexes(
				ci => ci.isLDC(7f), // -2, jump index
				new CIHelper.MemberMatch(OpCodes.Ldfld, nameof(ErrorMessage._Message.entry)), // for next Stloc_S
				ci => ci.isOp(OpCodes.Stloc_S), // local var 'entry'
				new CIHelper.MemberMatch($"set_{nameof(Transform.localPosition)}"), // +1, inject index
				new CIHelper.MemberMatch("SetAlpha")); // +1, jump index

			if (i == null)
				return cins;
#if BELOWZERO
			CIHelper.LabelClipboard.__enabled = false;

			// setting alpha to 1.0f
			list.ciInsert(i[4] + 1,
				OpCodes.Br, list.ciDefineLabel(i[4] + 1, ilg),
				OpCodes.Ldloc_S, list[i[2]].operand,
				OpCodes.Ldc_R4, 1.0f,
				OpCodes.Callvirt, typeof(Text).property("alpha").GetSetMethod());
#endif
			// ignoring alpha changes for message entry if console is visible (last two lines in the second loop)
			_injectStateCheck(i[3] + 1, i[4] + (Mod.Consts.isGameSN? 1: 2));

			// ignoring (time > message.timeEnd) loop if console is visible (just jumping to "float num = this.offsetY * 7f" line)
			_injectStateCheck(2, i[0] - 2);

			return list;
		}
	}
}