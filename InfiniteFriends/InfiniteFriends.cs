using HarmonyLib;
using System;
using System.Reflection;
#if BEPINEX
using BepInEx;
#elif MODWEAVER
using modweaver.core;
#endif

namespace InfiniteFriends;

internal partial class InfiniteFriends
{
     public const int MAX_PLAYER_HARD_CAP = 32;

     public static InfiniteFriends Instance;
     internal new static ILogger Logger { get; private set; }

     public InfiniteFriends()
     {
         InfiniteFriends.Instance = this;
     }

     private static void HarmonyPatch(string harmonyInstanceId)
     {
         try
         {
             Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId);
         }
         catch (Exception ex)
         {
             InfiniteFriends.Logger.LogError(ex.ToString());
         }
     }
}

#if BEPINEX
[BepInPlugin(Metadata.PLUGIN_GUID, Metadata.PLUGIN_NAME, Metadata.PLUGIN_VERSION)]
[BepInProcess("SpiderHeckApp.exe")]
internal partial class InfiniteFriends : BaseUnityPlugin
{
    protected void Awake()
    {
        InfiniteFriends.Logger = new BepInExLogger(BepInEx.Logging.Logger.CreateLogSource(Metadata.PLUGIN_NAME_SHORT));
        InfiniteFriends.HarmonyPatch(Metadata.PLUGIN_GUID);
    }
}
#elif MODWEAVER
[ModMainClass]
internal partial class InfiniteFriends : Mod
{
    public override void Init()
    {
        InfiniteFriends.Logger = new ModWeaverLogger(base.Logger);
        InfiniteFriends.HarmonyPatch(this.Metadata.id);
    }

    public override void Ready() { }
    public override void OnGUI(ModsMenuPopup ui) { }
}
#endif
