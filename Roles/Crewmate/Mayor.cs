using AmongUs.GameOptions;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal partial class Mayor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Mayor;
    private const int Id = 12000;
    public override CustomRoles ThisRoleBase => MayorHasPortableButton.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem MayorAdditionalVote;
    private static OptionItem MayorVoteGainWithEachTaskCompleted;
    private static OptionItem MayorVoteGainWithAfterMeeting;
    private static OptionItem MayorHasPortableButton;
    private static OptionItem MayorNumOfUseButton;
    private static OptionItem MayorHideVote;
    public static OptionItem MayorRevealWhenDoneTasks;

    private static readonly Dictionary<byte, int> MayorUsedButtonCount = [];
    public static int AdditionalVotes;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mayor);
        MayorAdditionalVote = IntegerOptionItem.Create(Id + 10, "MayorAdditionalVote", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
            .SetValueFormat(OptionFormat.Votes);
        MayorVoteGainWithEachTaskCompleted = IntegerOptionItem.Create(Id + 11, "MayorAdditionalVoteTask", new(0, 3, 1), 0, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
            .SetValueFormat(OptionFormat.Votes);
        MayorVoteGainWithAfterMeeting = IntegerOptionItem.Create(Id + 12, "MayorAdditionalVoteMeeting", new(0, 3, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
            .SetValueFormat(OptionFormat.Votes);
        MayorHasPortableButton = BooleanOptionItem.Create(Id + 13, "MayorHasPortableButton", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorNumOfUseButton = IntegerOptionItem.Create(Id + 14, "MayorNumOfUseButton", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(MayorHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
        MayorHideVote = BooleanOptionItem.Create(Id + 15, GeneralOption.HideAdditionalVotes, false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorRevealWhenDoneTasks = BooleanOptionItem.Create(Id + 16, "MayorRevealWhenDoneTasks", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        OverrideTasksData.Create(Id + 15, TabGroup.CrewmateRoles, CustomRoles.Mayor);
    }

    public override void Init()
    {
        MayorUsedButtonCount.Clear();
    }
    public override void Add(byte playerId)
    {
        MayorUsedButtonCount[playerId] = 0;
        AdditionalVotes = MayorAdditionalVote.GetInt();
    }
    public override void Remove(byte playerId)
    {
        MayorUsedButtonCount[playerId] = 0;
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        AdditionalVotes += MayorVoteGainWithEachTaskCompleted.GetInt();
        return true;
    }

    public override void AfterMeetingTasks()
    {
        AdditionalVotes += MayorVoteGainWithAfterMeeting.GetInt();
    }

    public override int AddRealVotesNum(PlayerVoteArea PVA) => AdditionalVotes;

    public override void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
    {
        if (MayorHideVote.GetBool()) return;

        for (var i = 0; i < AdditionalVotes; i++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = votedPlayer.TargetPlayerId,
                VotedForId = votedPlayer.VotedFor
            });
        }
    }

    //public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    //{
    //    if (target == null)
    //        MayorUsedButtonCount[reporter.PlayerId] += 1;
    //}
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown =
                !MayorUsedButtonCount.TryGetValue(playerId, out var count) || count < MayorNumOfUseButton.GetInt()
                ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                : 300f;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (MayorHasPortableButton.GetBool())
        {
            if (MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < MayorNumOfUseButton.GetInt())
            {
                MayorUsedButtonCount[pc.PlayerId] += 1;
                pc?.MyPhysics?.RpcBootFromVent(vent.Id);
                pc?.NoCheckStartMeeting(null);
            }
        }
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId)
        => MayorUsedButtonCount.TryGetValue(physics.myPlayer.PlayerId, out var count)
        && count >= MayorNumOfUseButton.GetInt();

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.Mayor) return false;
        if (MayorRevealWhenDoneTasks.GetBool() && target.GetPlayerTaskState().IsTaskFinished)
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessMayor"));
            return true;
        }
        return false;
    }

    public static bool VisibleToEveryone(PlayerControl target) => target.Is(CustomRoles.Mayor) && MayorRevealWhenDoneTasks.GetBool() && target.GetPlayerTaskState().IsTaskFinished;
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("MayorVentButtonText");
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("EmergencyButton");

    public override string GetProgressText(byte playerId, bool coooms)
    {
        var voteNum = AdditionalVotes;
        var ProgressText = new StringBuilder();
        ProgressText.Append(ColorString(voteNum < 1 ? Color.gray : GetRoleColor(CustomRoles.Mayor), $"({voteNum})"));
        return ProgressText.ToString();
    }
}
