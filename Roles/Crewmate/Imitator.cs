using System;
using System.Text.RegularExpressions;
using TONE.Roles.Core;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Crewmate;

internal class Imitator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Imitator;
    private const int Id = 13000;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static readonly Dictionary<byte, CustomRoles> ImitateRole = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Imitator);
    }

    public override void Init()
    {
        ImitateRole.Clear();
    }

    public override void Add(byte playerId)
    {
        ImitateRole[playerId] = CustomRoles.NotAssigned;
    }

    public override void Remove(byte playerId)
    {
        ImitateRole.Remove(playerId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!seer.IsAlive() || seen.IsAlive() || seen.GetCustomRole().IsCrewmate()) return string.Empty;

        return ColorString(GetRoleColor(seer.GetCustomRole()), $" {seen.GetVisiblePlayerId()}");
    }

    public static void ChangeRoleMap()
    {
        foreach (var apc in ImitateRole)
        {
            var player = apc.Key.GetPlayer();
            var role = apc.Value;
            if (!player || !player.IsAlive() || role == CustomRoles.NotAssigned) continue;

            player.RpcSetCustomRole(role);
            if (player.IsHost()) player.RpcChangeRoleBasis(role);
            player.GetRoleClass()?.OnAdd(player.PlayerId);
            player.SyncSettings();
            ImitateRole[player.PlayerId] = CustomRoles.NotAssigned;
        }
    }

    public static bool ImitatorMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || !pc || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Imitator)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (GuessManager.CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id||編號|玩家編號")) operate = 1;
        else if (GuessManager.CheckCommond(ref msg, "imi|imitate|效仿", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("ImitatorDead"));
            return true;
        }

        if (operate == 1)
        {
            SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error))
            {
                SendMessage(error, pc.PlayerId, sendOption: Hazel.SendOption.None);
                return true;
            }
            var target = GetPlayerById(targetId);
            if (target)
            {
                if (GuessManager.CantUseAbilityDuringDiscussionTime())
                {
                    pc.ShowInfoMessage(isUI, GetString("UseAbilityDuringDiscussion"), ColorString(GetRoleColor(CustomRoles.Imitator), GetString("Imitator").ToUpper()));
                    return true;
                }
                if (Balancer.Choose && !(targetId == Balancer.Target1 || targetId == Balancer.Target2))
                {
                    pc.ShowInfoMessage(isUI, GetString("SpecialMeeting2"), ColorString(GetRoleColor(CustomRoles.Imitator), GetString("Imitator").ToUpper()));
                    return true;
                }
                if (ImitateRole[pc.PlayerId] != CustomRoles.NotAssigned)
                {
                    pc.ShowInfoMessage(isUI, GetString("Imitator.AlreadyImitate"), ColorString(GetRoleColor(CustomRoles.Imitator), GetString("Imitator").ToUpper()));
                    return true;
                }
                if (!target.GetCustomRole().IsCrewmate())
                {
                    pc.ShowInfoMessage(isUI, GetString("Imitator.CantImitateNonCrewmate"), ColorString(GetRoleColor(CustomRoles.Imitator), GetString("Imitator").ToUpper()));
                    return true;
                }

                ImitateRole[pc.PlayerId] = target.GetCustomRole();
                pc.ShowInfoMessage(isUI, string.Format(GetString("Imitator.ImitateTarget"), ColorString(targetId.GetPlayerColor(), target.GetRealName())), ColorString(GetRoleColor(CustomRoles.Imitator), GetString("Imitator").ToUpper()));

                Logger.Info($"{pc.GetNameWithRole()} Imitate {target.GetNameWithRole()}", "Imitator");
            }
        }

        return true;
    }

    private static bool MsgToPlayerAndRole(string msg, out byte id, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num) && num <= byte.MaxValue)
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MinValue;
            error = GetString("ImitatorHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = id.GetPlayer();
        if (!target || target.IsAlive())
        {
            error = GetString("ImitatorNull");
            return false;
        }

        error = string.Empty;
        return true;
    }
}