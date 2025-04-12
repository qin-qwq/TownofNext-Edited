namespace TOHE.Roles.Impostor;
using static TOHE.Options;

internal class Bard : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bard;
    private const int Id = 33100;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bard);
    }

    /*public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(0, 100) < Arrogance.BardChance.GetInt();
    }*/

    public override void OnPlayerExiled(PlayerControl bard, NetworkedPlayerInfo exiled)
    {
        if (exiled != null) Main.AllPlayerKillCooldown[bard.PlayerId] /= 2;
    }
}
