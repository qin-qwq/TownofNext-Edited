using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class Revenant : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Revenant;
    private const int Id = 30200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Revenant);

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    // private static OptionItem RevenantCanCopyAddons;
    private static OptionItem CanKillKiller;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Revenant);
        //RevenantCanCopyAddons = BooleanOptionItem.Create(Id + 10, "RevenantCanCopyAddons", false, TabGroup.NeutralRoles, false)
        //   .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Revenant]);
        CanKillKiller = BooleanOptionItem.Create(Id + 11, "CanKillKiller", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revenant]);
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        CustomRoles role = killer.GetCustomRole();
        if (role.IsTNA()) return false;

        if (CanKillKiller.GetBool())
        {
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
        }

        killer.RpcGuardAndKill(target);

        target.RpcGuardAndKill(target);
        target.GetRoleClass()?.OnRemove(target.PlayerId);
        target.RpcChangeRoleBasis(role);
        target.RpcSetCustomRole(role);
        target.GetRoleClass()?.OnAdd(target.PlayerId);

        killer.ResetKillCooldown();
        killer.SetKillCooldown(forceAnime: !DisableShieldAnimations.GetBool());
        target.ResetKillCooldown();
        target.SetKillCooldown(forceAnime: true);

        target.Notify(string.Format(GetString("RevenantTargeted"), Utils.GetRoleName(role)));

        return false;
    }
}
