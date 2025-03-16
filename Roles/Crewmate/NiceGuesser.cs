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
    /*private static OptionItem EnableAwakening;
    private static OptionItem AwakeningThreshold;
    private static OptionItem ProgressPerTask;
    private static OptionItem ProgressPerGuess;
    private static OptionItem ProgressPerSecond;

    private static Dictionary<byte, float> AwakeningProgress = [];
    private static Dictionary<byte, bool> IsAwakened = [];*/

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
        GGCanGuessTime = IntegerOptionItem.Create(Id + 10, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetValueFormat(OptionFormat.Times);
        GGCanGuessCrew = BooleanOptionItem.Create(Id + 11, "GGCanGuessCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGCanGuessAdt = BooleanOptionItem.Create(Id + 12, "GGCanGuessAdt", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGTryHideMsg = BooleanOptionItem.Create(Id + 13, "GuesserTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetColor(Color.green);
        /*EnableAwakening = BooleanOptionItem.Create(Id + 14, "EnableAwakening", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        AwakeningThreshold = FloatOptionItem.Create(Id + 15, "AwakeningThreshold", new(0f, 100f, 10f), 100f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerTask = FloatOptionItem.Create(Id + 16, "ProgressPerTask", new(0f, 100f, 10f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerGuess = FloatOptionItem.Create(Id + 17, "ProgressPerGuess", new(0f, 100f, 10f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 18, "ProgressPerSecond", new(0.001f, 1f, 0.01f), 0.01f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);*/
    }

    /*public override void Init()
    {
        AwakeningProgress.Clear();
        IsAwakened.Clear();
    }

    public override void Add(byte playerId)
    {
        if (EnableAwakening.GetBool())
        {
            AwakeningProgress[playerId] = 0f;
            IsAwakened[playerId] = false;
        }
    }

    public override void Remove(byte playerId)
    {
        AwakeningProgress.Remove(playerId);
        IsAwakened.Remove(playerId);
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        if (!EnableAwakening.GetBool()) return base.GetProgressText(playerId, comms);

        var player = Utils.GetPlayerById(playerId);
        var progress = Mathf.Clamp(AwakeningProgress.GetValueOrDefault(playerId), 0f, 100f);
        var color = IsAwakened.GetValueOrDefault(playerId) ? "#FFA500" : "#FFFF00";

        return $"<color={color}>觉醒进度: {progress:F0}%</color>";
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!EnableAwakening.GetBool() || 
            !player.IsAlive() || 
            IsAwakened.GetValueOrDefault(player.PlayerId))
            return;

        AwakeningProgress[player.PlayerId] += ProgressPerSecond.GetFloat();
        CheckAwakening(player);
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (EnableAwakening.GetBool() && 
            !IsAwakened.GetValueOrDefault(player.PlayerId))
        {
            AwakeningProgress[player.PlayerId] += ProgressPerTask.GetFloat();
            CheckAwakening(player);
        }
        return true;
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {

        if (EnableAwakening.GetBool() &&
            !IsAwakened.GetValueOrDefault(guesser.PlayerId))
        {
            AwakeningProgress[guesser.PlayerId] += ProgressPerGuess.GetFloat();
            CheckAwakening(guesser);
        }
        return true;
    }

    private void CheckAwakening(PlayerControl player)
    {
        if (AwakeningProgress[player.PlayerId] >= AwakeningThreshold.GetFloat() && 
            !IsAwakened[player.PlayerId])
        {
            IsAwakened[player.PlayerId] = true;
            Main.PlayerStates[_Player.PlayerId].SubRoles.Add(CustomRoles.DoubleShot);
            player.Notify("正义赌怪已成功觉醒！", 5f);
        }
    }*/
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
