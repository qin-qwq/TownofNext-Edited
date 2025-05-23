using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Knight : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Knight;
    private const int Id = 10800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Knight);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static OptionItem CanVent;
    public static OptionItem KillCooldown;
    //public static OptionItem RequiterChance;
    //public static OptionItem RequiterIgnoresShields;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Knight);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight]);
        /*RequiterChance = IntegerOptionItem.Create(Id + 13, "RequiterChance", new(0, 100, 5), 0, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Percent);
        RequiterIgnoresShields = BooleanOptionItem.Create(Id + 14, "RequiterIgnoresShields", false, TabGroup.CrewmateRoles, false)
            .SetParent(RequiterChance);*/
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public static bool CheckCanUseVent(PlayerControl player) => player.Is(CustomRoles.Knight) && CanVent.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CheckCanUseVent(pc);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => !IsKilled(pc.PlayerId);

    private static bool IsKilled(byte playerId) => playerId.GetAbilityUseLimit() <= 0;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl banana)
    {
        killer.RpcRemoveAbilityUse();
        Logger.Info($"{killer.GetNameWithRole()} : " + "Kill chance used", "Knight");
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return true;
    }
}
