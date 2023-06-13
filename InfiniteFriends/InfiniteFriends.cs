using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

namespace InfiniteFriends
{
    [BepInPlugin(Metadata.PLUGIN_GUID, Metadata.PLUGIN_NAME, Metadata.PLUGIN_VERSION)]
    [BepInProcess("SpiderHeckApp.exe")]
    internal class InfiniteFriends : BaseUnityPlugin
    {
        // Config
        public const int MaxPlayerHardCap = 32;

        public static InfiniteFriends Instance;
        internal static new ManualLogSource Logger { get; private set; }

        private InfiniteFriends()
        {
            InfiniteFriends.Instance = this;
        }

        protected void Awake()
        {
            InfiniteFriends.Logger = BepInEx.Logging.Logger.CreateLogSource(Metadata.PLUGIN_NAME_SHORT);

            try
            {
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Metadata.PLUGIN_GUID);
            }
            catch (Exception ex)
            {
                InfiniteFriends.Logger.LogError(ex.ToString());
            }
        }
    }
}
