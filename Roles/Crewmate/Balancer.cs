using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.CheckForEndVotingPatch;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Crewmate;

internal class Balancer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Balancer;
    private const int Id = 32700;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public static OptionItem MeetingTime;
    public static OptionItem ExileWithoutAnyoneVoting;

    public static byte Target1 = 253;
    public static byte Target2 = 253;
    public static bool Choose;
    public static bool Choose2;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Balancer);
        MeetingTime = IntegerOptionItem.Create(Id + 3, "MeetingTime", new(15, 300, 15), 90, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Balancer])
            .SetValueFormat(OptionFormat.Seconds);
        ExileWithoutAnyoneVoting = BooleanOptionItem.Create(Id + 4, "ExileWithoutAnyoneVoting", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Balancer]);
    }
    public override void Init()
    {
        Choose = false;
        Choose2 = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
        Target1 = 253;
        Target2 = 253;
    }

    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (Choose) return true;
        if (voter.GetRoleClass().HasVoted) return true;
        if (voter.GetAbilityUseLimit() < 1) return true;
        if (voter == null || target == null) return true;
        if (voter.IsModded()) return true;
        if (Target1 != 253)
        {
            Target2 = target.PlayerId;
            if (Target1 == Target2)
            {
                SendMessage(GetString("Choose1=2"), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()), sendOption: SendOption.None);
                Target1 = 253;
                Target2 = 253;
                return false;
            }
            var Tar1 = GetPlayerById(Target1);
            if (!Tar1.IsAlive())
            {
                Target1 = 253;
                Target2 = 253;
                SendMessage(string.Format(GetString("Choose1IsDead"), target.GetRealName()), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()), sendOption: SendOption.None);
                return false;
            }
            voter.RpcRemoveAbilityUse();
            Choose = true;
            Choose2 = true;
            RpcVotingCompleteV2();
            return false;
        }
        Target1 = target.PlayerId;
        SendMessage(string.Format(GetString("Choose1"), target.GetRealName()), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
        return false;
    }

    public static void BalancerMsg(PlayerControl voter, PlayerControl target)
    {
        if (Choose) return;
        if (voter.GetAbilityUseLimit() < 1) return;
        if (voter == null || target == null) return;
        if (!voter.IsAlive())
        {
            SendMessage(GetString("BalancerDead"), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()), sendOption: SendOption.None);
            return;
        }
        if (Target1 != 253)
        {
            Target2 = target.PlayerId;
            if (Target1 == Target2)
            {
                SendMessage(GetString("Choose1=2"), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()), sendOption: SendOption.None);
                Target1 = 253;
                Target2 = 253;
                return;
            }
            var Tar1 = GetPlayerById(Target1);
            if (!Tar1.IsAlive())
            {
                Target1 = 253;
                Target2 = 253;
                SendMessage(string.Format(GetString("Choose1IsDead"), target.GetRealName()), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()), sendOption: SendOption.None);
                return;
            }
            voter.RpcRemoveAbilityUse();
            Choose = true;
            Choose2 = true;
            RpcVotingCompleteV2();
            return;
        }
        Target1 = target.PlayerId;
        SendMessage(string.Format(GetString("Choose1"), target.GetRealName()), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        var Tar1 = GetPlayerById(Target1);
        var Tar2 = GetPlayerById(Target2);
        if (Choose)
        {
            MeetingHudStartPatch.AddMsg(string.Format(GetString("SpecialMeeting"), ColorString(Target1.GetPlayerColor(), Tar1.GetRealName()), ColorString(Target2.GetPlayerColor(), Tar2.GetRealName()),
                255, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper())));
            if (!Tar1 && !Tar2 || !Tar1.IsAlive() && !Tar2.IsAlive())
            {
                RpcVotingCompleteV2();
                return;
            }
            if (!Tar1 || !Tar1.IsAlive())
            {
                CheckBalancerTarget(Target2);
            }
            if (!Tar2 || !Tar2.IsAlive())
            {
                CheckBalancerTarget(Target1);
            }
        }
        else
        {
            Target1 = 253;
            Target2 = 253;
        }
    }
    public static void CheckBalancerTarget(byte deadid)
    {
        if (deadid == 253) return;
        if (!Choose) return;

        if (Target1 == deadid)
        {
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), Target2.GetPlayer().Data, false, true, Target2);
            ConfirmEjections(Target2.GetPlayer().Data);
        }
        if (Target2 == deadid)
        {
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), Target1.GetPlayer().Data, false, true, Target1);
            ConfirmEjections(Target1.GetPlayer().Data);
        }
    }
    public static void BalancerAfterMeetingTasks()
    {
        Choose2 = false;
        var Tar1 = GetPlayerById(Target1);
        var Tar2 = GetPlayerById(Target2);

        _ = new LateTask(() =>
        {
            Tar1?.NoCheckStartMeeting(null);
        }, 1f);
    }
    public override void AfterMeetingTasks()
    {
        Target1 = 253;
        Target2 = 253;
        Choose = false;
    }

    public override bool CreateAbilityButton(PlayerControl pc) => pc.Is(CustomRoles.Balancer) && pc.IsAlive() && pc.GetAbilityUseLimit() > 0;

    public override bool ShowAbilityButtonFor(PlayerControl target) => target.IsAlive();

    public override string AbilityButtonName => "BalancerIcon";

    public override void OnClickAbilityButton(byte targetId)
    {
        Logger.Msg($"Click: ID {targetId}", "Balancer UI");
        var target = targetId.GetPlayer();
        if (!target || !target.IsAlive() || !GameStates.IsVoting) return;
        BalancerMsg(_Player, target);
    }
}
