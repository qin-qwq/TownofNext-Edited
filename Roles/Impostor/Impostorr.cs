using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Impostorr : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Impostorr;
    private const int Id = 32800;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SkillCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Impostorr);
        SkillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(2.5f, 120f, 2.5f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Impostorr])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = SkillCooldown.GetFloat();
    }

    private static bool BlackList(CustomRoles role)
    {
        return role is CustomRoles.Impostorr;
    }

    public override void UnShapeShiftButton(PlayerControl player)
    {
        CustomRoles role = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsImpostor() && !BlackList(role)).ToList().RandomElement();
        player.RpcChangeRoleBasis(role);
        player.GetRoleClass()?.OnRemove(player.PlayerId);
        player.RpcSetCustomRole(role);
        player.GetRoleClass()?.OnAdd(player.PlayerId);
        player.Notify(string.Format(GetString("RevenantTargeted"), Utils.GetRoleName(role)));
        player.RpcGuardAndKill(player);
        player.ResetKillCooldown();
        player.SetKillCooldown(forceAnime: true);
        player.MarkDirtySettings();
    }
}
