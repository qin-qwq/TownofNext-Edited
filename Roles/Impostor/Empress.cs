using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Empress : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Empress;
    private const int Id = 32900;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem EmpressShapeshiftCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Empress);
        EmpressShapeshiftCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Empress])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = EmpressShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(GetString("EmpressButtonText"));
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Teleport");

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        if (target.CanBeTeleported())
        {
            target.SetKillCooldown(5f);
            target.ResetKillCooldown();
            target.RpcTeleport(shapeshifter.GetCustomPosition());

            shapeshifter.RPCPlayCustomSound("Teleport");
            target.RPCPlayCustomSound("Teleport");
        }
        else
        {
            shapeshifter.Notify(ColorString(GetRoleColor(CustomRoles.Empress), GetString("ErrorTeleport")));
        }

        return false;
    }
}
