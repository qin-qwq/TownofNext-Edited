using static TONE.Options;

namespace TONE.Roles.Impostor;

internal class IdentityThief : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.IdentityThief;
    private const int Id = 34100;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem KillCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.IdentityThief);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.IdentityThief])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!killer || !target || Camouflage.IsCamouflage || Camouflager.AbilityActivated || Utils.IsActive(SystemTypes.MushroomMixupSabotage)) return true;

        if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out bool isShapeshifitng) && isShapeshifitng)
        {
            Logger.Info("Target was shapeshifting", "IdentityThief");
            return true;
        }

        killer.RpcShapeshift(target, false);

        return true;
    }
}
