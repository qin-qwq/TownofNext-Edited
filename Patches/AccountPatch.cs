using AmongUs.Data.Player;
using HarmonyLib;

namespace TOHE;

// Some of the below patches are from https://github.com/scp222thj/MalumMenu/
[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanPoints), MethodType.Setter)]
public static class RemoveDisconnectPenalty_PlayerBanData_BanPoints_Prefix
{
    /// <summary>
    /// Remove the time penalty after disconnecting from too many lobbies.
    /// </summary>
    /// <param name="__instance">The <c>PlayerBanData</c> instance.</param>
    /// <param name="value">The value being set to BanPoints.</param>
    /// <returns><c>false</c> to skip the original method, <c>true</c> to allow the original method to run.</returns>
    public static bool Prefix(PlayerBanData __instance, ref float value)
    {
        if (!(bool) (UnityEngine.Object) AmongUsClient.Instance || AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            return true;

        value = 0f;
        //__instance.BanPoints = 0f; // Remove all BanPoints
        return false;
    }
}