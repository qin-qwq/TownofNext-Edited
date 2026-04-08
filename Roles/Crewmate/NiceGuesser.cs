using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Crewmate;

internal class NiceGuesser : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.NiceGuesser;
    private const int Id = 10900;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem GGCanGuessTime;
    private static OptionItem GGCanGuessCrew;
    private static OptionItem GGCanGuessAdt;
    private static OptionItem SafeGuess;
    private static OptionItem SafeGuessLimit;
    private static OptionItem MisguessRolePrevGuessRoleUntilNextMeeting;

    private bool CantGuess;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
        GGCanGuessTime = IntegerOptionItem.Create(Id + 10, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetValueFormat(OptionFormat.Times);
        GGCanGuessCrew = BooleanOptionItem.Create(Id + 11, "GGCanGuessCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGCanGuessAdt = BooleanOptionItem.Create(Id + 12, "GCanGuessAdt", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        SafeGuess = BooleanOptionItem.Create(Id + 13, "SafeGuess", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        SafeGuessLimit = IntegerOptionItem.Create(Id + 14, "SafeGuessLimit", new(1, 15, 1), 2, TabGroup.CrewmateRoles, false).SetParent(SafeGuess)
            .SetValueFormat(OptionFormat.Times);
        MisguessRolePrevGuessRoleUntilNextMeeting = BooleanOptionItem.Create(Id + 15, "DoomsayerMisguessRolePrevGuessRoleUntilNextMeeting", true, TabGroup.CrewmateRoles, false).SetParent(SafeGuess);
    }

    public override void Init()
    {
        CantGuess = false;
    }

    public override void Add(byte playerId)
    {
        if (SafeGuess.GetBool()) playerId.SetAbilityUseLimit(SafeGuessLimit.GetInt());
    }

    public static bool CanSafeGuess(byte playerId) => SafeGuess.GetBool() && playerId.GetAbilityUseLimit() > 0;

    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!GGCanGuessCrew.GetBool() && TabId == 0) return true;
        if (!GGCanGuessAdt.GetBool() && TabId == 3) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (CantGuess)
        {
            guesser.ShowInfoMessage(isUI, GetString("DoomsayerCantGuess"));
            return true;
        }

        // Check limit
        if (GuessManager.GuesserGuessed[guesser.PlayerId] >= GGCanGuessTime.GetInt())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessMax"));
            return true;
        }

        // Nice Guesser Can't Guess Addons
        if (role.IsAdditionRole() && !GGCanGuessAdt.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
            return true;
        }

        // Nice Guesser Can't Guess Crewmates
        if (role.IsCrewmate() && !GGCanGuessCrew.GetBool() && !guesser.Is(CustomRoles.Madmate))
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessCrewRole"));
            return true;
        }

        return false;
    }

    public override bool CheckMisGuessed(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (target.Is(CustomRoles.Rebound) && guesser.Is(CustomRoles.NiceGuesser) && !CanSafeGuess(guesser.PlayerId))
        {
            guesserSuicide = true;
            Logger.Info($"{guesser.GetNameWithRole().RemoveHtmlTags()} guessed {target.GetNameWithRole().RemoveHtmlTags()}, doomsayer suicide because rebound", "GuessManager");
        }

        if (CanSafeGuess(guesser.PlayerId) && guesser.PlayerId == target.PlayerId)
        {
            guesser.ShowInfoMessage(isUI, GetString("SafeGuessNotCorrectlyGuessRole"));
            guesser.RpcRemoveAbilityUse();
            if (guesser.IsHost()) Utils.FlashColor(Utils.GetRoleColor(CustomRoles.NiceGuesser));
            else SendRPC(guesser);

            if (MisguessRolePrevGuessRoleUntilNextMeeting.GetBool())
            {
                CantGuess = true;
            }

            return true;
        }

        return false;
    }

    public override void OnReportDeadBody(PlayerControl goku, NetworkedPlayerInfo solos)
    {
        CantGuess = false;
    }

    public void SendRPC(PlayerControl pc)
    {
        if (!pc.IsNonHostModdedClient()) return;
        var writer = MessageWriter.Get(SendOption.Reliable);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        Utils.FlashColor(Utils.GetRoleColor(CustomRoles.NiceGuesser));
    }
}
