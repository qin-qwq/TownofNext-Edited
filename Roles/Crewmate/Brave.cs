using AmongUs.GameOptions;
using TONE.Roles.Core;
using static TONE.Translator;

namespace TONE.Roles.Crewmate;

internal class Brave : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Brave;
    private const int Id = 32400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Brave);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem HeartPlayerThreshold;
    private static OptionItem ShieldPlayerThreshold;
    private static OptionItem SwordPlayerThreshold;
    private static OptionItem KillCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Brave);
        HeartPlayerThreshold = IntegerOptionItem.Create(Id + 10, "BraveHeartThreshold", new(1, 15, 1), 12, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        ShieldPlayerThreshold = IntegerOptionItem.Create(Id + 11, "BraveShieldThreshold", new(1, 15, 1), 9, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        SwordPlayerThreshold = IntegerOptionItem.Create(Id + 12, "BraveSwordThreshold", new(1, 15, 1), 6, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        KillCooldown = FloatOptionItem.Create(Id + 13, "BraveSwordCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (Main.AllAlivePlayerControls.Length <= ShieldPlayerThreshold.GetInt())
        {
            killer.SetKillCooldown();
            killer.Notify(string.Format(GetString("TargetIsBrave"), target.GetRealName()));
            return false;
        }
        return true;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => Main.AllAlivePlayerControls.Length <= SwordPlayerThreshold.GetInt();
    public override bool KillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer) => Main.AllAlivePlayerControls.Length <= HeartPlayerThreshold.GetInt() && killer.PlayerId != seer.PlayerId;
}
