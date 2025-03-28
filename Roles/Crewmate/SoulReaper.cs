using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class SoulReaper : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.SoulReaper;
    private const int Id = 31800;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SoulReaper);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulReaper])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ReportCount(id) ? 300f : KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => !ReportCount(pc.PlayerId);

    private static bool ReportCount(byte playerId) => playerId.GetAbilityUseLimit() <= 0;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.RpcRemoveAbilityUse();
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return true;
    }

    public override void OnReportDeadBody(PlayerControl player, NetworkedPlayerInfo deadBody)
    {
        if (deadBody == null) return;

        if (player != null && player.Is(CustomRoles.SoulReaper) && player.PlayerId != deadBody.PlayerId)
        {
        _Player.RpcIncreaseAbilityUseLimitBy(1);
        }
    }

    public override void AfterMeetingTasks()
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.SoulReaper))
            {
                pc.ResetKillCooldown();
                pc.SetKillCooldown();
                if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            }
        }
    }
}
