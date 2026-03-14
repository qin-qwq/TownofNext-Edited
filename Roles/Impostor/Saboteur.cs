using static TONE.Utils;

namespace TONE.Roles.Impostor;

internal class Saboteur : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Saboteur;
    private const int Id = 2300;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem SaboteurCD;
    public static OptionItem SaboteurMinCD;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Saboteur);
        SaboteurCD = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Saboteur])
            .SetValueFormat(OptionFormat.Seconds);
        SaboteurMinCD = FloatOptionItem.Create(Id + 3, GeneralOption.MinKillCooldown, new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Saboteur])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SaboteurCD.GetFloat();

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (AnySabotageIsActive())
        {
            killer.SetKillCooldown(SaboteurMinCD.GetFloat());
        }
    }

    public override bool CanUseKillButton(PlayerControl pc) => true;

    public static bool IsCriticalSabotage()
        => IsActive(SystemTypes.Laboratory)
           || IsActive(SystemTypes.LifeSupp)
           || IsActive(SystemTypes.Reactor)
           || IsActive(SystemTypes.HeliSabotage);
}
