using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

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
    private static OptionItem GGTryHideMsg;
    private static OptionItem EnableAwakening;
    private static OptionItem ProgressPerTask;
    private static OptionItem ProgressPerSecond;

    private static float AwakeningProgress;
    private static bool IsAwakened;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
        GGCanGuessTime = IntegerOptionItem.Create(Id + 10, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetValueFormat(OptionFormat.Times);
        GGCanGuessCrew = BooleanOptionItem.Create(Id + 11, "GGCanGuessCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGCanGuessAdt = BooleanOptionItem.Create(Id + 12, "GGCanGuessAdt", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGTryHideMsg = BooleanOptionItem.Create(Id + 13, "GuesserTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetColor(Color.green);
        EnableAwakening = BooleanOptionItem.Create(Id + 14, "EnableAwakening", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        ProgressPerTask = FloatOptionItem.Create(Id + 15, "ProgressPerTask", new(0f, 100f, 10f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 17, "ProgressPerSecond", new(0.1f, 3f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false)
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

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!IsAwakened)
        {
            AwakeningProgress += ProgressPerTask.GetFloat();
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

    public static bool NeedHideMsg(PlayerControl pc) => pc.Is(CustomRoles.NiceGuesser) && GGTryHideMsg.GetBool();

    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!GGCanGuessCrew.GetBool() && TabId == 0) return true;
        if (!GGCanGuessAdt.GetBool() && TabId == 3) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        // Check limit
        if (GuessManager.GuesserGuessed[guesser.PlayerId] >= GGCanGuessTime.GetInt())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GGGuessMax"));
            return true;
        }

        // Nice Guesser Can't Guess Addons
        if (role.IsAdditionRole() && !GGCanGuessAdt.GetBool())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessAdtRole"));
            return true;
        }

        // Nice Guesser Can't Guess Crewmates
        if (role.IsCrewmate() && !GGCanGuessCrew.GetBool() && !guesser.Is(CustomRoles.Madmate))
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessCrewRole"));
            return true;
        }

        return false;
    }
}
