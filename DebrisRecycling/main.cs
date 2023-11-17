using Common;
using Common.Harmony;
using Common.Crafting;
using Common.Configuration;
using BepInEx;

namespace DebrisRecycling
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Main: BaseUnityPlugin
	{
		internal const string prefabsConfigName = "prefabs_config.json";

		internal static readonly ModConfig config = Mod.init<ModConfig>();

		public void Awake()
		{
			HarmonyHelper.patchAll(true);
			CraftHelper.patchAll();

			LanguageHelper.init(); // after CraftHelper

			DebrisPatcher.init(Mod.loadConfig<PrefabsConfig>(prefabsConfigName, Common.Configuration.Config.LoadOptions.ProcessAttributes));
		}
	}
}