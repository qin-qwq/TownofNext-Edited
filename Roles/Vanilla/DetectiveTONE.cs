using AmongUs.GameOptions;

namespace TONE.Roles.Vanilla;

internal class DetectiveTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.DetectiveTONE;
    private const int Id = 33200;
    public override CustomRoles ThisRoleBase => CustomRoles.Detective;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem DetectiveSuspectLimit;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.DetectiveTONE);
        DetectiveSuspectLimit = IntegerOptionItem.Create(Id + 2, GeneralOption.DetectiveBase_DetectiveSuspectLimit, new(2, 4, 1), 2, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.DetectiveTONE])
            .SetValueFormat(OptionFormat.Players);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.DetectiveSuspectLimit = DetectiveSuspectLimit.GetInt();
    }
}
