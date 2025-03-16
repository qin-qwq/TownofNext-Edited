using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Crewmater : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Crewmater;
    private const int Id = 32100;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static OptionItem CrewmaterKillCooldown;
    public static OptionItem CrewmaterKillLimit;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Crewmater);
        CrewmaterKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Crewmater])
            .SetValueFormat(OptionFormat.Seconds);
        CrewmaterKillLimit = IntegerOptionItem.Create(Id + 11, "CrewmaterKillLimit", new(1, 15, 1), 2, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Crewmater])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Crewmater);
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.GetPlayerTaskState().IsTaskFinished && player.IsAlive())
        {
            player.RpcChangeRoleBasis(CustomRoles.Soldier);
            player.RpcSetCustomRole(CustomRoles.Soldier);
            player.GetRoleClass()?.OnAdd(_Player.PlayerId);
            player.Notify(GetString("BecomeSoldier"), 5f);
            player.RpcGuardAndKill(player);
            return true;
        }
        return false;
    }
}
internal class Soldier : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Soldier;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Soldier);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public override void Add(byte playerId)
    {
        Main.PlayerStates[playerId].taskState.hasTasks = false;
        playerId.SetAbilityUseLimit(Crewmater.CrewmaterKillLimit.GetInt());
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : Crewmater.CrewmaterKillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => !IsKilled(pc.PlayerId);

    private static bool IsKilled(byte playerId) => playerId.GetAbilityUseLimit() <= 0;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.RpcRemoveAbilityUse();
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return true;
    }
}
