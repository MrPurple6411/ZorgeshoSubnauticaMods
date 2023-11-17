using Common;
using Common.Harmony;
using Common.Crafting;
using Common.Configuration;
using BepInEx;

namespace GravTrapImproved
{
	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Main: BaseUnityPlugin
	{
		internal static readonly ModConfig config = Mod.init<ModConfig>();
		internal static readonly TypesConfig typesConfig = Mod.loadConfig<TypesConfig>("types_config.json", Common.Configuration.Config.LoadOptions.ReadOnly | Common.Configuration.Config.LoadOptions.ProcessAttributes);

		public void Awake()
		{
			LanguageHelper.init();
			PersistentConsoleCommands.register<ConsoleCommands>();

			HarmonyHelper.patchAll(true);
			CraftHelper.patchAll();

			GravTrapObjectsType.init(typesConfig);
		}
	}
}