using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Core;

namespace TOHE.Roles.Impostor;

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
    //private static OptionItem EGCanGuessTaskDoneSnitch; Not used
    private static OptionItem EGTryHideMsg;
    private static OptionItem EnableAwakening;
    private static OptionItem ProgressPerKill;
    private static OptionItem ProgressPerSecond;

    private static float AwakeningProgress;
    private static bool IsAwakened;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilGuesser);
        EGCanGuessTime = IntegerOptionItem.Create(Id + 2, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetValueFormat(OptionFormat.Times);
        EGCanGuessImp = BooleanOptionItem.Create(Id + 3, "EGCanGuessImp", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGCanGuessAdt = BooleanOptionItem.Create(Id + 4, "EGCanGuessAdt", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        //EGCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 5, "EGCanGuessTaskDoneSnitch", true, TabGroup.ImpostorRoles, false)
        //    .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGTryHideMsg = BooleanOptionItem.Create(Id + 6, "GuesserTryHideMsg", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetColor(Color.green);
        EnableAwakening = BooleanOptionItem.Create(Id + 7, "EnableAwakening", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        ProgressPerKill = FloatOptionItem.Create(Id + 8, "ProgressPerKill", new(0f, 100f, 10f), 40f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 10, "ProgressPerSecond", new(0.1f, 3f, 0.1f), 0.5f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void Init()
    {
        AwakeningProgress = 0;
        IsAwakened = false;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!EnableAwakening.GetBool() || AwakeningProgress >= 100) return string.Empty;
        return string.Format(GetString("AwakeningProgress") + ": {0:F0}% / {1:F0}%", AwakeningProgress, 100);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (AwakeningProgress < 100)
        {
            AwakeningProgress += ProgressPerSecond.GetFloat() * Time.fixedDeltaTime;
        }
        else CheckAwakening(player);
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!IsAwakened)
        {
            AwakeningProgress += ProgressPerKill.GetFloat();
        }
        return true;
    }

    private static void CheckAwakening(PlayerControl player)
    {
        if (AwakeningProgress >= 100 && !IsAwakened && EnableAwakening.GetBool() && player.IsAlive())
        {
            IsAwakened = true;
            player.RpcSetCustomRole(CustomRoles.DoubleShot, false, false);
            player.Notify(GetString("SuccessfulAwakening"), 5f);
        }
    }
    
    public static bool NeedHideMsg(PlayerControl pc) => pc.Is(CustomRoles.EvilGuesser) && EGTryHideMsg.GetBool();

    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!EGCanGuessImp.GetBool() && TabId == 1) return true;
        if (!EGCanGuessAdt.GetBool() && TabId == 3) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        // Check limit
        if (GuessManager.GuesserGuessed[guesser.PlayerId] >= EGCanGuessTime.GetInt())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("EGGuessMax"));
            return true;
        }

        // Evil Guesser Can't Guess Addons
        if (role.IsAdditionRole() && !EGCanGuessAdt.GetBool())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessAdtRole"));
            return true;
        }

        // Evil Guesser Can't Guess Impostors
        if ((role.IsImpostor() || role.IsMadmate()) && !EGCanGuessImp.GetBool())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessImpRole"));
            return true;
        }

        return false;
    }
}
