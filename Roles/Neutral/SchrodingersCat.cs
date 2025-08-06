using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class SchrodingersCat : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.SchrodingersCat;
    private const int Id = 6900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.SchrodingersCat);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SchrodingersCat);
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Taskinator)) return true;

        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill();

        CustomRoles role = killer.GetCustomRole();

        target.GetRoleClass()?.OnRemove(target.PlayerId);
        target.RpcSetCustomRole(role);
        target.RpcSetRoleDesync(role.GetRoleTypes(), target.GetClientId());
        target.GetRoleClass()?.OnAdd(target.PlayerId);
        if (killer.Is(CustomRoles.Narc)) target.RpcSetCustomRole(CustomRoles.Narc);

        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

        target.ResetKillCooldown();
        target.SetKillCooldown(forceAnime: true);

        killer.SetKillCooldown();

        return false;
    }
}
