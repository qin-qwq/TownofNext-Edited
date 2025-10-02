using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Swooper : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Swooper;
    private const int Id = 4700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Swooper);
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SwooperCooldown;
    private static OptionItem SwooperDuration;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Swooper);
        SwooperCooldown = FloatOptionItem.Create(Id + 2, "SwooperCooldown", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Swooper])
            .SetValueFormat(OptionFormat.Seconds);
        SwooperDuration = FloatOptionItem.Create(Id + 4, "SwooperDuration", new(1f, 60f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Swooper])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = SwooperCooldown.GetFloat();
    }
    public override bool OnCheckVanish(PlayerControl player, float killCooldown)
    {
        player.RpcMakeInvisible();
        _ = new LateTask(() =>
        {
            player.Notify(GetString("SwooperInvisStateCountdown"), 3f);
        }, SwooperDuration.GetFloat() - 10f);
        _ = new LateTask(() =>
        {
            player.Notify(GetString("SwooperInvisStateCountdownn"), 3f);
        }, SwooperDuration.GetFloat() - 5f);
        _ = new LateTask(() =>
        {
            player.RpcResetAbilityCooldown();
            player.Notify(GetString("SwooperInvisStateOut"), 5f);
            player.RpcMakeVisible();
        }, SwooperDuration.GetFloat());
        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Main.Invisible.Contains(killer.PlayerId))
        {
            target.RpcMurderPlayer(target);
            target.SetRealKiller(killer);
            killer.SetKillCooldown();
            return false;
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("SwooperVentButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("invisible");
}
