using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class Revenant : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Revenant;
    private const int Id = 30200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Revenant);
    public override bool IsDesyncRole => true;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    // private static OptionItem RevenantCanCopyAddons;
    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Revenant);
        //RevenantCanCopyAddons = BooleanOptionItem.Create(Id + 10, "RevenantCanCopyAddons", false, TabGroup.NeutralRoles, false)
        //   .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Revenant]);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte babuyaga) => opt.SetVision(false);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 300f;

    public override bool CanUseKillButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        CustomRoles role = killer.GetCustomRole();
        if (role.IsTNA()) return false;

        killer.RpcMurderPlayer(killer);
        killer.SetRealKiller(target);

        target.GetRoleClass()?.OnRemove(target.PlayerId);
        target.RpcChangeRoleBasis(role);
        target.RpcSetCustomRole(role);
        target.GetRoleClass()?.OnAdd(target.PlayerId);
        if (killer.Is(CustomRoles.Narc)) target.RpcSetCustomRole(CustomRoles.Narc);

        target.Notify(string.Format(GetString("RevenantTargeted"), Utils.GetRoleName(role)));

        target.ResetKillCooldown();
        target.SetKillCooldown(forceAnime: true);

        return false;
    }
}
