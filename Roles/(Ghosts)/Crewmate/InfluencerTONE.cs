using AmongUs.GameOptions;
using static TONE.Options;

namespace TONE.Roles._Ghosts_.Crewmate;

internal class InfluencerTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.InfluencerTONE;
    private const int Id = 34800;

    public override CustomRoles ThisRoleBase => CustomRoles.Influencer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;
    //==================================================================\\

    private static OptionItem AbilityCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, Role);
        AbilityCooldown = FloatOptionItem.Create(Role, Id + 10, GeneralOption.InfluencerBase_SpiritGuideCooldownSeconds, new(2.5f, 120f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.SpiritGuideCooldownSeconds = AbilityCooldown.GetFloat();
    }
}
