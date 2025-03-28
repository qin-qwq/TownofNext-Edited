using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

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
    private static OptionItem EnableAwakening;
    private static OptionItem ProgressPerTask;
    private static OptionItem ProgressPerSecond;
    private static OptionItem BomberRadius;

    private static float AwakeningProgress;
    private static bool IsAwakened;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Nezha);
        EnableAwakening = BooleanOptionItem.Create(Id + 10, "EnableAwakening", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nezha]);
        ProgressPerTask = FloatOptionItem.Create(Id + 11, "ProgressPerTask", new(0f, 100f, 10f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 12, "ProgressPerSecond", new(0.1f, 3f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        BomberRadius = FloatOptionItem.Create(Id + 13, "BomberRadius", new(0.5f, 100f, 0.5f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Init()
    {
        AwakeningProgress = 0;
        IsAwakened = false;
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

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!EnableAwakening.GetBool() || AwakeningProgress >= 100) return string.Empty;
        return string.Format(GetString("AwakeningProgress") + ": {0:F0}% / {1:F0}%", AwakeningProgress, 100);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (AwakeningProgress < 100)
        {
            AwakeningProgress += ProgressPerSecond.GetFloat() * Time.fixedDeltaTime;
        }
        else CheckAwakening(player);
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!IsAwakened)
        {
            AwakeningProgress += ProgressPerTask.GetFloat();
        }
        return true;
    }

    private static void CheckAwakening(PlayerControl player)
    {
        if (AwakeningProgress >= 100 && !IsAwakened && EnableAwakening.GetBool())
        {
            IsAwakened = true;
            player.Notify(GetString("SuccessfulAwakening"), 5f);
        }
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl nz)
    {
        if (EnableAwakening.GetBool() && IsAwakened)
        {
            var playerRole = nz.GetCustomRole();

            _ = new Explosion(5f, 0.5f, nz.GetCustomPosition());

            foreach (var target in Main.AllPlayerControls)
            {
                if (target.PlayerId == nz.PlayerId) continue;
                if (!target.IsAlive() || Medic.IsProtected(target.PlayerId) || target.inVent || target.IsTransformedNeutralApocalypse() || target.Is(CustomRoles.Solsticer)) continue;

                var pos = nz.transform.position;
                var dis = Utils.GetDistance(pos, target.transform.position);
                if (dis > BomberRadius.GetFloat()) continue;

                target.SetDeathReason(PlayerState.DeathReason.Torched);
                target.RpcMurderPlayer(target);
                target.SetRealKiller(nz);
            }

            return true;
        }

        return true;
    }
}

