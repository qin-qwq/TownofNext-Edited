using TOHE.Modules;
using TOHE.Roles.Core;

namespace TOHE.Roles.Impostor;

internal class Godfather : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Godfather;
    private const int Id = 3400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Godfather);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem GodfatherAbilityUses;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Godfather);
        GodfatherAbilityUses = IntegerOptionItem.Create(Id + 3, GeneralOption.SkillLimitTimes, new(1, 20, 1), 1, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(GodfatherAbilityUses.GetInt());
    }
    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (voter.PlayerId == target.PlayerId && voter.GetAbilityUseLimit() > 0)
        {
            voter.RpcRemoveAbilityUse();
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
            return false;
        }
        return true;
    }
}
