using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Nezha : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Nezha;
    private const int Id = 32600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Nezha);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static readonly HashSet<byte> NezhaList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Nezha);
    }

    public override void Init()
    {
        NezhaList.Clear();
    }

    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (_Player == null || !exileIds.Contains(_Player.PlayerId)) return;
        var deathList = new List<byte>();
        var death = _Player;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (NezhaList.Contains(pc.PlayerId))
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(death);
                    deathList.Add(pc.PlayerId);
                }
            }
        }
        NezhaList.Clear();
        SendRPC();
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Torched, [.. deathList]);
    }

    private void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.WritePacked(NezhaList.Count);
        foreach (var playerId in NezhaList)
        {
            writer.Write(playerId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var count = reader.ReadPackedInt32();
        NezhaList.Clear();
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                NezhaList.Add(reader.ReadByte());
            }
        }
    }

    public override void OnVoted(PlayerControl votedPlayer, PlayerControl votedTarget)
    {
        if (votedPlayer.Is(CustomRoles.Nezha))
        {
            NezhaList.Add(votedTarget.PlayerId);
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        NezhaList.Clear();
    }
}

