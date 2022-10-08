using BepInEx;
using HarmonyLib;
#if DEBUG
using HarmonyLib.Tools;
#endif
using System;
using System.Reflection;

namespace InfiniteFriends
{
    [BepInPlugin(PluginInfo.PLUGIN_ID, PluginInfo.PLUGIN_SHORT_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("SpiderHeckApp.exe")]
    public class InfiniteFriends : BaseUnityPlugin
    {
        // Config
        public const int MaxPlayerHardCap = 32;

        public void Awake()
        {
            try
            {
#if DEBUG
                HarmonyFileLog.Enabled = true;
#endif
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.PLUGIN_ID);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.ToString());
            }
        }
    }
}
