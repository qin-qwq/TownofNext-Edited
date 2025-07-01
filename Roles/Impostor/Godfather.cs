using Hazel;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Godfather : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Godfather;
    private const int Id = 3400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Godfather);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem GodfatherAbilityUses;
    private static OptionItem HideGodfatherEndCommand;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Godfather);
        GodfatherAbilityUses = IntegerOptionItem.Create(Id + 3, GeneralOption.SkillLimitTimes, new(1, 20, 1), 1, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather])
            .SetValueFormat(OptionFormat.Times);
        HideGodfatherEndCommand = BooleanOptionItem.Create(Id + 4, "HideGodfatherEndCommand", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather])
            .SetColor(Color.green);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(GodfatherAbilityUses.GetInt());
    }
    public static void TryHideMsgForGodfather()
    {
        ChatUpdatePatch.DoBlockChat = true;

        if (ChatManager.quickChatSpamMode != QuickChatSpamMode.QuickChatSpam_Disabled)
        {
            ChatManager.SendQuickChatSpam();
            ChatUpdatePatch.DoBlockChat = false;
            return;
        }

        var rd = IRandom.Instance;
        string msg;
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(1, 100) < 20)
                msg += "fin";
            var player = Main.AllAlivePlayerControls.RandomElement();
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        ChatUpdatePatch.DoBlockChat = false;
    }
    public static bool FMsg(PlayerControl pc, string msg)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Godfather)) return false;

        int operate;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "fin|结束|结束会议|結束|結束會議")) operate = 1;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("PresidentDead"), pc.PlayerId);
            return false;
        }

        else if (operate == 1)
        {

            if (HideGodfatherEndCommand.GetBool())
            {
                TryHideMsgForGodfather();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (pc.GetAbilityUseLimit() < 1)
            {
                Utils.SendMessage(GetString("PresidentEndMax"), pc.PlayerId);
                return true;
            }
            pc.RpcRemoveAbilityUse();

            foreach (var pva in MeetingHud.Instance.playerStates)
            {
                if (pva == null) continue;

                if (pva.VotedFor < 253)
                    MeetingHud.Instance.RpcClearVote(pva.TargetPlayerId);
            }
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
        }
        SendRPC(pc.PlayerId, isEnd: true);
        return true;
    }
    private static void SendRPC(byte playerId, bool isEnd = true)
    {
        var msg = new RpcGodfatherEnd(PlayerControl.LocalPlayer.NetId, playerId);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc, bool isEnd = true)
    {
        byte PlayerId = reader.ReadByte();
        FMsg(pc, $"/fin");
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    //msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
}
