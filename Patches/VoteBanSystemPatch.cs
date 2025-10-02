using Hazel;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Patches;

// From: https://github.com/Rabek009/MoreGamemodes/blob/master/Patches/ClientPatch.cs

[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.CmdAddVote))]
internal static class CmdAddVotePatch
{
    public static bool Prefix([HarmonyArgument(0)] int clientId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        PlayerControl pc = PlayerControl.LocalPlayer;
        PlayerControl target = Utils.GetClientById(clientId)?.Character;
        if (target != null) pc.GetRoleClass()?.OnVoteKick(pc, target);
        Logger.Info($" {pc.GetNameWithRole()} => {target.GetNameWithRole()}", "VoteKick");

        return false;
    }
}

[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
internal static class AddVotePatch
{
    public static bool Prefix(VoteBanSystem __instance, [HarmonyArgument(0)] int srcClient, [HarmonyArgument(1)] int clientId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        PlayerControl pc = Utils.GetClientById(srcClient)?.Character;
        PlayerControl target = Utils.GetClientById(clientId)?.Character;
        if (pc != null && target != null) pc.GetRoleClass()?.OnVoteKick(pc, target);
        Logger.Info($" {pc.GetNameWithRole()} => {target.GetNameWithRole()}", "VoteKick");

        if (AmongUsClient.Instance.ClientId == srcClient || __instance != VoteBanSystem.Instance) return false;

        VoteBanSystem.Instance = Object.Instantiate(AmongUsClient.Instance.VoteBanPrefab);
        AmongUsClient.Instance.Spawn(VoteBanSystem.Instance);

        _ = new LateTask(() =>
        {
            MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
            writer.StartMessage(5);
            writer.Write(AmongUsClient.Instance.GameId);
            writer.StartMessage(5);
            writer.WritePacked(__instance.NetId);
            writer.EndMessage();
            writer.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }, 0.5f);

        _ = new LateTask(() =>
        {
            AmongUsClient.Instance.RemoveNetObject(__instance);
            Object.Destroy(__instance.gameObject);
        }, 5f);

        return false;
    }
}
