using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Core;
using static TONE.CheckForEndVotingPatch;
using static TONE.Options;
using static TONE.Utils;

namespace TONE.Roles.Impostor;

internal class Speaker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Speaker;
    private const int Id = 1600;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem SkillLimit;
    private static OptionItem ImpKnowTarget;
    private static OptionItem TarKnowTarget;

    private int VoteNum = 0;
    private static readonly Dictionary<byte, byte> Target = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Speaker);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Speaker])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimit = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(1, 15, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Speaker])
            .SetValueFormat(OptionFormat.Times);
        ImpKnowTarget = BooleanOptionItem.Create(Id + 12, "ImpKnowTarget", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Speaker]);
        TarKnowTarget = BooleanOptionItem.Create(Id + 13, "TarKnowTarget", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Speaker]);
    }

    public override void Init()
    {
        VoteNum = 0;
        Target.Clear();
    }

    public override void Add(byte playerId)
    {
        VoteNum = 0;
        Target[playerId] = byte.MaxValue;
        playerId.SetAbilityUseLimit(SkillLimit.GetInt());

        var pc = GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public static bool IsSpoken(byte targetId)
    {
        foreach (var player in Target.Keys)
        {
            if (Target[player] == targetId) return true;
        }
        return false;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0 && target.PlayerId != Target[killer.PlayerId])
        {
            return killer.CheckDoubleTrigger(target, () =>
            {
                killer.SetKillCooldown(5f);
                killer.RpcRemoveAbilityUse();
                Target[killer.PlayerId] = target.PlayerId;
                NotifyRoles(SpecifyTarget: target);
                SendRPC(0, killer, target);
            });
        }
        else return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (Target[_Player.PlayerId] != byte.MaxValue)
        {
            PlayerVoteArea pva = GetPlayerVoteArea(Target[_Player.PlayerId]);
            VoteNum = 1 + Target[_Player.PlayerId].GetPlayer().GetRoleClass().AddRealVotesNum(pva);
        }
    }

    private void SendRPC(byte typeId, PlayerControl player, PlayerControl target)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(typeId);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte typeId = reader.ReadByte();
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                Target[playerId] = targetId;
                break;
            case 1:
                Target[playerId] = byte.MaxValue;
                break;
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (Target[seer.PlayerId] != byte.MaxValue && Target[seer.PlayerId] == seen.PlayerId)
            return ColorString(GetRoleColor(CustomRoles.Speaker), " ❖");
        return string.Empty;
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (!seer.IsAlive() || (seer.GetCustomRole().IsImpostor() && !seer.Is(CustomRoles.Speaker) && ImpKnowTarget.GetBool())
        || (seer.PlayerId == Target[_Player.PlayerId] && isForMeeting && TarKnowTarget.GetBool()))
        {
            if (Target[_Player.PlayerId] != byte.MaxValue && Target[_Player.PlayerId] == target.PlayerId)
            {
                return ColorString(GetRoleColor(CustomRoles.Speaker), " ❖");
            }
        }
        return string.Empty;
    }

    public override void AfterMeetingTasks()
    {
        if (VoteNum != 0)
        {
            VoteNum = 0;
        }
        if (Target[_Player.PlayerId] != byte.MaxValue)
        {
            var target = Target[_Player.PlayerId];
            Target[_Player.PlayerId] = byte.MaxValue;
            SendRPC(1, _Player, _Player);
            NotifyRoles(SpecifyTarget: target.GetPlayer());
        }
    }

    public override int AddRealVotesNum(PlayerVoteArea PVA) => VoteNum;
}
