using HarmonyLib;
using System.Collections.Generic;

namespace InfiniteFriends.Patches
{
    // Generate new `PlayerPingContainer`s to handle more than 4 players
    [HarmonyPatch(typeof(PlayerPingManager), nameof(PlayerPingManager.Instantiate))]
    class PlayerPingManager_Patch_Instantiate
    {
        [HarmonyPrefix]
        static bool Prefix(ref List<PlayerPingContainer> ___playerContainers, LobbyController ___lobbyController)
        {
            // Append the list with clones as necessary
            while (___playerContainers.Count < ___lobbyController.spawnedPlayers.Count)
            {
                PlayerPingContainer container = PlayerPingContainer.Instantiate(___playerContainers[0]);
                container.transform.SetParent(___playerContainers[0].transform.parent, false);
                ___playerContainers.Add(container);
            }
            
            return true;
        }
    }
}
