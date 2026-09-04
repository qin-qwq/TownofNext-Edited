using System;
using System.Text.RegularExpressions;
using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Crewmate;

internal class Notary : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Notary;
    private const int Id = 34700;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem NotarizeLimitPerGame;
    private static OptionItem NotarizeLimitPerMeeting;
    enum OptionName
    {
        NotarizeLimitPerMeeting,
        AbilityUseGainWithEachTaskCompleted
    }
    public static readonly List<byte> NotarizeList = [];
    private static readonly Dictionary<byte, int> NotarizeLimitMeeting = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, Role);
        NotarizeLimitPerGame = IntegerOptionItem.Create(Role, Id + 10, GeneralOption.SkillLimitTimes, new(1, 30, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        NotarizeLimitPerMeeting = IntegerOptionItem.Create(Role, Id + 11, OptionName.NotarizeLimitPerMeeting, new(1, 30, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        NotaryAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Role, Id + 12, OptionName.AbilityUseGainWithEachTaskCompleted, new(0f, 5f, 0.1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        NotarizeList.Clear();
        NotarizeLimitMeeting.Clear();
    }

    public override void Add(byte playerId)
    {
        NotarizeLimitMeeting[playerId] = NotarizeLimitPerMeeting.GetInt();
        playerId.SetAbilityUseLimit(NotarizeLimitPerGame.GetInt());
    }

    public override void AfterMeetingTasks()
    {
        if (!_Player) return;

        NotarizeLimitMeeting[_Player.PlayerId] = NotarizeLimitPerMeeting.GetInt();
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (NotarizeList.Contains(target.PlayerId))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessNotarize"));
            return true;
        }
        return false;
    }

    public override bool RoleCommand(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || !pc || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Notary)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (GuessManager.CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id||編號|玩家編號")) operate = 1;
        else if (GuessManager.CheckCommond(ref msg, "nr|notarize|公证", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("NotaryDead"));
            return true;
        }

        if (operate == 1)
        {
            SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            if (NotarizeLimitMeeting[pc.PlayerId] < 1)
            {
                pc.ShowInfoMessage(isUI, GetString("NotaryNotarizeMaxMeetingMsg"));
                return true;
            }
            if (pc.GetAbilityUseLimit() < 1)
            {
                pc.ShowInfoMessage(isUI, GetString("NotaryNotarizeMaxGameMsg"));
                return true;
            }
            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                pc.ShowInfoMessage(isUI, error);
                return true;
            }
            var target = GetPlayerById(targetId);

            if (NotarizeList.Contains(targetId))
            {
                pc.ShowInfoMessage(isUI, GetString("AlreadyNotarize"));
                return true;
            }
            if (GuessManager.CantUseAbilityDuringDiscussionTime())
            {
                pc.ShowInfoMessage(isUI, GetString("UseAbilityDuringDiscussion"));
                return true;
            }
            if (Balancer.Choose && !(targetId == Balancer.Target1 || targetId == Balancer.Target2))
            {
                pc.ShowInfoMessage(isUI, GetString("SpecialMeeting2"));
                return true;
            }
            if (pc.PlayerId == target.PlayerId)
            {
                pc.ShowInfoMessage(isUI, GetString("Justice_LaughToWhoTrialSelf"), ColorString(Color.cyan, GetString("MessageFromKPD")));
                return true;
            }
            if (role.IsAdditionRole())
            {
                pc.ShowInfoMessage(isUI, GetString("NotarizeAddon"));
                return true;
            }
            if (role.IsRevealingRole(target))
            {
                pc.ShowInfoMessage(isUI, GetString("NotarizeRevealingRole"));
                return true;
            }
            if (!target.Is(role))
            {
                RPC.PlaySoundRPC(Sounds.SabotageSound, pc.PlayerId);
                pc.ShowInfoMessage(isUI, GetString("NotarizeFail"));
                NotarizeLimitMeeting[pc.PlayerId] = 0;
                pc.RpcRemoveAbilityUse();
                SendRPC(byte.MaxValue);
                return true;
            }

            Logger.Info($"{pc.GetNameWithRole()} try notarize {target.GetNameWithRole()}", "Notary");

            NotarizeLimitMeeting[pc.PlayerId]--;
            pc.RpcRemoveAbilityUse();
            NotarizeList.Add(targetId);
            SendRPC(targetId);
            RPC.PlaySoundRPC(Sounds.TaskComplete, pc.PlayerId);
            SendMessage(string.Format(GetString("NotarizeTarget"), ColorString(targetId.GetPlayerColor(), target.GetRealName()), target.GetCustomRole().ToColoredString()), 255, ColorString(GetRoleColor(CustomRoles.Notary), GetString("Notary").ToUpper()));
        }
        return true;
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
            error = GetString("NotaryHelp");
            role = new();
            return false;
        }

        PlayerControl target = GetPlayerById(id);
        if (target == null || !target.IsAlive())
        {
            error = GetString("NotaryNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByName(msg, out role))
        {
            error = GetString("NotaryHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void SendRPC(byte targerId)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(targerId);
        writer.Write(NotarizeLimitMeeting[_Player.PlayerId]);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte targetId = reader.ReadByte();
        int num = reader.ReadInt32();

        NotarizeLimitMeeting[pc.PlayerId] = num;
        if (targetId != byte.MaxValue) NotarizeList.Add(targetId);
    }

    public override bool CreateAbilityButton(PlayerControl pc) => pc.Is(CustomRoles.Notary) && pc.IsAlive() && pc.GetAbilityUseLimit() > 0 && NotarizeLimitMeeting[pc.PlayerId] > 0;

    public override bool UseGuessPage => true;

    public override bool ShowAbilityButtonFor(PlayerControl target) => target.IsAlive();

    public override string AbilityButtonName => "NotaryIcon";

    public override void OnClickAbilityButton(byte targetId, CustomRoles role)
    {
        Logger.Msg($"Click: ID {targetId}", "Notary UI");
        var target = targetId.GetPlayer();
        if (!target || !target.IsAlive() || !GameStates.IsVoting) return;
        RoleCommand(_Player, $"/nr {targetId} {GetString(role.ToString())}", true);
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => NotarizeList.Contains(target.PlayerId);
}