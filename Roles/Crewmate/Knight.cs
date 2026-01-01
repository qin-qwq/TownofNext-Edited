using AmongUs.GameOptions;
using TONE.Modules;
using TONE.Roles.Core;
using TONE.Roles.Double;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Crewmate;

internal class Knight : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Knight;
    private const int Id = 10800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Knight);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem CanKnowTargetRole;
    public static OptionItem CanKillBeforeFirstMeeting;
    public static OptionItem RequiterChance;
    public static OptionItem RequiterIgnoresShields;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Knight);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Seconds);
        CanKnowTargetRole = BooleanOptionItem.Create(Id + 12, "CanKnowTargetRole", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight]);
        CanKillBeforeFirstMeeting = BooleanOptionItem.Create(Id + 13, "CanKillBeforeFirstMeeting", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight]);
        RequiterChance = IntegerOptionItem.Create(Id + 14, "RequiterChance", new(0, 100, 5), 0, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Percent);
        RequiterIgnoresShields = BooleanOptionItem.Create(Id + 15, "RequiterIgnoresShields", false, TabGroup.CrewmateRoles, false)
            .SetParent(RequiterChance);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetAbilityUseLimit() <= 0 ? 300f : KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => (CanKillBeforeFirstMeeting.GetBool() || !MeetingStates.FirstMeeting) && pc.GetAbilityUseLimit() > 0;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl banana)
    {
        if (CanKnowTargetRole.GetBool())
        {
            CustomRoles role = banana.GetCustomRole();
            killer.Notify(string.Format(GetString("KnightKnowRole"), Utils.GetRoleName(role)));
        }
        killer.RpcRemoveAbilityUse();
        Logger.Info($"{killer.GetNameWithRole()} : " + "Kill chance used", "Knight");
        return true;
    }
}

internal class Requiter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Requiter;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }

    public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(1, 100) <= Knight.RequiterChance.GetInt();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Knight.KillCooldown.GetFloat();
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
        if (!Knight.RequiterIgnoresShields.GetBool()) return true;

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
