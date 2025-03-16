using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Trickster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Trickster;
    private const int Id = 4800;
    public override CustomRoles ThisRoleBase => CanTurnOffLinghts.GetBool() ? CustomRoles.Shapeshifter : CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    public static OptionItem CanTurnOffLinghts;
    private static OptionItem TurnOffLinghtsCooldown;  

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Trickster);
        CanTurnOffLinghts = BooleanOptionItem.Create(Id + 10, "CanTurnOffLinghts", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Trickster]);
        TurnOffLinghtsCooldown = FloatOptionItem.Create(Id + 11, "TurnOffLinghtsCooldown", new(0f, 180f, 5f), 45f, TabGroup.ImpostorRoles, false)
            .SetParent(CanTurnOffLinghts)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = TurnOffLinghtsCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void UnShapeShiftButton(PlayerControl player)
    {
        if (Utils.IsActive(SystemTypes.Electrical)) return;

        // Code from AU: SabotageSystemType.UpdateSystem switch SystemTypes.Electrical
        byte switchId = 4;
        for (int index = 0; index < 5; ++index)
        {
            if (BoolRange.Next())
                switchId |= (byte)(1 << index);
        }
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(switchId | 128U));
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("TricksteShapeshiftText"));
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("TurnOff");
}
