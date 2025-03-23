using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class InjusticeSpirit : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.InjusticeSpirit);
    public override CustomRoles Role => CustomRoles.InjusticeSpirit;
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;
    //==================================================================\\

    public static OptionItem RevealCooldown;
    public bool KnowTargetRole = false;
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.InjusticeSpirit);
        RevealCooldown = FloatOptionItem.Create(Id + 10, "RevealCooldown", new(0f, 120f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.InjusticeSpirit])
            .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.InjusticeSpirit);
    }
    public override void Init()
    {
        KnowTargetRole = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = RevealCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() <= 0) return false;
        if (target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Madmate) && !target.GetCustomRole().IsConverted()) return false;
        else
        {
            killer.RpcRemoveAbilityUse();
            target.RpcSetCustomRole(CustomRoles.Revealed, false, false);
            return true;
        }
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.GetPlayerTaskState().IsTaskFinished)
        {
            _Player.RpcIncreaseAbilityUseLimitBy(1);
            return true;
        }
        return false;
    }
    public static bool KnowRole(PlayerControl seer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Revealed)) return true;
        return false;
    }
}
