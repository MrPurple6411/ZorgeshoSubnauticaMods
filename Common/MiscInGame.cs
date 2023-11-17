using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

#if SUBNAUTICA
	using Sprite = Atlas.Sprite;
#elif BELOWZERO
	using Sprite = UnityEngine.Sprite;
#endif

namespace Common
{
	static partial class StringExtensions
	{
		public static string onScreen(this string s)
		{
			if (!GameUtils.isLoadingState && Time.timeScale != 0f)
				ErrorMessage.AddDebug(s);

			return s;
		}

		// messages with the same prefix will stay in the same message slot
		public static string onScreen(this string s, string prefix) { Debug.addMessage(s, prefix); return s; }

		public static void onScreen(this List<string> list, string msg = "", int maxCount = 30)
		{
			var listToPrint = list.Count > maxCount? list.GetRange(0, maxCount): list;
			listToPrint.ForEach(s => ErrorMessage.AddDebug(msg + s));
		}

		public static string onScreen(this List<string> list, string prefix)
		{
			StringBuilder sb = new();
			list.ForEach(line => sb.AppendLine(line));
			return sb.ToString().onScreen(prefix);
		}
	}

	static class Strings
	{
		public static class Mouse
		{
			static string _str(int utf32) => $"<color=#ADF8FFFF>{char.ConvertFromUtf32(utf32)}</color>";

			public static readonly string rightButton	= _str(57404);
			public static readonly string middleButton	= _str(57405);
			public static readonly string scrollUp		= _str(57406);
			public static readonly string scrollDown	= _str(57407);
		}
	}

	static partial class SpriteHelper // extended in other Common projects
	{
		public static Sprite getSprite(object spriteID)
		{
			$"TechSpriteHelper.getSprite({spriteID.GetType()}) is not implemented!".logError();
			return SpriteManager.defaultSprite;
		}
	}

	static class GameUtils
	{
		// can't use vanilla GetVehicle in OnPlayerModeChange after 06.11 update :(
		public static Vehicle getVehicle(this Player player) => player? player.GetComponentInParent<Vehicle>(): null; // don't use null-conditional here

		public static TechType getHeldToolType() => Inventory.main?.GetHeld()?.GetTechType() ?? TechType.None;

		public static bool isLoadingState =>
			uGUI._main?.loading.loadingBackground?.sequence._target == true;

		// use that when needed (Time.time -> PDA.time in BZ)
		public static float time => PDA.time;
		public static void clearScreenMessages() => // expire all messages except QMM main menu messages
			ErrorMessage.main?.messages.Where(m => m.timeEnd - time < 1e3f).forEach(m => m.timeEnd = time - 1f);

		public static GameObject getTarget(float maxDistance)
		{
			Targeting.GetTarget(Player.main.gameObject, maxDistance, out GameObject result, out _);
			return result;
		}

		public static void setText(this HandReticle hand, string textUse = null, string textUseSubscript = null, string textHand = null, string textHandSubscript = null)
		{
			if (textUse != null)			hand.textUse = textUse;
			if (textHand != null)			hand.textHand = textHand;
			if (textUseSubscript != null)	hand.textUseSubscript = textUseSubscript;
			if (textHandSubscript != null)	hand.textHandSubscript = textHandSubscript;
		}

		// findNearest* methods are for use in non-performance critical code
		public static C findNearestToCam<C>(Predicate<C> condition = null) where C: Component =>
			UnityHelper.findNearest(LargeWorldStreamer.main?.cachedCameraPosition, out _, condition);

		public static C findNearestToPlayer<C>(Predicate<C> condition = null) where C: Component =>
			UnityHelper.findNearest(Player.main?.transform.position, out _, condition);

		public static C findNearestToPlayer<C>(out float distance, Predicate<C> condition = null) where C: Component =>
			UnityHelper.findNearest(Player.main?.transform.position, out distance, condition);
	}
}