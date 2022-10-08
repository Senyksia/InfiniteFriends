using BepInEx.Logging;
using System.Diagnostics;

namespace InfiniteFriends.Patches
{
    public static class PatchLogger
    {
        private static readonly ManualLogSource logger = Logger.CreateLogSource(PluginInfo.PLUGIN_SHORT_NAME + "::Patches");

        [Conditional("DEBUG")]
        public static void LogDebug(object data)
        {
            logger.LogDebug(data);
        }

        public static void LogError(object data)
        {
            logger.LogError(data);
        }

        public static void LogInfo(object data)
        {
            logger.LogInfo(data);
        }

        public static void LogWarning(object data)
        {
            logger.LogWarning(data);
        }
    }
}
