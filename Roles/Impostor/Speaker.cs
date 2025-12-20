using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using static TOHE.CheckForEndVotingPatch;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

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

    private byte VoteTarget = byte.MaxValue;
    private byte Target = byte.MaxValue;

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
        VoteTarget = byte.MaxValue;
        Target = byte.MaxValue;
    }

    public override void Add(byte playerId)
    {
        VoteTarget = byte.MaxValue;
        Target = byte.MaxValue;
        playerId.SetAbilityUseLimit(SkillLimit.GetInt());

        var pc = GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0 && target.PlayerId != Target)
        {
            return killer.CheckDoubleTrigger(target, () =>
            {
                killer.SetKillCooldown(5f);
                killer.RpcRemoveAbilityUse();
                Target = target.PlayerId;
                NotifyRoles(SpecifyTarget: target);
                SendRPC();
            });
        }
        else return true;
    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(Target);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte target = reader.ReadByte();

        Target = target;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId && Target != byte.MaxValue && Target == seen.PlayerId)
            return ColorString(GetRoleColor(CustomRoles.Speaker), " ❖");
        return string.Empty;
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (!seer.IsAlive() || (seer.GetCustomRole().IsImpostor() && ImpKnowTarget.GetBool())
        || (seer.PlayerId == Target && isForMeeting && TarKnowTarget.GetBool()))
        {
            if (Target == target.PlayerId)
            {
                return ColorString(GetRoleColor(CustomRoles.Speaker), " ❖");
            }
        }
        return string.Empty;
    }

    public void ChangeVote(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            if (pva.TargetPlayerId.GetPlayer().Is(CustomRoles.Speaker))
            {
                VoteTarget = pva.VotedFor;
            }
            if (pva.AmDead || pva.VotedFor == VoteTarget || !_Player.IsAlive() || pva == null) continue;
            var voter = GetPlayerById(pva.TargetPlayerId);
            if (voter == null || voter.Data == null || voter.PlayerId != Target) continue;
            if (VoteTarget < 252)
            {
                pva.VotedFor = VoteTarget;
                ReturnChangedPva(pva);
            }
        }
    }

    public override void AfterMeetingTasks()
    {
        if (VoteTarget != byte.MaxValue)
        {
            VoteTarget = byte.MaxValue;
        }
        if (Target != byte.MaxValue)
        {
            var target = Target.GetPlayer();
            Target = byte.MaxValue;
            SendRPC();
            NotifyRoles(SpecifyTarget: target);
        }
    }
}
