using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
#if DEBUG
using HarmonyLib.Tools;
#endif
using System;
using System.Reflection;

namespace InfiniteFriends
{
    [BepInPlugin(PluginInfo.PLUGIN_ID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("SpiderHeckApp.exe")]
    public class InfiniteFriends : BaseUnityPlugin
    {
        // Config
        public const int MaxPlayerHardCap = 32;
        public static ManualLogSource logger;

        public void Awake()
        {
#if DEBUG
            HarmonyFileLog.Enabled = true;
#endif
            logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_SHORT_NAME);

            try
            {
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.PLUGIN_ID);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
            }
        }
    }
}
