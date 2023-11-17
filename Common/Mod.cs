#define DISABLE_VERSION_CHECK_IN_DEVBUILD
namespace Common;

using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using Common.Utils;
using BepInEx.Bootstrap;
using System.Linq;

static partial class Mod
{
	public static bool isShuttingDown { get; private set; }
	class ShutdownListener: MonoBehaviour { void OnApplicationQuit() { isShuttingDown = true; "Shutting down".logDbg(); } }

	const string tmpFileName = "run the game to generate configs"; // name is also in the post-build.bat
	const string updateMessage = "An update is available! (current version is v<color=orange>{0}</color>, new version is v<color=orange>{1}</color>)";

	public static readonly string id = Assembly.GetExecutingAssembly().GetName().Name; // not using mod.json for ID
	public static string name { get { init(); return _name; } }
	static string _name;

	static bool inited;

	// supposed to be called before any other mod's code
	public static void init()
	{
		if (inited || !(inited = true))
			return;

		UnityHelper.createPersistentGameObject<ShutdownListener>($"{id}.ShutdownListener");

		// may be overkill to make it for all mods and from the start
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

		try { File.Delete(Paths.modRootPath + tmpFileName); }
		catch (UnauthorizedAccessException) {}

		var assembly = Assembly.GetExecutingAssembly();
		var myPluginInfo = Chainloader.PluginInfos.Values.First((x)=> x.Location == assembly.Location);

		_name = myPluginInfo.Metadata.Name;
		bool needCheckVer = VersionChecker.updateURLs.TryGetValue(myPluginInfo.Metadata.GUID, out string url);

#if DISABLE_VERSION_CHECK_IN_DEVBUILD
		if (needCheckVer && Consts.isDevBuild)
		{																											"Version check is disabled for dev build!".logDbg();
			needCheckVer = false;
		}
#endif
		if (needCheckVer)
		{
            Utils.Version currentVersion = new (myPluginInfo.Metadata.Version.ToString());
			var latestVersion = VersionChecker.getLatestVersion(url);							$"Latest version is {latestVersion}".logDbg();

			if (latestVersion > currentVersion)
				addCriticalMessage(updateMessage.format(currentVersion, latestVersion), color: "yellow");
		}

		"Mod inited".logDbg();
	}

	public static void addCriticalMessage(string msg, int size = MainMenuMessages.defaultSize, string color = MainMenuMessages.defaultColor)
	{
			MainMenuMessages.add(msg, size, color);
	}

	public static bool isModEnabled(string modID)
	{
		return Chainloader.PluginInfos.Any((x)=> x.Key == modID || x.Value.Metadata.Name == modID);
	}
}