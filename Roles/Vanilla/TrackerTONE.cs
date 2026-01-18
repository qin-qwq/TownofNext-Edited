using AmongUs.GameOptions;

namespace TONE.Roles.Vanilla;

internal class TrackerTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TrackerTONE;
    private const int Id = 6250;
    public override CustomRoles ThisRoleBase => CustomRoles.Tracker;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem TrackCooldown;
    private static OptionItem TrackDuration;
    private static OptionItem TrackDelay;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TrackerTONE);
        TrackCooldown = IntegerOptionItem.Create(Id + 2, GeneralOption.TrackerBase_TrackingCooldown, new(1, 120, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.TrackerTONE])
            .SetValueFormat(OptionFormat.Seconds);
        TrackDuration = IntegerOptionItem.Create(Id + 3, GeneralOption.TrackerBase_TrackingDuration, new(5, 120, 5), 30, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.TrackerTONE])
            .SetValueFormat(OptionFormat.Seconds);
        TrackDelay = IntegerOptionItem.Create(Id + 4, GeneralOption.TrackerBase_TrackingDelay, new(0, 10, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.TrackerTONE])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.TrackerCooldown = TrackCooldown.GetInt();
        AURoleOptions.TrackerDuration = TrackDuration.GetInt();
        AURoleOptions.TrackerDelay = TrackDelay.GetInt();
    }
}
