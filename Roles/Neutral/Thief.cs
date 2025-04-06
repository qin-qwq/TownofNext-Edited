using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Roles.Neutral;

internal class Thief : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles Role => CustomRoles.Thief;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem ThiefKillCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Thief);
        ThiefKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Thief])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ThiefKillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        CustomRoles role = target.GetCustomRole();
        if (role.IsTNA()) return false;

        if (target.GetCustomRole().IsImpostor())
        {
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
            killer.Notify(string.Format(GetString("TargetIsImpostor"), target.GetRealName(true)));
            return false;
        }
        killer.GetRoleClass()?.OnRemove(killer.PlayerId);
        killer.RpcChangeRoleBasis(role);
        killer.RpcSetCustomRole(role);
        killer.GetRoleClass()?.OnAdd(killer.PlayerId);

        target.GetRoleClass()?.OnRemove(target.PlayerId);
        target.RpcChangeRoleBasis(CustomRoles.Thief);
        target.RpcSetCustomRole(CustomRoles.Thief);
        target.GetRoleClass()?.OnAdd(target.PlayerId);

        killer.Notify(string.Format(GetString("RevenantTargeted"), Utils.GetRoleName(role)));
        target.Notify(string.Format(GetString("YouBecomeThief")));

        killer.ResetKillCooldown();
        killer.SetKillCooldown(forceAnime: !DisableShieldAnimations.GetBool());
        target.ResetKillCooldown();
        target.SetKillCooldown(forceAnime: true);
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(GetString("ThiefButtonTText"));
    }
}
