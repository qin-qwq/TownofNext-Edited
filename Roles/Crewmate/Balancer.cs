using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using UnityEngine;
using static TOHE.CheckForEndVotingPatch;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Balancer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Balancer;
    private const int Id = 32700;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public static OptionItem MeetingTime;

    public static byte Target1 = 253;
    public static byte Target2 = 253;
    public static bool Choose;
    public static bool Choose2;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Balancer);
        MeetingTime = IntegerOptionItem.Create(Id + 3, "MeetingTime", new(15, 300, 15), 90, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Balancer])
            .SetValueFormat(OptionFormat.Seconds);
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
        if (voter.GetAbilityUseLimit() < 1) return true;
        if (voter == null || target == null) return true;
        if (voter.IsHost()) return true;
        if (Target1 != 253)
        {
            Target2 = target.PlayerId;
            if (Target1 == Target2)
            {
                SendMessage(GetString("Choose1=2"), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
                Target1 = 253;
                Target2 = 253;
                return false;
            }
            var Tar1 = GetPlayerById(Target1);
            if (!Tar1.IsAlive())
            {
                Target1 = 253;
                Target2 = 253;
                SendMessage(string.Format(GetString("Choose1IsDead"), target.GetRealName()), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
                return false;
            }
            voter.RpcRemoveAbilityUse();
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
            Choose = true;
            Choose2 = true;
            SendRPC();
            return false;
        }
        Target1 = target.PlayerId;
        SendMessage(string.Format(GetString("Choose1"), target.GetRealName()), voter.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
        return false;
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        var Tar1 = GetPlayerById(Target1);
        var Tar2 = GetPlayerById(Target2);
        if (Choose) MeetingHudStartPatch.AddMsg(string.Format(GetString("SpecialMeeting"), Tar1.GetRealName(), Tar2.GetRealName()), 255, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
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
            TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, Target2);
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
        }
        if (Target2 == deadid)
        {
            TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, Target1);
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
        }
    }
    public static void BalancerAfterMeetingTasks()
    {
        Choose2 = false;
        var Tar1 = GetPlayerById(Target1);
        _ = new LateTask(() =>
        {
            Tar1?.NoCheckStartMeeting(null);
        }, 2.5f);
    }
    public override void AfterMeetingTasks()
    {
        Target1 = 253;
        Target2 = 253;
        Choose = false;
        SendRPC();
    }
    public void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(Choose);
        writer.Write(Choose2);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        Choose = reader.ReadBoolean();
        Choose2 = reader.ReadBoolean();
    }
    private static void BalancerOnClick(byte targetId /*, MeetingHud __instance*/)
    {
        Logger.Msg($"Click: ID {targetId}", "Balancer UI");
        var target = targetId.GetPlayer();
        if (target == null || !target.IsAlive() || !GameStates.IsVoting || PlayerControl.LocalPlayer.GetAbilityUseLimit() < 1) return;
        if (Target1 != 253)
        {
            Target2 = targetId;
            if (Target1 == Target2)
            {
                SendMessage(GetString("Choose1=2"), PlayerControl.LocalPlayer.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
                Target1 = 253;
                Target2 = 253;
                return;
            }
            var Tar1 = GetPlayerById(Target1);
            if (!Tar1.IsAlive())
            {
                Target1 = 253;
                Target2 = 253;
                SendMessage(string.Format(GetString("Choose1IsDead"), target.GetRealName()), PlayerControl.LocalPlayer.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
                return;
            }
            PlayerControl.LocalPlayer.RpcRemoveAbilityUse();
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
            Choose = true;
            Choose2 = true;
            return;
        }
        Target1 = targetId;
        SendMessage(string.Format(GetString("Choose1"), target.GetRealName()), PlayerControl.LocalPlayer.PlayerId, ColorString(GetRoleColor(CustomRoles.Balancer), GetString("Balancer").ToUpper()));
        return;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Balancer) && PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.IsHost())
                CreateBalancerButton(__instance);
        }
    }
    public static void CreateBalancerButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            var pc = GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("BalancerIcon");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => BalancerOnClick(pva.TargetPlayerId/*, __instance*/)));
        }
    }
}
