using Common;
using Common.Harmony;
using Common.Crafting;
using BepInEx;

namespace PrawnSuitSonarUpgrade
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Main: BaseUnityPlugin
	{
		public void Awake()
		{
			Mod.init();

			HarmonyHelper.patchAll();
			CraftHelper.patchAll();
		}
	}
}