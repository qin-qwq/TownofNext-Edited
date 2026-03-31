using TONE.Roles.Core;

namespace TONE.Roles.Neutral;

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

        var addon = killer.GetBetrayalAddon(true);
        var role = killer.GetCustomRole();

        target.GetRoleClass()?.OnRemove(target.PlayerId);
        target.RpcSetCustomRole(role);
        target.RpcChangeRoleBasis(role);
        target.GetRoleClass()?.OnAdd(target.PlayerId);

        if (killer.GetBetrayalAddon() != CustomRoles.NotAssigned)
            target.RpcSetCustomRole(addon);

        Utils.NotifyRoles(SpecifyTarget: target, ForceLoop: true);

        target.ResetKillCooldown();
        target.SetKillCooldown(forceAnime: true);

        killer.SetKillCooldown();

        return false;
    }
}
