using AmongUs.GameOptions;
using TONE.Modules;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Impostor;

internal class Miner : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Miner;
    private const int Id = 4200;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem MinerSSCD;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Miner);
        MinerSSCD = FloatOptionItem.Create(Id + 3, GeneralOption.AbilityCooldown, new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Miner])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = MinerSSCD.GetFloat();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("MinerTeleButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Retreat");
    public override bool OnCheckVanish(PlayerControl player)
    {
        Vector2 closestVentPosition = player.GetClosestVent().transform.position;
        player.RpcTeleport(closestVentPosition);
        return false;
    }
}
