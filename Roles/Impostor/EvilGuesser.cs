using UnityEngine;
using static TOHE.Options;

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
    /*private static OptionItem EnableAwakening;
    private static OptionItem AwakeningThreshold;
    private static OptionItem ProgressPerKill;
    private static OptionItem ProgressPerGuess;
    private static OptionItem ProgressPerSecond;

    private static Dictionary<byte, float> AwakeningProgress = [];
    private static Dictionary<byte, bool> IsAwakened = [];*/

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
        /*EnableAwakening = BooleanOptionItem.Create(Id + 7, "EnableAwakening", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        AwakeningThreshold = FloatOptionItem.Create(Id + 8, "AwakeningThreshold", new(0f, 100f, 10f), 100f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerKill = FloatOptionItem.Create(Id + 9, "ProgressPerKill", new(0f, 100f, 10f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerGuess = FloatOptionItem.Create(Id + 10, "ProgressPerGuess", new(0f, 100f, 10f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 11, "ProgressPerSecond", new(0.001f, 1f, 0.01f), 0.01f, TabGroup.ImpostorRoles, false)
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

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (!EnableAwakening.GetBool() || isSuicide || IsAwakened[killer.PlayerId]) return;

        AwakeningProgress[killer.PlayerId] += ProgressPerKill.GetFloat();
        CheckAwakening(killer);
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
            player.Notify("邪恶赌怪已成功觉醒！", 5f);
        }
    }*/
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
