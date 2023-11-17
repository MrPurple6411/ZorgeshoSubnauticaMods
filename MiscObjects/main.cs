using Common;
using Common.Harmony;
using Common.Crafting;
using BepInEx;

namespace MiscObjects
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Main: BaseUnityPlugin
	{
		internal static readonly ModConfig config = Mod.init<ModConfig>();

		public void Awake()
		{
			HarmonyHelper.patchAll(true);
			CraftHelper.patchAll();

			LanguageHelper.init();
		}
	}
}