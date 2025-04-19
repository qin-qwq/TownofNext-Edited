using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Konan : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Konan;
    private const int Id = 33300;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Tracker;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem TrackCooldown;
    private static OptionItem TrackDuration;
    private static OptionItem TrackDelay;

    private bool Didvote = false;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Konan);
        TrackCooldown = IntegerOptionItem.Create(Id + 10, GeneralOption.TrackerBase_TrackingCooldown, new(1, 120, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Konan])
            .SetValueFormat(OptionFormat.Seconds);
        TrackDuration = IntegerOptionItem.Create(Id + 11, GeneralOption.TrackerBase_TrackingDuration, new(5, 120, 5), 30, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Konan])
            .SetValueFormat(OptionFormat.Seconds);
        TrackDelay = IntegerOptionItem.Create(Id + 12, GeneralOption.TrackerBase_TrackingDelay, new(0, 10, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Konan])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.TrackerCooldown = TrackCooldown.GetInt();
        AURoleOptions.TrackerDuration = TrackDuration.GetInt();
        AURoleOptions.TrackerDelay = TrackDelay.GetInt();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }
    public static bool KonanCheckVotingForTarget(PlayerControl pc, PlayerVoteArea pva)
        => pc.Is(CustomRoles.Konan) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead && pc.GetAbilityUseLimit() > 0;

    public override void AfterMeetingTasks() => Didvote = false;
    public override bool CheckVote(PlayerControl votePlayer, PlayerControl voteTarget)
    {
        if (votePlayer == null || voteTarget == null) return true;
        if (Didvote == true) return false;
        if (voteTarget.GetCustomRole().IsCrewmate() && votePlayer.GetAbilityUseLimit() > 0)
        {
            Didvote = true;
            votePlayer.RpcRemoveAbilityUse();
            SendMessage(GetString("KonanVoteHasReturned"), votePlayer.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Konan), string.Format(GetString("VoteAbilityUsed"), GetString("Konan"))));
            return false;
        }
        return true;
    }
}
