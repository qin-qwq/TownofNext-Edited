
using AmongUs.GameOptions;

namespace TONE.Roles.Vanilla;

internal class ScientistTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.ScientistTONE;
    private const int Id = 6200;
    public override CustomRoles ThisRoleBase => CustomRoles.Scientist;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem BatteryCooldown;
    private static OptionItem BatteryDuration;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ScientistTONE);
        BatteryCooldown = IntegerOptionItem.Create(Id + 2, GeneralOption.ScientistBase_BatteryCooldown, new(1, 250, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ScientistTONE])
            .SetValueFormat(OptionFormat.Seconds);
        BatteryDuration = IntegerOptionItem.Create(Id + 3, GeneralOption.ScientistBase_BatteryDuration, new(1, 250, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ScientistTONE])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ScientistCooldown = BatteryCooldown.GetInt();
        AURoleOptions.ScientistBatteryCharge = BatteryDuration.GetInt();
    }
}
