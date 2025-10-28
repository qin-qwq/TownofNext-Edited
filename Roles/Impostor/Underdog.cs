using System;

namespace TOHE.Roles.Impostor;

internal class Underdog : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Underdog;
    private const int Id = 2700;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem UnderdogKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(2700, TabGroup.ImpostorRoles, CustomRoles.Underdog);
        UnderdogKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.DefaultKillCooldown, new(0f, 180f, 2.5f), 45f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "Underdog.ReduceKillCooldown", new(0f, 180f, 1f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.MinKillCooldown, new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Math.Clamp(UnderdogKillCooldown.GetFloat() - (Main.AllPlayerControls.Count(x => !x.IsAlive()) * ReduceKillCooldown.GetFloat()), MinKillCooldown.GetFloat(), UnderdogKillCooldown.GetFloat());

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        killer?.ResetKillCooldown();
        killer?.SyncSettings();
    }
}
