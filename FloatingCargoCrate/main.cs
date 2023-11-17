using Common;
using Common.Harmony;
using Common.Crafting;
using BepInEx;

namespace FloatingCargoCrate
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Main: BaseUnityPlugin
	{
		internal static readonly ModConfig config = Mod.init<ModConfig>();

		public void Awake()
		{
			LanguageHelper.init();

			HarmonyHelper.patchAll();
			CraftHelper.patchAll();
		}
	}
}