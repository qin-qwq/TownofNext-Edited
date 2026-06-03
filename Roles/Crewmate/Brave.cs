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
    public override bool IsBalance => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem HeartPlayerThreshold;
    private static OptionItem ShieldPlayerThreshold;
    private static OptionItem SwordPlayerThreshold;
    private static OptionItem HeartMinimumPlayerThreshold;
    private static OptionItem ShieldMinimumPlayerThreshold;
    private static OptionItem SwordMinimumPlayerThreshold;
    private static OptionItem KillCooldown;

    private static int HeartPlayer;
    private static int ShieldPlayer;
    private static int SwordPlayer;

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
        HeartMinimumPlayerThreshold = IntegerOptionItem.Create(Id + 13, "BraveHeartMinimumThreshold", new(1, 15, 1), 8, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        ShieldMinimumPlayerThreshold = IntegerOptionItem.Create(Id + 14, "BraveShieldMinimumThreshold", new(1, 15, 1), 6, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        SwordMinimumPlayerThreshold = IntegerOptionItem.Create(Id + 15, "BraveSwordMinimumThreshold", new(1, 15, 1), 4, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        KillCooldown = FloatOptionItem.Create(Id + 16, "BraveSwordCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add(byte playerId)
    {
        HeartPlayer = base.CalculatePlayers(HeartPlayerThreshold.GetInt(), HeartMinimumPlayerThreshold.GetInt());
        ShieldPlayer = base.CalculatePlayers(ShieldPlayerThreshold.GetInt(), ShieldMinimumPlayerThreshold.GetInt());
        SwordPlayer = base.CalculatePlayers(SwordPlayerThreshold.GetInt(), SwordMinimumPlayerThreshold.GetInt());
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (Main.AllAlivePlayerControls.Count <= ShieldPlayer)
        {
            killer.SetKillCooldown();
            killer.Notify(string.Format(GetString("TargetIsBrave"), target.GetRealName()));
            return false;
        }
        return true;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => Main.AllAlivePlayerControls.Count <= SwordPlayer;
    public override bool KillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer) => Main.AllAlivePlayerControls.Count <= HeartPlayer && killer.PlayerId != seer.PlayerId;
}
