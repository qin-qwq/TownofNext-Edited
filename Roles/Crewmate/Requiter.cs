using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Double;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Requiter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Requiter;
    private const int Id = 33200;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static OptionItem CanVent;
    public static OptionItem KillCooldown;
    public static OptionItem RequiterIgnoresShields;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Requiter);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Requiter])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Requiter]);
        RequiterIgnoresShields = BooleanOptionItem.Create(Id + 12, "RequiterIgnoresShields", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Requiter]);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }

    /*public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(1, 100) <= Knight.RequiterChance.GetInt();
    }*/

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override void SetKillCooldown(byte id) => KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => pc.GetAbilityUseLimit() > 0;

    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        if (exiled == null || exiled.Object == null || exiled.Object == player || !player.IsAlive()) return;
        if (exiled.Object.IsPlayerCrewmateTeam())
            player.RpcIncreaseAbilityUseLimitBy(1);
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        // options disabled,requiter doesn't ignore protections
        if (!RequiterIgnoresShields.GetBool()) return true;

        // requiter should never ignore Solsticer and Mini protections
        if (target.Is(CustomRoles.Solsticer)) return true;
        if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18) return true;

        // TNAs
        if (target.GetCustomRole().IsTNA()) return true;
        
        killer.RpcMurderPlayer(target);
        killer.ResetKillCooldown();
        return false;
    }

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;
        killer.RpcRemoveAbilityUse();
        target.SetDeathReason(PlayerState.DeathReason.Retribution);
    }
}
