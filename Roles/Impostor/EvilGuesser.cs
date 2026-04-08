using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using static TONE.Translator;

namespace TONE.Roles.Impostor;

internal class EvilGuesser : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.EvilGuesser;
    private const int Id = 1300;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem EGCanGuessTime;
    private static OptionItem EGCanGuessImp;
    private static OptionItem EGCanGuessAdt;
    private static OptionItem SafeGuess;
    private static OptionItem SafeGuessLimit;
    private static OptionItem MisguessRolePrevGuessRoleUntilNextMeeting;

    private bool CantGuess;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilGuesser);
        EGCanGuessTime = IntegerOptionItem.Create(Id + 2, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetValueFormat(OptionFormat.Times);
        EGCanGuessImp = BooleanOptionItem.Create(Id + 3, "EGCanGuessImp", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGCanGuessAdt = BooleanOptionItem.Create(Id + 4, "GCanGuessAdt", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        SafeGuess = BooleanOptionItem.Create(Id + 5, "SafeGuess", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        SafeGuessLimit = IntegerOptionItem.Create(Id + 14, "SafeGuessLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(SafeGuess)
            .SetValueFormat(OptionFormat.Times);
        MisguessRolePrevGuessRoleUntilNextMeeting = BooleanOptionItem.Create(Id + 15, "DoomsayerMisguessRolePrevGuessRoleUntilNextMeeting", true, TabGroup.ImpostorRoles, false).SetParent(SafeGuess);
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
        if (!EGCanGuessImp.GetBool() && TabId == 1) return true;
        if (!EGCanGuessAdt.GetBool() && TabId == 3) return true;

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
        if (GuessManager.GuesserGuessed[guesser.PlayerId] >= EGCanGuessTime.GetInt())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessMax"));
            return true;
        }

        // Evil Guesser Can't Guess Addons
        if (role.IsAdditionRole() && !EGCanGuessAdt.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
            return true;
        }

        // Evil Guesser Can't Guess Impostors
        if ((role.IsImpostor() || role.IsMadmate()) && !EGCanGuessImp.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessImpRole"));
            return true;
        }

        return false;
    }

    public override bool CheckMisGuessed(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (target.Is(CustomRoles.Rebound) && guesser.Is(CustomRoles.EvilGuesser) && !CanSafeGuess(guesser.PlayerId))
        {
            guesserSuicide = true;
            Logger.Info($"{guesser.GetNameWithRole().RemoveHtmlTags()} guessed {target.GetNameWithRole().RemoveHtmlTags()}, doomsayer suicide because rebound", "GuessManager");
        }

        if (CanSafeGuess(guesser.PlayerId) && guesser.PlayerId == target.PlayerId)
        {
            guesser.ShowInfoMessage(isUI, GetString("SafeGuessNotCorrectlyGuessRole"));
            guesser.RpcRemoveAbilityUse();
            if (guesser.IsHost()) Utils.FlashColor(Utils.GetRoleColor(CustomRoles.EvilGuesser));
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
        Utils.FlashColor(Utils.GetRoleColor(CustomRoles.EvilGuesser));
    }
}
