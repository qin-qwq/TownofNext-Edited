using Hazel;
using System;
using System.Text.RegularExpressions;
using TONE.Modules.ChatManager;
using TONE.Roles.Crewmate;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Coven;

internal class Ritualist : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Ritualist;
    private const int Id = 30800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    private static OptionItem MaxRitsPerRound;
    public static OptionItem EnchantedKnowsCoven;
    public static OptionItem EnchantedKnowsEnchanted;

    private static readonly Dictionary<byte, int> RitualLimit = [];
    private static readonly Dictionary<byte, List<byte>> EnchantedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Ritualist, 1, zeroOne: false);
        MaxRitsPerRound = IntegerOptionItem.Create(Id + 10, "RitualistMaxRitsPerRound", new(1, 15, 1), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist])
            .SetValueFormat(OptionFormat.Times);
        EnchantedKnowsCoven = BooleanOptionItem.Create(Id + 12, "RitualistEnchantedKnowsCoven", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist]);
        EnchantedKnowsEnchanted = BooleanOptionItem.Create(Id + 13, "RitualistEnchantedKnowsEnchanted", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist]);

    }
    public override void Init()
    {
        RitualLimit.Clear();
        EnchantedPlayers.Clear();
    }
    public override void Add(byte PlayerId)
    {
        EnchantedPlayers[PlayerId] = [];
        RitualLimit.Add(PlayerId, MaxRitsPerRound.GetInt());
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);
    public override void OnReportDeadBody(PlayerControl hatsune, NetworkedPlayerInfo miku)
    {
        foreach (var pid in RitualLimit.Keys)
        {
            RitualLimit[pid] = MaxRitsPerRound.GetInt();
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.GetCustomRole().IsCovenTeam())
        {
            killer.Notify(GetString("CovenDontKillOtherCoven"));
            return false;
        }
        return true;
    }
    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Ritualist), target.PlayerId.ToString()) + " " + TargetPlayerName : "";
    public static bool RitualistMsgCheck(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Ritualist)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id||編號|玩家編號")) operate = 1;
        else if (CheckCommond(ref msg, "rt|rit|ritual|bloodritual|鲜血仪式|仪式|献祭|举行|附魔", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("GuessDead"));
            return true;
        }

        if (operate == 1)
        {
            SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }

        else if (operate == 2)
        {
            if (RitualLimit[pc.PlayerId] <= 0)
            {
                pc.ShowInfoMessage(isUI, GetString("RitualistRitualMax"));
                return true;
            }

            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                pc.ShowInfoMessage(isUI, error);
                return true;
            }
            if (Balancer.Choose && !(targetId == Balancer.Target1 || targetId == Balancer.Target2))
            {
                pc.ShowInfoMessage(isUI, GetString("SpecialMeeting2"));
                return true;
            }
            var target = GetPlayerById(targetId);
            if (role.IsAdditionRole())
            {
                pc.ShowInfoMessage(isUI, GetString("RitualistGuessAddon"));
                return true;
            }
            if (!target.Is(role))
            {
                RPC.PlaySoundRPC(Sounds.SabotageSound, pc.PlayerId);
                pc.ShowInfoMessage(isUI, GetString("RitualistRitualFail"));
                RitualLimit[pc.PlayerId] = 0;
                return true;
            }
            if (target.GetCustomRole().IsRevealingRole(target))
            {
                pc.ShowInfoMessage(isUI, GetString("GuessRevealingRole"));
                return true;
            }
            if (!target.CanBeRecruitedBy(pc))
            {
                pc.ShowInfoMessage(isUI, GetString("RitualistRitualImpossible"));
                return true;
            }

            Logger.Info($"{pc.GetNameWithRole()} enchant {target.GetNameWithRole()}", "Ritualist");

            RitualLimit[pc.PlayerId]--;

            EnchantedPlayers[pc.PlayerId].Add(target.PlayerId);
            RPC.PlaySoundRPC(Sounds.TaskUpdateSound, target.PlayerId);
            SendMessage(string.Format(GetString("RitualistConvertNotif"), CustomRoles.Ritualist.ToColoredString()), target.PlayerId);
            RPC.PlaySoundRPC(Sounds.TaskComplete, pc.PlayerId);
            SendMessage(string.Format(GetString("RitualistRitualSuccess"), target.GetRealName()), pc.PlayerId);
            return true;
        }
        return false;
    }
    public override void AfterMeetingTasks()
    {
        foreach (var rit in EnchantedPlayers.Keys)
        {
            var ritualist = GetPlayerById(rit);
            foreach (var pc in EnchantedPlayers[rit])
            {
                ConvertRole(ritualist, GetPlayerById(pc));
            }
            EnchantedPlayers[rit].Clear();
        }
    }
    public void ConvertRole(PlayerControl killer, PlayerControl target)
    {
        var addon = killer.GetBetrayalAddon(true);
        if (target.CanBeRecruitedBy(killer))
        {
            Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + addon.ToString(), "Ritualist Assign");
            target.RpcSetCustomRole(addon);
            if (addon is CustomRoles.Admired)
            {
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId);
            }
        }
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            id = byte.MaxValue;
            error = GetString("RitualistCommandHelp");
            role = new();
            return false;
        }

        PlayerControl target = GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByName(msg, out role))
        {
            error = GetString("RitualistCommandHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        if (msg.StartsWith("/cmd"))
        {
            msg = "/" + msg[4..].TrimStart();
        }
        var comList = command.Split('|');
        foreach (var comm in comList)
        {
            if (exact)
            {
                if (msg == "/" + comm) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comm))
                {
                    msg = msg.Replace("/" + comm, string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    public static bool CanBeConverted(PlayerControl pc)
    {
        return pc != null && !(pc.GetCustomRole().IsCovenTeam() || pc.GetBetrayalAddon().IsCovenTeam()) && !pc.IsTransformedNeutralApocalypse() && !pc.Is(CustomRoles.Solsticer);
    }
}
