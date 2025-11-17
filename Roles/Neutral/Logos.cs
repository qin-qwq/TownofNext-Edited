using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Logos : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Logos;
    private const int Id = 33500;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Logos, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos]);
    }

    public override void Add(byte playerId)
    {
        var logos = _Player;
        if (AmongUsClient.Instance.AmHost && logos.IsAlive())
        {
            var rolelist = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && !role.IsGhostRole() && role.IsNK()).ToList();
            if (rolelist.Any())
            {
                var role = rolelist.RandomElement();
                logos.GetRoleClass()?.OnRemove(logos.PlayerId);
                logos.RpcChangeRoleBasis(role);
                logos.RpcSetCustomRole(role);
                logos.GetRoleClass()?.OnAdd(logos.PlayerId);
                logos.SyncSettings();
            }
            else
            {
                var role = CustomRoles.Sunnyboy;
                logos.GetRoleClass()?.OnRemove(logos.PlayerId);
                logos.RpcChangeRoleBasis(role);
                logos.RpcSetCustomRole(role);
                logos.GetRoleClass()?.OnAdd(logos.PlayerId);
                logos.SyncSettings();
            }
        }
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => true;
}
