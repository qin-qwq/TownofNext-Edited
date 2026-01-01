
namespace TONE.Roles.Vanilla;

internal class CrewmateTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.CrewmateTONE;
    private const int Id = 6000;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CrewmateTONE);
    }
}
