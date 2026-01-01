using AmongUs.GameOptions;

namespace TONE.Roles.Vanilla;

internal class ViperTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.ViperTONE;
    private const int Id = 35000;
    public override CustomRoles ThisRoleBase => CustomRoles.Viper;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorVanilla;
    //==================================================================\\

    private static OptionItem ViperDissolveTime;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ViperTONE);
        ViperDissolveTime = IntegerOptionItem.Create(Id + 2, GeneralOption.ViperBase_ViperDissolveTime, new(1, 180, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ViperTONE])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ViperDissolveTime = ViperDissolveTime.GetInt();
    }
}
