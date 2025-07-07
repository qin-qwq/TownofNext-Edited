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
    //public override bool IsDesyncRole => true;
    public override bool IsExperimental => true;
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

        var sender = CustomRpcSender.Create("SchrodingersCat.OnCheckMurderAsTarget", SendOption.Reliable);

        target.GetRoleClass()?.OnRemove(target.PlayerId);
        target.RpcChangeRoleBasis(role);
        target.RpcSetCustomRole(role);
        target.GetRoleClass()?.OnAdd(target.PlayerId);
        if (killer.Is(CustomRoles.Narc)) target.RpcSetCustomRole(CustomRoles.Narc);

        sender.SendMessage();

        target.Notify(string.Format(GetString("RevenantTargeted"), Utils.GetRoleName(role)));

        target.ResetKillCooldown();
        target.SetKillCooldown(forceAnime: true);

        killer.SetKillCooldown();

        return false;
    }

    //public override void ApplyGameOptions(IGameOptions opt, byte babuyaga) => opt.SetVision(false);
    //public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 300f;

    //public override bool CanUseKillButton(PlayerControl pc) => false;
    //public override bool CanUseSabotage(PlayerControl pc) => false;
    //public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
}
