using Hazel;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Godfather : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Godfather;
    private const int Id = 3400;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem GodfatherChangeOpt;
    private static OptionItem GodfatherAbilityUses;
    private static OptionItem HideGodfatherEndCommand;

    private static readonly HashSet<byte> GodfatherTarget = [];
    private bool Didvote = false;

    [Obfuscation(Exclude = true)]
    private enum GodfatherChangeModeList
    {
        GodfatherCount_Refugee,
        GodfatherCount_Madmate
    }

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Godfather);
        GodfatherChangeOpt = StringOptionItem.Create(Id + 2, "GodfatherTargetCountMode", EnumHelper.GetAllNames<GodfatherChangeModeList>(), 0, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather]);
        GodfatherAbilityUses = IntegerOptionItem.Create(Id + 3, GeneralOption.SkillLimitTimes, new(1, 20, 1), 1, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather])
            .SetValueFormat(OptionFormat.Times);
        HideGodfatherEndCommand = BooleanOptionItem.Create(Id + 15, "HideGodfatherEndCommand", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather]);
    }

    public override void Init()
    {
        GodfatherTarget.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(GodfatherAbilityUses.GetInt());
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }
    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Remove(CheckDeadBody);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => GodfatherTarget.Clear();
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        var godfather = _Player;
        List<CustomRoles> BTAddonList = godfather.GetCustomSubRoles().Where(x => x.IsBetrayalAddonV2()).ToList();
        //this list will only contain 1 element,or just be an empty list...

        var ChangeRole = BTAddonList.Any() ? BTAddonList.FirstOrDefault() switch
        {
            CustomRoles.Admired => CustomRoles.Sheriff,
            CustomRoles.Recruit => CustomRoles.Sidekick,
            _ => CustomRoles.Refugee
        }
        : CustomRoles.Refugee;
        var ChangeAddon = BTAddonList.Any() ? BTAddonList.FirstOrDefault() : CustomRoles.Madmate;
        if (GodfatherTarget.Contains(target.PlayerId))
        {
            if (!killer.IsAlive()) return;
            if (GodfatherChangeOpt.GetValue() == 0)
            {
                killer.RpcChangeRoleBasis(ChangeRole);
                killer.GetRoleClass()?.OnRemove(killer.PlayerId);
                killer.RpcSetCustomRole(ChangeRole);
                killer.GetRoleClass()?.OnAdd(killer.PlayerId);
                if (ChangeRole is CustomRoles.Refugee
                    && (ChangeAddon is not CustomRoles.Madmate || godfather.Is(CustomRoles.Madmate)))//Can Godfather become Madmate?
                    killer.RpcSetCustomRole(ChangeAddon);
            }
            else
            {
                killer.RpcSetCustomRole(ChangeAddon);
            }

            killer.RpcGuardAndKill();
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Godfather), GetString("GodfatherRefugeeMsg")));
            NotifyRoles(killer);
        }
    }
    public override void AfterMeetingTasks() => Didvote = false;
    public override bool CheckVote(PlayerControl votePlayer, PlayerControl voteTarget)
    {
        if (votePlayer == null || voteTarget == null) return true;
        if (Didvote == true) return false;
        Didvote = true;

        GodfatherTarget.Add(voteTarget.PlayerId);
        SendMessage(GetString("VoteHasReturned"), votePlayer.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Godfather), string.Format(GetString("VoteAbilityUsed"), GetString("Godfather"))));
        return false;
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
                msg += "f";
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
        if (CheckCommond(ref msg, "f|结束|结束会议|結束|結束會議")) operate = 1;
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
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GodfatherEnd, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc, bool isEnd = true)
    {
        byte PlayerId = reader.ReadByte();
        FMsg(pc, $"/f");
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
