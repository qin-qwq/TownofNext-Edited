namespace TONE.Roles.Impostor;

internal class Bard : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bard;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static float BardKillCooldown;

    public override void Init()
    {
        BardKillCooldown = Options.DefaultKillCooldown;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = BardKillCooldown;

    public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(0, 100) < Arrogance.BardChance.GetInt();
    }

    public override void OnPlayerExiled(PlayerControl bard, NetworkedPlayerInfo exiled)
    {
        if (exiled != null) BardKillCooldown /= 2;
    }
}
