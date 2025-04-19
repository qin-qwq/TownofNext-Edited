using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Disruptor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Disruptor;
    private const int Id = 32500;

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem KillTargetWhenCantTP;
    private static OptionItem CanVent;
    public static OptionItem HasImpostorVision;
    private static OptionItem CanUsesSabotage;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Disruptor, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disruptor])
            .SetValueFormat(OptionFormat.Seconds);
        KillTargetWhenCantTP = BooleanOptionItem.Create(Id + 11, "KillTargetWhenCantTP", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disruptor]);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disruptor]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disruptor]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 14, GeneralOption.CanUseSabotage, false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disruptor]);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if(!target.CanBeTeleported() && !KillTargetWhenCantTP.GetBool())
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Disruptor), GetString("ErrorTeleport")));
            return false;
        }
        else if(!target.CanBeTeleported() && KillTargetWhenCantTP.GetBool())
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Disruptor), GetString("ErrorTeleport")));
            return true;
        }

        target.RPCPlayCustomSound("Teleport");
        killer.RpcGuardAndKill(killer);
        target.RpcRandomVentTeleport();
        target.RpcMurderPlayer(target);
        target.SetRealKiller(killer);
        killer.ResetKillCooldown();
        killer.SetKillCooldown(forceAnime: !DisableShieldAnimations.GetBool());

        return false;
    } 
}
