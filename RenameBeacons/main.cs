using BepInEx;
using Common;
using Common.Harmony;

namespace RenameBeacons
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Main: BaseUnityPlugin
	{
		public void Awake()
		{
			HarmonyHelper.patchAll();
			LanguageHelper.init();
		}
	}

	class L10n: LanguageHelper
	{
		public static readonly string ids_name = "Name";
		public static readonly string ids_rename = "rename";
	}
}