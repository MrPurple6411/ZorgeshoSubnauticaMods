using Common;
using Common.Harmony;
using BepInEx;

namespace CustomHotkeys
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Main: BaseUnityPlugin
	{
		internal static readonly ModConfig config = Mod.init<ModConfig>();

		internal const string hotkeyConfigName = "hotkeys.json";
		internal static readonly HKConfig hkConfig = Mod.loadConfig<HKConfig>(hotkeyConfigName, Common.Configuration.Config.LoadOptions.ProcessAttributes);

		public void Awake()
		{
			HarmonyHelper.patchAll(true);

			if (config.addConsoleCommands)
				PersistentConsoleCommands.register<ConsoleCommands>();
		}
	}
}