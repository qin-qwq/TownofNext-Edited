using AmongUs.GameOptions;
using UnityEngine;

namespace TONE.Roles.Vanilla;

internal class ViperTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.ViperTONE;
    private const int Id = 35000;
    public override CustomRoles ThisRoleBase => CustomRoles.Viper;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorVanilla;
    //==================================================================\\

    public static OptionItem ViperDissolveTime;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ViperTONE);
        ViperDissolveTime = IntegerOptionItem.Create(Id + 2, GeneralOption.ViperBase_ViperDissolveTime, new(1, 180, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ViperTONE])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting)
    {
        var ViperRole = RoleManager.Instance.GetRole(RoleTypes.Viper);
        var NewSprite = ViperRole.TryCast<ViperRole>()!.killSprite;
        return NewSprite;
    }
}
