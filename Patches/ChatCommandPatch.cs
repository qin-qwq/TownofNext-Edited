using AmongUs.InnerNet.GameDataMessages;
using Assets.CoreScripts;
using Hazel;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TONE.Modules;
using TONE.Modules.ChatManager;
using TONE.Modules.Rpc;
using TONE.Roles.AddOns.Common;
using TONE.Roles.Core;
using TONE.Roles.Core.AssignManager;
using TONE.Roles.Core.DraftAssign;
using TONE.Roles.Coven;
using TONE.Roles.Crewmate;
using TONE.Roles.Impostor;
using TONE.Roles.Neutral;
using UnityEngine;
using UnityEngine.Networking;
using static TONE.Translator;


namespace TONE;

internal class Command(string key, string arguments, Command.UsageLevels usageLevel, Command.UsageTimes usageTime, Action<PlayerControl, string, string[]> action, bool isCanceled, bool alwaysHidden, string[] argsDescriptions = null)
{
    public enum UsageLevels
    {
        Everyone,
        Modded,
        Host,
        HostIsDeveloper,
        Developer,
        Up,
        HostOrModerator,
        HostOrVIP
    }

    public enum UsageTimes
    {
        Always,
        InLobby,
        InGame,
        InMeeting,
        AfterDeath,
        AfterDeathOrLobby
    }

    public static List<Command> AllCommands = [];

    public string[] CommandForms = GetString($"CommandForms.{key}", SupportedLangs.SChinese).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    public string Key => key;
    public string Arguments => arguments;
    public string Description => GetString($"CommandDescription.{key}");
    public string[] ArgsDescriptions => argsDescriptions ?? [];
    public UsageLevels UsageLevel => usageLevel;
    public UsageTimes UsageTime => usageTime;
    public Action<PlayerControl, string, string[]> Action => action;
    public bool IsCanceled => isCanceled;
    public bool AlwaysHidden => alwaysHidden;

    public bool IsThisCommand(string text)
    {
        if (!text.StartsWith('/')) return false;

        text = text.ToLower().Trim().TrimStart('/');
        return CommandForms.Any(text.Split(' ')[0].Equals);
    }

    public bool CanUseCommand(PlayerControl pc, bool checkTime = true, bool sendErrorMessage = false)
    {
        if (UsageLevel == UsageLevels.Everyone && UsageTime == UsageTimes.Always) return true;

        switch (UsageLevel)
        {
            case UsageLevels.Host when !pc.IsHost():
            case UsageLevels.Modded when !pc.IsModded():
            case UsageLevels.Developer when !pc.Data.FriendCode.CanUseDev():
            case UsageLevels.HostIsDeveloper when !pc.IsHost() || !pc.Data.FriendCode.CanUseDev():
            case UsageLevels.Up when !pc.IsHost() || !pc.Data.FriendCode.GetDevUser().IsUp:
            case UsageLevels.HostOrModerator when !pc.IsHost() && AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode):
            case UsageLevels.HostOrVIP when !pc.IsHost() && AmongUsClient.Instance.AmHost && !Utils.IsPlayerVIP(pc.FriendCode):
                if (sendErrorMessage) Utils.SendMessage(GetString($"Commands.NoAccess.Level.{UsageLevel}"), pc.PlayerId);
                return false;
        }

        if (!checkTime) return true;

        switch (UsageTime)
        {
            case UsageTimes.InLobby when !GameStates.IsLobby:
            case UsageTimes.InGame when !GameStates.InGame:
            case UsageTimes.InMeeting when !GameStates.IsMeeting:
            case UsageTimes.AfterDeath when pc.IsAlive():
            case UsageTimes.AfterDeathOrLobby when pc.IsAlive() && !GameStates.IsLobby:
                if (sendErrorMessage) Utils.SendMessage(GetString($"Commands.NoAccess.Time.{UsageTime}"), pc.PlayerId);
                return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
    private static readonly string modLogFiles = @$"{Main.Path}/TONE-DATA/ModLogs.txt";
    private static readonly string modTagsFiles = @$"{Main.Path}/TONE-DATA/Tags/MOD_TAGS";
    private static readonly string sponsorTagsFiles = @$"{Main.Path}/TONE-DATA/Tags/SPONSOR_TAGS";
    private static readonly string vipTagsFiles = @$"{Main.Path}/TONE-DATA/Tags/VIP_TAGS";
    private static readonly string modFiles = @$"{Main.Path}/TONE-DATA/Moderators.txt";
    private static readonly string vipFiles = @$"{Main.Path}/TONE-DATA/VIP-List.txt";

    private static readonly Dictionary<char, int> Pollvotes = [];
    private static readonly Dictionary<char, string> PollQuestions = [];
    private static readonly List<byte> PollVoted = [];
    private static Dictionary<int, int> TempCurrentOptions = [];
    private static float Polltimer = 60f;
    private static string PollMSG = "";
    private static bool MapPoll;

    public const string Csize = "85%"; // CustomRole Settings Font-Size
    public const string Asize = "75%"; // All Appended Addons Font-Size

    public static List<string> ChatHistory = [];

    private static long LastUpload;

    public static void LoadCommands()
    {
        Command.AllCommands =
        [
            new("Dump", "", Command.UsageLevels.Modded, Command.UsageTimes.Always, DumpCommand, false, false),
            new("Version", "", Command.UsageLevels.Modded, Command.UsageTimes.Always, VersionCommand, true, false),
            new("Answer", "{letter}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, AnswerCommand, false, false, [GetString("CommandArgs.Answer.Letter")]),
            new("ShowQuestion", "", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, ShowQuestionCommand, false, false),
            new("Winner", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, WinnerCommand, true, false),
            new("LastResult", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, LastResultCommand, true, false),
            new("GameResult", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, GameResultCommand, true, false),
            new("KillLog", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, KillLogCommand, true, false),
            new("RoleSummary", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, RoleSummaryCommand, true, false),
            new("GhostInfo", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, GhostInfoCommand, true, false),
            new("ApocalypseInfo", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, ApocalypseInfoCommand, true, false),
            new("CovenInfo", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, CovenInfoCommand, true, false),
            new("ReName", "{name}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, ReNameCommand, true, false, [GetString("CommandArgs.Rename.Name")]),
            new("HideName", "", Command.UsageLevels.Host, Command.UsageTimes.Always, HideNameCommand, true, false),
            new("Level", "{level}", Command.UsageLevels.Host, Command.UsageTimes.Always, LevelCommand, true, false, [GetString("CommandArgs.Level.Level")]),
            new("Now", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, NowCommand, true, false),
            new("Disconnect", "{team}", Command.UsageLevels.Host, Command.UsageTimes.InGame, DisconnectCommand, true, false, [GetString("CommandArgs.Disconnect.Team")]),
            new("Role", "[role]", Command.UsageLevels.Everyone, Command.UsageTimes.Always, RoleCommand, true, false, [GetString("CommandArgs.Role.Role")]),
            new("Factions", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, FactionsCommand, true, false),
            new("Up", "{role}", Command.UsageLevels.Up, Command.UsageTimes.InLobby, UpCommand, true, false, [GetString("CommandArgs.Up.Role")]),
            new("SetPlayers", "{number}", Command.UsageLevels.Host, Command.UsageTimes.InLobby, SetPlayersCommand, true, false, [GetString("CommandArgs.SetPlayers.Number")]),
            new("Help", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, HelpCommand, true, false),
            new("Icons", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, IconsCommand, true, false),
            new("SettingIcons", "", Command.UsageLevels.Host, Command.UsageTimes.Always, SettingIconsCommand, true, false),
            new("KCount", "", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, KCountCommand, true, false),
            new("Vote", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, VoteCommand, true, false, [GetString("CommandArgs.Vote.Id")]),
            new("Death", "", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, DeathCommand, true, false),
            new("MyRole", "", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, MyRoleCommand, true, false),
            new("Me", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.Always, MeCommand, true, false, [GetString("CommandArgs.Me.Id")]),
            new("Template", "{tag}", Command.UsageLevels.Everyone, Command.UsageTimes.Always, TemplateCommand, true, false, [GetString("CommandArgs.Template.Tag")]),
            new("MessageWait", "{duration}", Command.UsageLevels.Host, Command.UsageTimes.Always, MessageWaitCommand, true, false, [GetString("CommandArgs.MessageWait.Duration")]),
            new("TpOut", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TpOutCommand, true, false),
            new("TpIn", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TpInCommand, true, false),
            new("Say", "{message}", Command.UsageLevels.Everyone, Command.UsageTimes.Always, SayCommand, true, false, [GetString("CommandArgs.Say.Message")]),
            new("MId", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, MIdCommand, true, false),
            new("Ban", "{id} [reason]", Command.UsageLevels.Everyone, Command.UsageTimes.Always, BanCommand, true, false, [GetString("CommandArgs.Ban.Id"), GetString("CommandArgs.Ban.Reason")]),
            new("Warn", "{id} [reason]", Command.UsageLevels.Everyone, Command.UsageTimes.Always, WarnCommand, true, false, [GetString("CommandArgs.Warn.Id"), GetString("CommandArgs.Warn.Reason")]),
            new("Kick", "{id} [reason]", Command.UsageLevels.Everyone, Command.UsageTimes.Always, KickCommand, true, false, [GetString("CommandArgs.Kick.Id"), GetString("CommandArgs.Kick.Reason")]),
            new("TagColor", "{color}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TagColorCommand, true, false, [GetString("CommandArgs.TagColor.Color")]),
            new("ModColor", "{color}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, ModColorCommand, true, false, [GetString("CommandArgs.ModColor.Color")]),
            new("VIPColor", "{color}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, VIPColorCommand, true, false, [GetString("CommandArgs.VIPColor.Color")]),
            new("Exe", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, ExeCommand, true, false, [GetString("CommandArgs.Exe.Id")]),
            new("Kill", "{id}", Command.UsageLevels.Host, Command.UsageTimes.InGame, KillCommand, true, false, [GetString("CommandArgs.Kill.Id")]),
            new("Revive", "{id}", Command.UsageLevels.Host, Command.UsageTimes.InGame, ReviveCommand, true, false, [GetString("CommandArgs.Revive.Id")]),
            new("AddMod", "{id}", Command.UsageLevels.Host, Command.UsageTimes.Always, AddModCommand, true, false, [GetString("CommandArgs.AddMod.Id")]),
            new("DeleteMod", "{id}", Command.UsageLevels.Host, Command.UsageTimes.Always, DeleteModCommand, true, false, [GetString("CommandArgs.DeleteMod.Id")]),
            new("AddVIP", "{id}", Command.UsageLevels.Host, Command.UsageTimes.Always, AddVIPCommand, true, false, [GetString("CommandArgs.AddVIP.Id")]),
            new("DeleteVIP", "{id}", Command.UsageLevels.Host, Command.UsageTimes.Always, DeleteVIPCommand, true, false, [GetString("CommandArgs.DeleteVIP.Id")]),
            new("Colour", "{color}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, ColourCommand, true, false, [GetString("CommandArgs.Colour.Color")]),
            new("Quit", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.Always, QuitCommand, true, false, [GetString("CommandArgs.Quit.Id")]),
            new("Xf", "", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, XfCommand, true, false),
            new("Id", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, IdCommand, true, false),
            new("ChangeRole", "{role}", Command.UsageLevels.HostIsDeveloper, Command.UsageTimes.InGame, ChangeRoleCommand, true, false, [GetString("CommandArgs.ChangeRole.Role")]),
            new("End", "", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, EndCommand, true, false),
            new("CosId", "", Command.UsageLevels.Host, Command.UsageTimes.Always, CosIdCommand, true, false),
            new("Meeting", "", Command.UsageLevels.Host, Command.UsageTimes.InGame, MeetingCommand, true, false),
            new("CS", "{sound}", Command.UsageLevels.Host, Command.UsageTimes.Always, CSCommand, true, false, [GetString("CommandArgs.CS.Sound")]),
            new("SD", "{sound}", Command.UsageLevels.Host, Command.UsageTimes.Always, SDCommand, true, false, [GetString("CommandArgs.SD.Sound")]),
            new("Poll", "{question} {answerA} {answerB} [answerC] [answerD] [answerE]", Command.UsageLevels.Host, Command.UsageTimes.InLobby, PollCommand, true, false, [GetString("CommandArgs.Poll.Question"), GetString("CommandArgs.Poll.AnswerA"), GetString("CommandArgs.Poll.AnswerB"), GetString("CommandArgs.Poll.AnswerC"), GetString("CommandArgs.Poll.AnswerD"), GetString("CommandArgs.Poll.AnswerE")]),
            new("PV", "{vote}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, PVCommand, false, false, [GetString("CommandArgs.PV.Vote")]),
            new("RPS", "{number}", Command.UsageLevels.Everyone, Command.UsageTimes.Always, RPSCommand, true, false, [GetString("CommandArgs.RPS.Number")]),
            new("CoinFlip", "", Command.UsageLevels.Everyone, Command.UsageTimes.Always, CoinFlipCommand, true, false),
            new("GNO", "{number}", Command.UsageLevels.Everyone, Command.UsageTimes.Always, GNOCommand, true, false, [GetString("CommandArgs.GNO.Number")]),
            new("Rand", "{number1} {number2}", Command.UsageLevels.Everyone, Command.UsageTimes.Always, RandCommand, true, false, [GetString("CommandArgs.Rand.Number1"), GetString("CommandArgs.Rand.Number2")]),
            new("EightBall", "[question]", Command.UsageLevels.Everyone, Command.UsageTimes.Always, EightBallCommand, true, false, [GetString("CommandArgs.EightBall.Question")]),
            new("Start", "{number}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, StartCommand, true, false, [GetString("CommandArgs.Start.Number")]),
            new("DraftStart", "", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, DraftStartCommand, true, false),
            new("Draft", "{number}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, DraftCommand, true, false, [GetString("CommandArgs.Draft.Number")]),
            new("DraftDescription", "{number}", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, DraftDescriptionCommand, true, false, [GetString("CommandArgs.DraftDescription.Number")]),
            new("Spam", "", Command.UsageLevels.Host, Command.UsageTimes.Always, SpamCommand, true, false),
            new("Fix", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, FixCommand, true, false, [GetString("CommandArgs.Fix.Id")]),
            new("AfkExempt", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, AFKExemptCommand, true, false, [GetString("CommandArgs.AfkExempt.Id")]),
            new("Spectate", "{id}", Command.UsageLevels.Host, Command.UsageTimes.InLobby, SpectateCommand, true, false, [GetString("CommandArgs.Spectate.Id")]),
            new("EnableAllRoles", "", Command.UsageLevels.Host, Command.UsageTimes.InLobby, EnableAllRolesCommand, true, false),
            new("Preset", "{mode} {preset_id}", Command.UsageLevels.Host, Command.UsageTimes.InLobby, PresetCommand, true, false, [GetString("CommandArgs.Preset.Mode"), GetString("CommandArgs.Preset.PresetId")]),
            new("SetRole", "{id} [role]", Command.UsageLevels.Up, Command.UsageTimes.InLobby, SetRoleCommand, true, false, [GetString("CommandArgs.SetRole.Id"), GetString("CommandArgs.SetRole.Role")]),
            new("MapPoll", "", Command.UsageLevels.Host, Command.UsageTimes.InLobby, MapPollCommand, true, false),

            new("Guess", "{id} {role}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Guess.Id"), GetString("CommandArgs.Guess.Role")]),
            new("Trial", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Trial.Id")]),
            new("Finish", "", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false),
            new("Reveal", "", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false),
            new("Compare", "{id1} {id2}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Compare.Id1"), GetString("CommandArgs.Compare.Id2")]),
            new("Duel", "{number}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Duel.Number")]),
            new("Revenge", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.AfterDeath, (_, _, _) => { }, true, false, [GetString("CommandArgs.Revenge.Id")]),
            new("Retributionist", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.AfterDeath, (_, _, _) => { }, true, false, [GetString("CommandArgs.Retributionist.Id")]),
            new("Exorcise", "", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false),
            new("BloodRitual", "{id} {role}", Command.UsageLevels.Everyone, Command.UsageTimes.AfterDeath, (_, _, _) => { }, true, false, [GetString("CommandArgs.BloodRitual.Id"), GetString("CommandArgs.BloodRitual.Role")]),
            new("Medium", "{answer}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Medium.Answer")]),
            new("Summon", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Summon.Id")]),
            new("Swap", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Swap.Id")]),
            new("Expel", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Expel.Id")]),
            new("Imitate", "{id}", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, (_, _, _) => { }, true, false, [GetString("CommandArgs.Imitate.Id")]),
        ];
    }

    public static bool Prefix(ChatController __instance)
    {
        if (__instance.quickChatField.visible == false && __instance.freeChatField.textArea.text == "") return false;
        if (!GameStates.IsModHost && !AmongUsClient.Instance.AmHost) return true;
        __instance.timeSinceLastMessage = 3f;
        var text = __instance.freeChatField.textArea.text;
        if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        var canceled = false;
        if (text.StartsWith("/cmd"))
        {
            if (AmongUsClient.Instance.AmHost) canceled = true;
            text = "/" + text[4..].TrimStart();
        }
        string[] args = text.Trim().Split(' ');
        var cancelVal = "";
        Main.isChatCommand = true;
        Logger.Info(text, "SendChat");
        if ((Options.NewHideMsg.GetBool() || Blackmailer.HasEnabled) && AmongUsClient.Instance.AmHost) // Blackmailer.ForBlackmailer.Contains(PlayerControl.LocalPlayer.PlayerId)) && PlayerControl.LocalPlayer.IsAlive())
        {
            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
        }
        //if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn" && text[..3] != "/rs") args[0] = "/r";
        if (text.Length >= 4) if (text[..3] == "/up") args[0] = "/up";

        if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Judge.TrialMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (President.EndMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Inspector.InspectCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Pirate.DuelCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Councillor cl && cl.MurderMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Nemesis.NemesisMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Retributionist.RetributionistMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Exorcist ex && ex.CheckCommand(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Ritualist.RitualistMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Medium.MsMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Summoner.SummonerCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Swapper sw && sw.SwapMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Dictator dt && dt.ExilePlayer(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Imitator.ImitatorMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Lovers.LoversMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (ImpostorChannel(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (CovenChannel(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Jackal.JackalChannel(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Jailer.JailerChannel(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (RoundUp.DeputyCommand(PlayerControl.LocalPlayer, text)) goto Canceled;
        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        if (Blackmailer.CheckBlackmaile(PlayerControl.LocalPlayer) && PlayerControl.LocalPlayer.IsAlive())
        {
            goto Canceled;
        }
        if (Exorcist.IsExorcismCurrentlyActive() && PlayerControl.LocalPlayer.IsAlive())
        {
            Exorcist.ExorcisePlayer(PlayerControl.LocalPlayer);
            goto Canceled;
        }

        Main.isChatCommand = false;

        if (text.StartsWith('/') && AmongUsClient.Instance.AmHost)
        {
            foreach (Command command in Command.AllCommands)
            {
                if (!command.IsThisCommand(text)) continue;

                Logger.Info($" Recognized command: {text}", "ChatCommand");
                Main.isChatCommand = true;

                if (!command.CanUseCommand(PlayerControl.LocalPlayer, sendErrorMessage: true))
                {
                    canceled = true;
                    break;
                }

                command.Action(PlayerControl.LocalPlayer, text, args);

                if (command.IsCanceled || command.AlwaysHidden) canceled = true;
                break;
            }
        }

        if (text.StartsWith('/') && !AmongUsClient.Instance.AmHost)
        {
            foreach (Command command in Command.AllCommands)
            {
                if (!command.IsThisCommand(text)) continue;
                if (command.Key is not "Dump" and not "Version") continue;

                Logger.Info($" Recognized command: {text}", "ChatCommand");
                Main.isChatCommand = true;

                if (!command.CanUseCommand(PlayerControl.LocalPlayer, sendErrorMessage: true))
                {
                    canceled = true;
                    break;
                }

                command.Action(PlayerControl.LocalPlayer, text, args);

                if (command.IsCanceled || command.AlwaysHidden) canceled = true;
                break;
            }
        }

        goto Skip;
    Canceled:
        Main.isChatCommand = false;
        canceled = true;
    Skip:
        if (!Blackmailer.CheckBlackmaile(PlayerControl.LocalPlayer))
        {
            if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Lovers)
            {
                if (Lovers.LoversMsg(PlayerControl.LocalPlayer, text, false))
                {
                    Main.isChatCommand = true;
                    canceled = true;
                }
            }
            else if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Imp)
            {
                if (ImpostorChannel(PlayerControl.LocalPlayer, text, false))
                {
                    Main.isChatCommand = true;
                    canceled = true;
                }
            }
            else if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Coven)
            {
                if (CovenChannel(PlayerControl.LocalPlayer, text, false))
                {
                    Main.isChatCommand = true;
                    canceled = true;
                }
            }
            else if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Jackal)
            {
                if (Jackal.JackalChannel(PlayerControl.LocalPlayer, text, false))
                {
                    Main.isChatCommand = true;
                    canceled = true;
                }
            }
            else if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Jailer)
            {
                if (Jailer.JailerChannel(PlayerControl.LocalPlayer, text, false))
                {
                    Main.isChatCommand = true;
                    canceled = true;
                }
            }
        }
        if (canceled)
        {
            Logger.Info("Command Canceled", "ChatCommand");
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(cancelVal);

            __instance.quickChatMenu.Clear();
            __instance.quickChatField.Clear();
        }

        return !canceled;
    }

    public static string FixRoleNameInput(string text)
    {
        text = text.Replace("着", "者").Trim().ToLower();
        return text switch
        {
            _ => text,
        };
    }

    public static bool GetRoleByName(string name, out CustomRoles role)
    {
        role = new();

        if (name == "" || name == string.Empty) return false;

        if ((TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.SChinese) == SupportedLangs.SChinese)
        {
            Regex r = new("[\u4e00-\u9fa5]+$");
            MatchCollection mc = r.Matches(name);
            string result = string.Empty;
            for (int i = 0; i < mc.Count; i++)
            {
                if (mc[i].ToString() == "是") continue;
                result += mc[i]; //匹配结果是完整的数字，此处可以不做拼接的
            }
            name = FixRoleNameInput(result.Replace("是", string.Empty).Trim());
        }
        else name = name.Trim().ToLower();

        string nameWithoutId = Regex.Replace(name.Replace(" ", ""), @"^\d+", "");

        if (Options.CrossLanguageGetRole.GetBool())
        {
            foreach (var rl in CustomRolesHelper.AllRoles)
            {
                if (!CrossLangRoleNames.ContainsKey(rl))
                    continue;
                else
                {
                    if (!CrossLangRoleNames[rl].Contains(nameWithoutId))
                        continue;
                    else
                    {
                        role = rl;
                        return true;
                    }
                }
            }
        }
        else
        {
            foreach (var rl in CustomRolesHelper.AllRoles)
            {
                if (rl.IsVanilla()) continue;
                var roleName = GetString(rl.ToString()).ToLower().Trim().Replace(" ", "");
                if (nameWithoutId == roleName)
                {
                    role = rl;
                    return true;
                }
            }
        }
        return false;
    }
    public static CustomRoles ParseRole(string role)
    {
        role = FixRoleNameInput(role).ToLower().Trim().Replace(" ", string.Empty);
        var result = CustomRoles.NotAssigned;

        foreach (var rl in CustomRolesHelper.AllRoles)
        {
            if (rl.IsVanilla()) continue;

            if (Options.CrossLanguageGetRole.GetBool())
            {
                if (!CrossLangRoleNames.ContainsKey(rl))
                    continue;
                else
                {
                    if (!CrossLangRoleNames[rl].Contains(role))
                        continue;
                    else
                    {
                        result = rl;
                        break;
                    }
                }
            }
            else
            {
                var roleName = GetString(rl.ToString());
                if (role == roleName.ToLower().Trim().TrimStart('*').Replace(" ", string.Empty))
                {
                    result = rl;
                    break;
                }
            }
        }

        return result;
    }

    public static void SendRolesInfo(string role, byte playerId, bool isDev = false, bool isUp = false)
    {
        if (GameModeBase.GetGameMode() is not CustomGameMode.Standard and not CustomGameMode.HidenSeekTONE)
        {
            Utils.SendMessage(GetString($"ModeDescribe.{GameModeBase.GetGameMode()}"), playerId, sendOption: SendOption.None);
            return;
        }
        role = role.Trim().ToLower();
        if (role.StartsWith("/r")) _ = role.Replace("/r", string.Empty);
        if (role.StartsWith("/up")) _ = role.Replace("/up", string.Empty);
        if (role.EndsWith("\r\n")) _ = role.Replace("\r\n", string.Empty);
        if (role.EndsWith("\n")) _ = role.Replace("\n", string.Empty);
        if (role.StartsWith("/bt")) _ = role.Replace("/bt", string.Empty);
        if (role.StartsWith("/rt")) _ = role.Replace("/rt", string.Empty);

        if (role == "" || role == string.Empty)
        {
            Utils.ShowActiveRoles(playerId);
            return;
        }

        var result = ParseRole(role);

        if (result == CustomRoles.NotAssigned)
        {
            Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), playerId, sendOption: SendOption.None);
            return;
        }

        bool shouldDevAssign = isDev || isUp;

        if (result is CustomRoles.GM or CustomRoles.Mini || result.IsGhostRole() && !isDev
            || result.GetCount() < 1 || result.GetMode() == 0)
        {
            shouldDevAssign = false;
        }

        byte pid = playerId == 255 ? (byte)0 : playerId;

        if (isUp)
        {
            if (result.IsGhostRole() || !shouldDevAssign || result.IsAddonAssignedMidGame() || (result.NotAssignInVanillaServer() && Main.CurrentServerIsVanilla) || (result.NotSpawnInRoundUp() && Options.CurrentGameMode == CustomGameMode.RoundUp))
            {
                Utils.SendMessage(string.Format(GetString("Message.YTPlanSelectFailed"), Translator.GetActualRoleName(result)), playerId, sendOption: SendOption.None);
                return;
            }

            GhostRoleAssign.forceRole.Remove(pid);

            if (result.IsAdditionRole())
            {
                if (!AddonAssign.SetAddOns.ContainsKey(pid)) AddonAssign.SetAddOns[pid] = [];

                if (!AddonAssign.SetAddOns[pid].Contains(result))
                    AddonAssign.SetAddOns[pid].Add(result);
            }
            else
                RoleAssign.SetRoles[pid] = result;

            Utils.SendMessage(string.Format(GetString("Message.YTPlanSelected"), Translator.GetActualRoleName(result)), playerId, sendOption: SendOption.None);
            return;
        }

        if (isDev && shouldDevAssign)
        {
            if (result.IsGhostRole() && !result.IsAdditionRole())
            {
                CustomRoles setrole = result.GetCustomRoleTeam() switch
                {
                    Custom_Team.Impostor => CustomRoles.ImpostorTONE,
                    _ => CustomRoles.CrewmateTONE

                };
                RoleAssign.SetRoles[pid] = setrole;
                GhostRoleAssign.forceRole[pid] = result;
            }
        }


        var Des = result.GetInfoLong();
        var title = "▲" + $"<color=#ffffff>" + result.GetRoleTitle() + "</color>\n";
        var Conf = new StringBuilder();
        string rlHex = Utils.GetRoleColorCode(result);
        if (Options.CustomRoleSpawnChances.ContainsKey(result))
        {
            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[result], ref Conf);
            var cleared = Conf.ToString();
            var Setting = $"<color={rlHex}>{GetString(result.ToString())} {GetString("Settings:")}</color>\n";
            Conf.Clear().Append($"<color=#ffffff>" + $"<size={Csize}>" + Setting + cleared + "</size>" + "</color>");

        }
        // Show role info
        Utils.SendMessage(Des, playerId, title, noReplay: true);

        // Show role settings
        Utils.SendMessage("", playerId, Conf.ToString(), noReplay: true);
        return;
    }
    public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;
        if (!AmongUsClient.Instance.AmHost) return;

        if (!Blackmailer.CheckBlackmaile(player)) ChatManager.SendMessage(player, text);

        if (text.StartsWith("\n")) text = text[1..];
        if (text.StartsWith("/cmd"))
        {
            canceled = true;
            text = "/" + text[4..].TrimStart();
        }
        //if (!text.StartsWith("/")) return;
        string[] args = text.Split(' ');

        //if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn") args[0] = "/r";
        //   if (SpamManager.CheckSpam(player, text)) return;
        if (GuessManager.GuesserMsg(player, text)) { canceled = true; Logger.Info($"Is Guesser command", "OnReceiveChat"); return; }
        if (Judge.TrialMsg(player, text)) { canceled = true; Logger.Info($"Is Judge command", "OnReceiveChat"); return; }
        if (President.EndMsg(player, text)) { canceled = true; Logger.Info($"Is President command", "OnReceiveChat"); return; }
        if (Inspector.InspectCheckMsg(player, text)) { canceled = true; Logger.Info($"Is Inspector command", "OnReceiveChat"); return; }
        if (Pirate.DuelCheckMsg(player, text)) { canceled = true; Logger.Info($"Is Pirate command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Councillor cl && cl.MurderMsg(player, text)) { canceled = true; Logger.Info($"Is Councillor command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Swapper sw && sw.SwapMsg(player, text)) { canceled = true; Logger.Info($"Is Swapper command", "OnReceiveChat"); return; }
        if (Medium.MsMsg(player, text)) { Logger.Info($"Is Medium command", "OnReceiveChat"); return; }
        if (Nemesis.NemesisMsgCheck(player, text)) { Logger.Info($"Is Nemesis Revenge command", "OnReceiveChat"); return; }
        if (Retributionist.RetributionistMsgCheck(player, text)) { Logger.Info($"Is Retributionist Revenge command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Exorcist ex && ex.CheckCommand(player, text)) { canceled = true; Logger.Info($"Is Exorcist command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Dictator dt && dt.ExilePlayer(player, text)) { canceled = true; Logger.Info($"Is Dictator command", "OnReceiveChat"); return; }
        if (Ritualist.RitualistMsgCheck(player, text)) { canceled = true; Logger.Info($"Is Ritualist command", "OnReceiveChat"); return; }
        if (Summoner.SummonerCheckMsg(player, text)) { canceled = true; Logger.Info($"Is Summoner command", "OnReceiveChat"); return; }
        if (Imitator.ImitatorMsg(player, text)) { canceled = true; Logger.Info($"Is Imitator command", "OnReceiveChat"); return; }
        if (Lovers.LoversMsg(player, text)) { canceled = true; Logger.Info($"Is Lovers Private Chat", "OnReceiveChat"); return; }
        if (ImpostorChannel(player, text)) { canceled = true; Logger.Info($"Is Impostor Channel", "OnReceiveChat"); return; }
        if (CovenChannel(player, text)) { canceled = true; Logger.Info($"Is Coven Channel", "OnReceiveChat"); return; }
        if (Jackal.JackalChannel(player, text)) { canceled = true; Logger.Info($"Is Jackal Channel", "OnReceiveChat"); return; }
        if (Jailer.JailerChannel(player, text)) { canceled = true; Logger.Info($"Is Jailer Channel", "OnReceiveChat"); return; }
        if (RoundUp.DeputyCommand(player, text)) { canceled = true; Logger.Info($"Is RoundUp Command", "OnReceiveChat"); return; }

        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        if (Blackmailer.CheckBlackmaile(player) && player.IsAlive() && !player.IsHost())
        {
            Logger.Info($"This player (id {player.PlayerId}) was Blackmailed", "OnReceiveChat");
            ChatManager.SendPreviousMessagesToAll();
            ChatManager.cancel = false;
            canceled = true;
            return;
        }
        if (Exorcist.IsExorcismCurrentlyActive() && player.IsAlive() && !player.IsHost())
        {
            Logger.Info($"This player (id {player.PlayerId}) was Exorcised", "OnReceiveChat");
            Exorcist.ExorcisePlayer(player);
            canceled = true;
            return;
        }

        if (text.StartsWith('/') && (!GameStates.IsMeeting || MeetingHud.Instance.state is not MeetingHud.VoteStates.Results and not MeetingHud.VoteStates.Proceeding))
        {
            foreach (Command command in Command.AllCommands)
            {
                if (!command.IsThisCommand(text)) continue;
                if (command.Key is "Dump" or "Version") continue;

                Logger.Info($" Recognized command: {text}", "ReceiveChat");

                if (!command.CanUseCommand(player, sendErrorMessage: true))
                {
                    canceled = true;
                    break;
                }

                command.Action(player, text, args);
                if (command.IsCanceled) canceled |= command.AlwaysHidden;
                break;
            }
        }

        if (SpamManager.CheckSpam(player, text)) return;
    }

    private static void DumpCommand(PlayerControl player, string text, string[] args)
    {
        Utils.DumpLog();
    }

    private static void VersionCommand(PlayerControl player, string text, string[] args)
    {
        string version_text = "";
        var target = player;
        var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
        var name = target?.Data?.PlayerName;
        try
        {
            foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key).ToArray())
            {
                var pc = Utils.GetClientById(kvp.Key)?.Character;
                version_text += $"{kvp.Key}/{(pc?.PlayerId != null ? pc.PlayerId.ToString() : "null")}:{pc?.GetRealName(clientData: true) ?? "null"}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
            }
            if (version_text != "")
            {
                target.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(target, version_text);
                target.SetName(name);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.Message, "/version");
            version_text = "Error while getting version : " + e.Message;
            if (version_text != "")
            {
                target.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(target, version_text);
                target.SetName(name);
            }
        }
    }

    private static void AnswerCommand(PlayerControl player, string text, string[] args)
    {
        Quizmaster.AnswerByChat(player, args);
    }

    private static void ShowQuestionCommand(PlayerControl player, string text, string[] args)
    {
        Quizmaster.ShowQuestion(player);
    }

    private static void WinnerCommand(PlayerControl player, string text, string[] args)
    {
        if (Main.winnerNameList.Count == 0) Utils.SendMessage(GetString("NoInfoExists"), player.PlayerId, sendOption: SendOption.None);
        else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList), player.PlayerId);
    }

    private static void LastResultCommand(PlayerControl player, string text, string[] args)
    {
        Utils.ShowKillLog(player.PlayerId);
        Utils.ShowLastRoles(player.PlayerId);
        Utils.ShowLastResult(player.PlayerId);
    }

    private static void GameResultCommand(PlayerControl player, string text, string[] args)
    {
        Utils.ShowLastResult(player.PlayerId);
    }

    private static void KillLogCommand(PlayerControl player, string text, string[] args)
    {
        Utils.ShowKillLog(player.PlayerId);
    }

    private static void RoleSummaryCommand(PlayerControl player, string text, string[] args)
    {
        Utils.ShowLastRoles(player.PlayerId);
    }

    private static void GhostInfoCommand(PlayerControl player, string text, string[] args)
    {
        Utils.SendMessage(GetString("Message.GhostRoleInfo"), player.PlayerId);
    }

    private static void ApocalypseInfoCommand(PlayerControl player, string text, string[] args)
    {
        Utils.SendMessage(GetString("Message.ApocalypseInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Apocalypse), GetString("ApocalypseInfoTitle")));
    }

    private static void CovenInfoCommand(PlayerControl player, string text, string[] args)
    {
        Utils.SendMessage(GetString("Message.CovenInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Coven), GetString("CovenInfoTitle")));
    }

    private static void ReNameCommand(PlayerControl player, string text, string[] args)
    {
        if (Options.PlayerCanSetName.GetBool() || player.FriendCode.CanUseDev() || player.FriendCode.GetDevUser().NameCmd || TagManager.ReadPermission(player.FriendCode) >= 1 ||
            player.IsHost())
        {
            if (args.Length < 1) return;
            if (args.Skip(1).Join(delimiter: " ").Length is > 10 or < 1 || args.Skip(1).Join(delimiter: " ")[0] == '<') // <#ffffff>E is a valid name without this
            {
                Utils.SendMessage(GetString("Message.AllowNameLength"), player.PlayerId, sendOption: SendOption.None);
                return;
            }
            var temp = args.Skip(1).Join(delimiter: " ");
            if (player.IsHost()) Main.HostRealName = temp;
            Main.AllPlayerNames[player.PlayerId] = temp;
            Utils.SendMessage(string.Format(GetString("Message.SetName"), temp), player.PlayerId);
        }
        else
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
        }
    }

    private static void HideNameCommand(PlayerControl player, string text, string[] args)
    {
        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
        GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
            ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                ? $"<color={Main.HideColor.Value}>TONE</color>"
                : $"<color={Main.ModColor}>TONE</color>";
    }

    private static void LevelCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        Utils.SendMessage(string.Format(GetString("Message.SetLevel"), subArgs), player.PlayerId);
        _ = int.TryParse(subArgs, out int input);
        if (input is < 1 or > 999)
        {
            Utils.SendMessage(GetString("Message.AllowLevelRange"), player.PlayerId);
            return;
        }
        var number = Convert.ToUInt32(input);
        player.RpcSetLevel(number - 1);
    }

    private static void NowCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        switch (subArgs)
        {
            case "r":
            case "roles":
            case "funções":
                Utils.ShowActiveRoles(player.PlayerId);
                break;
            case "a":
            case "all":
            case "tudo":
                Utils.ShowAllActiveSettings(player.PlayerId);
                break;
            default:
                Utils.ShowActiveSettings(player.PlayerId);
                break;
        }
    }

    private static void DisconnectCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        switch (subArgs)
        {
            case "crew":
            case "tripulante":
            case "船员":
                GameManager.Instance.enabled = false;
                Utils.NotifyGameEnding();
                GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
                break;

            case "imp":
            case "impostor":
            case "内鬼":
            case "伪装者":
                GameManager.Instance.enabled = false;
                Utils.NotifyGameEnding();
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                break;

            default:
                if (!HudManager.InstanceExists) break;
                HudManager.Instance.Chat.AddChat(player, "crew | imp");
                if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Brazilian)
                {
                    HudManager.Instance.Chat.AddChat(player, "tripulante | impostor");
                }
                break;
        }
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
    }

    private static void RoleCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = "";
        if (text.Contains("/role") || text.Contains("/роль"))
            subArgs = text.Remove(0, 5);
        else
            subArgs = text.Remove(0, 2);
        SendRolesInfo(subArgs, player.PlayerId);
    }

    private static void FactionsCommand(PlayerControl player, string text, string[] args)
    {
        var impCount = $"{GetString("NumberOfImpostors")}: {GameOptionsManager.Instance.GameHostOptions.NumImpostors}";
        if (Options.UseVariableImp.GetBool()) impCount = $"{GetString("ImpRolesMinPlayer")}: {Options.ImpRolesMinPlayer.GetInt()}\n{GetString("ImpRolesMaxPlayer")}: {Options.ImpRolesMaxPlayer.GetInt()}";
        var nnkCount = $"{GetString("NonNeutralKillingRolesMinPlayer")}: {Options.NonNeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NonNeutralKillingRolesMaxPlayer")}: {Options.NonNeutralKillingRolesMaxPlayer.GetInt()}";
        var nkCount = $"{GetString("NeutralKillingRolesMinPlayer")}: {Options.NeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NeutralKillingRolesMaxPlayer")}: {Options.NeutralKillingRolesMaxPlayer.GetInt()}";
        var apocCount = $"{GetString("NeutralApocalypseRolesMinPlayer")}: {Options.NeutralApocalypseRolesMinPlayer.GetInt()}\n{GetString("NeutralApocalypseRolesMaxPlayer")}: {Options.NeutralApocalypseRolesMaxPlayer.GetInt()}";
        var covCount = $"{GetString("CovenRolesMinPlayer")}: {Options.CovenRolesMinPlayer.GetInt()}\n{GetString("CovenRolesMaxPlayer")}: {Options.CovenRolesMaxPlayer.GetInt()}";
        var addonCount = $"{GetString("NoLimitAddonsNumMax")}: {Options.NoLimitAddonsNumMax.GetInt()}";
        Utils.SendMessage($"{impCount}\n{nnkCount}\n{nkCount}\n{apocCount}\n{covCount}\n{addonCount}", player.PlayerId, $"<color={Main.ModColor}>{GetString("FactionSettingsTitle")}</color>");
    }

    private static void UpCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = text.Remove(0, 3);
        if (!Options.EnableUpMode.GetBool())
        {
            Utils.SendMessage(string.Format(GetString("Message.YTPlanDisabled"), GetString("EnableYTPlan")), player.PlayerId);
            return;
        }
        SendRolesInfo(subArgs, player.PlayerId, isUp: true);
    }

    private static void SetPlayersCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        var numbereer = Convert.ToByte(subArgs);
        if (numbereer > 15 && GameStates.IsVanillaServer)
        {
            Utils.SendMessage(GetString("Message.MaxPlayersFailByRegion"));
            return;
        }
        Utils.SendMessage(GetString("Message.MaxPlayers") + numbereer);
        if (GameStates.IsNormalGame)
            GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers = numbereer;

        else if (GameStates.IsHideNSeek)
            GameOptionsManager.Instance.currentHideNSeekGameOptions.MaxPlayers = numbereer;
    }

    private static void HelpCommand(PlayerControl player, string text, string[] args)
    {
        if (player.IsHost())
        {
            Utils.ShowHelp(player.PlayerId);
        }
        else
        {
            Utils.ShowHelpToClient(player.PlayerId);
        }
    }

    private static void IconsCommand(PlayerControl player, string text, string[] args)
    {
        Utils.SendMessage(GetString("Command.icons"), player.PlayerId, GetString("IconsTitle"), ShouldSplit: true);
    }

    private static void SettingIconsCommand(PlayerControl player, string text, string[] args)
    {
        Utils.SendMessage(string.Format(GetString("Command.sicons"), Options.BalanceNeedPlayers.GetInt()), player.PlayerId, GetString("IconsTitle"));
    }

    private static void KCountCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.EnableKillerLeftCommand.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        var sub = new StringBuilder();

        GameModeBase.GetGameMode().GetGameModeClass().AppendKcount(sub);

        Utils.SendMessage(sub.ToString(), player.PlayerId);
    }

    private static void VoteCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length != 2 ? "" : args[1];
        if (subArgs == "" || !int.TryParse(subArgs, out int arg))
            return;
        var plr = Utils.GetPlayerById(arg);

        if (!Options.EnableVoteCommand.GetBool())
        {
            Utils.SendMessage(GetString("VoteDisabled"), player.PlayerId);
            return;
        }

        if (MeetingHud.Instance && MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.Animating or MeetingHud.VoteStates.Results)
        {
            Utils.SendMessage(GetString("UseVoteCommandDuringDiscussion"), player.PlayerId);
            return;
        }
        if (Options.CurrentGameMode == CustomGameMode.RoundUp && RoundUp.Deputy != byte.MaxValue && PlayerControl.LocalPlayer.PlayerId == RoundUp.Deputy)
        {
            Utils.SendMessage(GetString("RoundUp_Help"), player.PlayerId);
            return;
        }

        if (arg != 253) // skip
        {
            if (plr == null || !plr.IsAlive())
            {
                Utils.SendMessage(GetString("VoteDead"), player.PlayerId);
                return;
            }
        }
        if (!player.IsAlive())
        {
            Utils.SendMessage(GetString("CannotVoteWhenDead"), player.PlayerId);
            return;
        }
        if (GameStates.IsMeeting)
        {
            player.RpcCastVote((byte)arg);
        }
    }

    private static void DeathCommand(PlayerControl player, string text, string[] args)
    {
        if (player.IsAlive())
        {
            Utils.SendMessage(string.Format(GetString("DeathCmd.NotDead"), player.GetRealName(), player.GetCustomRole().ToColoredString()), player.PlayerId, sendOption: SendOption.None);
        }
        else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Vote)
        {
            Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Ejected"), sendTo: player.PlayerId);
        }
        else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Shrouded)
        {
            Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Shrouded"), sendTo: player.PlayerId);
        }
        else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.FollowingSuicide)
        {
            Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Lovers"), sendTo: player.PlayerId);
        }
        else
        {
            var killer = player.GetRealKiller(out var MurderRole);
            string killerName = killer == null ? "N/A" : killer.GetRealName(clientData: true);
            string killerRole = killer == null ? "N/A" : Utils.GetRoleName(MurderRole);
            Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(player.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killerName + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{killerRole}</color>" + "</b>", sendTo: player.PlayerId);
        }
    }

    private static void MyRoleCommand(PlayerControl player, string text, string[] args)
    {
        var role = player.GetCustomRole();
        var lp = player;
        var Des = lp.GetRoleInfo(true);
        var title = $"<color=#ffffff>" + role.GetRoleTitle() + "</color>\n";
        var Conf = new StringBuilder();
        var Sub = new StringBuilder();
        var rlHex = Utils.GetRoleColorCode(role);
        var SubTitle = $"<color={rlHex}>" + GetString("YourAddon") + "</color>\n";

        if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref Conf);
        var cleared = Conf.ToString();
        var Setting = $"<color={rlHex}>{GetString(role.ToString())} {GetString("Settings:")}</color>\n";
        Conf.Clear().Append($"<color=#ffffff>" + $"<size={Csize}>" + Setting + cleared + "</size>" + "</color>");

        foreach (var subRole in Main.PlayerStates[lp.PlayerId].SubRoles.ToArray())
            Sub.Append($"\n\n" + $"<size={Asize}>" + Utils.GetRoleTitle(subRole) + Utils.GetInfoLong(subRole) + "</size>");

        if (Sub.ToString() != string.Empty)
        {
            var ACleared = Sub.ToString().Remove(0, 2);
            ACleared = ACleared.Length > 1200 ? $"<size={Asize}>" + ACleared.RemoveHtmlTags() + "</size>" : ACleared;
            Sub.Clear().Append(ACleared);
        }

        Utils.SendMessage(Des, lp.PlayerId, title, noReplay: true);
        Utils.SendMessage("", lp.PlayerId, Conf.ToString(), noReplay: true);
        if (Sub.ToString() != string.Empty) Utils.SendMessage(Sub.ToString(), lp.PlayerId, SubTitle, noReplay: true);
    }

    private static void MeCommand(PlayerControl player, string text, string[] args)
    {
        string Devbox = player.FriendCode.GetDevUser().DeBug ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
        string UpBox = player.FriendCode.GetDevUser().IsUp ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
        string ColorBox = player.FriendCode.GetDevUser().ColorCmd ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";

        var subArgs = text.Length == 3 ? string.Empty : text.Remove(0, 3);
        if (string.IsNullOrEmpty(subArgs))
        {
            Utils.SendMessage((player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), player.PlayerId, player.GetRealName(clientData: true), player.GetClient().FriendCode, player.GetClient().GetHashedPuid(), player.FriendCode.GetDevUser().GetUserType(), Devbox, UpBox, ColorBox)}", player.PlayerId);
        }
        else
        {
            var tagCanMe = TagManager.ReadPermission(player.FriendCode) >= 2;
            if ((Options.ApplyModeratorList.GetValue() == 0 || !Utils.IsPlayerModerator(player.FriendCode)) && !tagCanMe && !player.FriendCode.CanUseDev() && !player.IsHost())
            {
                Utils.SendMessage(GetString("Message.MeCommandNoPermission"), player.PlayerId);
                return;
            }

            if (byte.TryParse(subArgs, out byte meid))
            {
                if (meid != player.PlayerId)
                {
                    var targetplayer = Utils.GetPlayerById(meid);
                    if (targetplayer != null && targetplayer.GetClient() != null)
                    {
                        Utils.SendMessage($"{string.Format(GetString("Message.MeCommandTargetInfo"), targetplayer.PlayerId, targetplayer.GetRealName(clientData: true), targetplayer.GetClient().FriendCode, targetplayer.GetClient().GetHashedPuid(), targetplayer.FriendCode.GetDevUser().GetUserType())}", player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage($"{(GetString("Message.MeCommandInvalidID"))}", player.PlayerId);
                    }
                }
                else
                {
                    Utils.SendMessage($"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser().GetUserType(), Devbox, UpBox, ColorBox)}", player.PlayerId);
                }
            }
            else
            {
                Utils.SendMessage($"{(GetString("Message.MeCommandInvalidID"))}", player.PlayerId);
            }
        }
    }

    private static void TemplateCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length > 1)
        {
            if (player.IsHost())
            {
                TemplateManager.SendTemplate(args[1]);
            }
            else TemplateManager.SendTemplate(args[1], player.PlayerId);
        }
        else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId, sendOption: SendOption.None);
    }

    private static void MessageWaitCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length > 1 && int.TryParse(args[1], out int sec))
        {
            Main.MessageWait.Value = sec;
            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
        }
        else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
    }

    private static void TpOutCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.PlayerCanUseTP.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        player.RpcTeleport(new Vector2(0.1f, 3.8f));
    }

    private static void TpInCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.PlayerCanUseTP.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        player.RpcTeleport(new Vector2(-0.2f, 1.3f));
    }

    private static void SayCommand(PlayerControl player, string text, string[] args)
    {
        if (player.IsHost())
        {
            if (args.Length > 1)
            {
                Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                return;
            }
        }
        else if (player.FriendCode.CanUseDev())
        {
            if (args.Length > 1)
            {
                Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color={Main.ModColor}>{GetString("MessageFromDev")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                return;
            }
        }
        else if (player.FriendCode.IsDevUser())
        {
            if (args.Length > 1)
            {
                Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#4bc9b0>{GetString("MessageFromSponsor")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                return;
            }
        }
        else if (Utils.IsPlayerModerator(player.FriendCode) || TagManager.CanUseSayCommand(player.FriendCode))
        {
            if (!TagManager.CanUseSayCommand(player.FriendCode) && (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowSayCommand.GetBool() == false))
            {
                Utils.SendMessage(GetString("SayCommandDisabled"), player.PlayerId);
                return;
            }
            else
            {
                var modTitle = (Utils.IsPlayerModerator(player.FriendCode) || TagManager.ReadPermission(player.FriendCode) >= 2) ? $"<color=#8bbee0>{GetString("MessageFromModerator")}" : $"<color=#ffff00>{GetString("MessageFromVIP")}";
                if (args.Length > 1)
                    Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"{modTitle} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                string modLogname3 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n4) ? n4 : "";

                string moderatorFriendCode3 = player.FriendCode.ToString();
                string logMessage3 = $"[{DateTime.Now}] {moderatorFriendCode3},{modLogname3} used /s: {args.Skip(1).Join(delimiter: " ")}";
                File.AppendAllText(modLogFiles, logMessage3 + Environment.NewLine);

            }
        }
    }

    private static void MIdCommand(PlayerControl player, string text, string[] args)
    {
        var tagCanUse = TagManager.ReadPermission(player.FriendCode) >= 2 || player.IsHost();
        //checking if modlist on or not
        //checking if player is has necessary privellege or not
        if (!tagCanUse && !Utils.IsPlayerModerator(player.FriendCode))
        {
            Utils.SendMessage(GetString("midCommandNoAccess"), player.PlayerId);
            return;
        }
        if (!tagCanUse && Options.ApplyModeratorList.GetValue() == 0)
        {
            Utils.SendMessage(GetString("midCommandDisabled"), player.PlayerId);
            return;
        }
        string msgText1 = GetString("PlayerIdList");
        foreach (var pc in Main.EnumeratePlayerControls())
        {
            if (pc == null) continue;
            msgText1 += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
        }
        Utils.SendMessage(msgText1, player.PlayerId);
    }

    private static void BanCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = "";
        var tagCanBan = TagManager.ReadPermission(player.FriendCode) >= 5;
        // Check if the ban command is enabled in the settings
        if (!tagCanBan && Options.ApplyModeratorList.GetValue() == 0)
        {
            Utils.SendMessage(GetString("BanCommandDisabled"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        // Check if the player has the necessary privileges to use the command
        if (!tagCanBan && !Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.CanUseDev() && !player.IsHost())
        {
            Utils.SendMessage(GetString("BanCommandNoAccess"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        string banReason;
        if (args.Length < 3)
        {
            Utils.SendMessage(GetString("BanCommandNoReason"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        else
        {
            subArgs = args[1];
            banReason = string.Join(" ", args.Skip(2));
        }
        //subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte banPlayerId))
        {
            Utils.SendMessage(GetString("BanCommandInvalidID"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        if (banPlayerId == 0)
        {
            Utils.SendMessage(GetString("BanCommandBanHost"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        var bannedPlayer = Utils.GetPlayerById(banPlayerId);
        if (bannedPlayer == null)
        {
            Utils.SendMessage(GetString("BanCommandInvalidID"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        // Prevent moderators from banning other moderators
        if ((Utils.IsPlayerModerator(bannedPlayer.FriendCode) || TagManager.ReadPermission(bannedPlayer.FriendCode) >= 5) && !player.IsHost())
        {
            Utils.SendMessage(GetString("BanCommandBanMod"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        // Ban the specified player
        AmongUsClient.Instance.KickPlayer(bannedPlayer.GetClientId(), true);
        string bannedPlayerName = bannedPlayer.GetRealName();
        string textToSend1 = $"{bannedPlayerName} {GetString("BanCommandBanned")}{player.name} \nReason: {banReason}\n";
        if (GameStates.IsInGame)
        {
            textToSend1 += $" {GetString("BanCommandBannedRole")} {GetString(bannedPlayer.GetCustomRole().ToString())}";
        }
        Utils.SendMessage(textToSend1);
        string modLogname = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n1) ? n1 : "";
        string banlogname = Main.AllPlayerNames.TryGetValue(bannedPlayer.PlayerId, out var n11) ? n11 : "";
        string moderatorFriendCode = player.FriendCode.ToString();
        string bannedPlayerFriendCode = bannedPlayer.FriendCode.ToString();
        string bannedPlayerHashPuid = bannedPlayer.GetClient().GetHashedPuid();
        string logMessage = $"[{DateTime.Now}] {moderatorFriendCode},{modLogname} Banned: {bannedPlayerFriendCode},{bannedPlayerHashPuid},{banlogname} Reason: {banReason}";
        File.AppendAllText(modLogFiles, logMessage + Environment.NewLine);
    }

    private static void WarnCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = "";
        var tagCanWarn = TagManager.ReadPermission(player.FriendCode) >= 2;
        if (!tagCanWarn && Options.ApplyModeratorList.GetValue() == 0)
        {
            Utils.SendMessage(GetString("WarnCommandDisabled"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        if (!tagCanWarn && !Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.CanUseDev() && !player.IsHost())
        {
            Utils.SendMessage(GetString("WarnCommandNoAccess"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte warnPlayerId))
        {
            Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        if (warnPlayerId == 0)
        {
            Utils.SendMessage(GetString("WarnCommandWarnHost"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        var warnedPlayer = Utils.GetPlayerById(warnPlayerId);
        if (warnedPlayer == null)
        {
            Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        // Prevent moderators from warning other moderators
        if ((Utils.IsPlayerModerator(warnedPlayer.FriendCode) || TagManager.ReadPermission(warnedPlayer.FriendCode) >= 2) && !player.IsHost())
        {
            Utils.SendMessage(GetString("WarnCommandWarnMod"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        // warn the specified player
        string warnReason = "Reason : Not specified\n";
        string warnedPlayerName = warnedPlayer.GetRealName();
        //textToSend2 = $" {warnedPlayerName} {GetString("WarnCommandWarned")} ~{player.name}";
        if (args.Length > 2)
        {
            warnReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
        }
        else
        {
            Utils.SendMessage("Use /warn [id] [reason] in future. \nExample :-\n /warn 5 lava chatting", player.PlayerId);
        }
        Utils.SendMessage($" {warnedPlayerName} {GetString("WarnCommandWarned")} {warnReason} ~{player.name}");
        string modLogname1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n2) ? n2 : "";
        string warnlogname = Main.AllPlayerNames.TryGetValue(warnedPlayer.PlayerId, out var n12) ? n12 : "";
        string moderatorFriendCode1 = player.FriendCode.ToString();
        string warnedPlayerFriendCode = warnedPlayer.FriendCode.ToString();
        string warnedPlayerHashPuid = warnedPlayer.GetClient().GetHashedPuid();
        string logMessage1 = $"[{DateTime.Now}] {moderatorFriendCode1},{modLogname1} Warned: {warnedPlayerFriendCode},{warnedPlayerHashPuid},{warnlogname} Reason: {warnReason}";
        File.AppendAllText(modLogFiles, logMessage1 + Environment.NewLine);
    }

    private static void KickCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = "";
        var tagCanKick = TagManager.ReadPermission(player.FriendCode) >= 4;
        // Check if the kick command is enabled in the settings
        if (!tagCanKick && Options.ApplyModeratorList.GetValue() == 0)
        {
            Utils.SendMessage(GetString("KickCommandDisabled"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        // Check if the player has the necessary privileges to use the command
        if (!tagCanKick && !Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.CanUseDev() && !player.IsHost())
        {
            Utils.SendMessage(GetString("KickCommandNoAccess"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte kickPlayerId))
        {
            Utils.SendMessage(GetString("KickCommandInvalidID"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        if (kickPlayerId == 0)
        {
            Utils.SendMessage(GetString("KickCommandKickHost"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        var kickedPlayer = Utils.GetPlayerById(kickPlayerId);
        if (kickedPlayer == null)
        {
            Utils.SendMessage(GetString("KickCommandInvalidID"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        // Prevent moderators from kicking other moderators
        if ((Utils.IsPlayerModerator(kickedPlayer.FriendCode) || TagManager.ReadPermission(kickedPlayer.FriendCode) >= 4) && !player.IsHost())
        {
            Utils.SendMessage(GetString("KickCommandKickMod"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        // Kick the specified player
        AmongUsClient.Instance.KickPlayer(kickedPlayer.GetClientId(), false);
        string kickedPlayerName = kickedPlayer.GetRealName();
        string kickReason = "Reason : Not specified\n";
        if (args.Length > 2)
            kickReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
        else
        {
            Utils.SendMessage("Use /kick [id] [reason] in future. \nExample :-\n /kick 5 not following rules", player.PlayerId);
        }
        string textToSend = $"{kickedPlayerName} {GetString("KickCommandKicked")} {player.name} \n {kickReason}";

        if (GameStates.IsInGame)
        {
            textToSend += $" {GetString("KickCommandKickedRole")} {GetString(kickedPlayer.GetCustomRole().ToString())}";
        }
        Utils.SendMessage(textToSend);
        string modLogname2 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n3) ? n3 : "";
        string kicklogname = Main.AllPlayerNames.TryGetValue(kickedPlayer.PlayerId, out var n13) ? n13 : "";

        string moderatorFriendCode2 = player.FriendCode.ToString();
        string kickedPlayerFriendCode = kickedPlayer.FriendCode.ToString();
        string kickedPlayerHashPuid = kickedPlayer.GetClient().GetHashedPuid();
        string logMessage2 = $"[{DateTime.Now}] {moderatorFriendCode2},{modLogname2} Kicked: {kickedPlayerFriendCode},{kickedPlayerHashPuid},{kicklogname} Reason: {kickReason}";
        File.AppendAllText(modLogFiles, logMessage2 + Environment.NewLine);
    }

    private static void TagColorCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = "";
        string name1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n) ? n : "";
        if (name1 == "") return;
        if (!name1.Contains('\r') && player.FriendCode.GetDevUser().HasTag())
        {
            subArgs = args.Length != 2 ? "" : args[1];
            if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
            {
                Logger.Msg($"{subArgs}", "tagcolor");
                Utils.SendMessage(GetString("TagColorInvalidHexCode"), player.PlayerId);
                return;
            }
            string tagColorFilePath = $"{sponsorTagsFiles}/{player.FriendCode}.txt";
            if (!File.Exists(tagColorFilePath))
            {
                Logger.Msg($"File Not exist, creating file at {tagColorFilePath}", "tagcolor");
                File.Create(tagColorFilePath).Close();
            }

            File.WriteAllText(tagColorFilePath, $"{subArgs}");
        }
    }

    private static void ModColorCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = "";
        if (Options.ApplyModeratorList.GetValue() == 0)
        {
            Utils.SendMessage(GetString("ColorCommandDisabled"), player.PlayerId);
            return;
        }
        if (!Utils.IsPlayerModerator(player.FriendCode))
        {
            Utils.SendMessage(GetString("ColorCommandNoAccess"), player.PlayerId);
            return;
        }
        if (player.IsHost())
        {
            Utils.SendMessage(GetString("Message.CanNotUseByHost"), player.PlayerId);
            return;
        }
        if (!Options.GradientTagsOpt.GetBool())
        {
            subArgs = args.Length != 2 ? "" : args[1];
            if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
            {
                Logger.Msg($"{subArgs}", "modcolor");
                Utils.SendMessage(GetString("ColorInvalidHexCode"), player.PlayerId);
                return;
            }
            string colorFilePath = $"{modTagsFiles}/{player.FriendCode}.txt";
            if (!File.Exists(colorFilePath))
            {
                Logger.Warn($"File Not exist, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                File.Create(colorFilePath).Close();
            }

            File.WriteAllText(colorFilePath, $"{subArgs}");
        }
        else
        {
            subArgs = args.Length < 3 ? "" : args[1] + " " + args[2];
            Regex regex = new(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
            if (string.IsNullOrEmpty(subArgs) || !regex.IsMatch(subArgs))
            {
                Logger.Msg($"{subArgs}", "modcolor");
                Utils.SendMessage(GetString("ColorInvalidGradientCode"), player.PlayerId);
                return;
            }
            string colorFilePath = $"{modTagsFiles}/{player.FriendCode}.txt";
            if (!File.Exists(colorFilePath))
            {
                Logger.Msg($"File Not exist, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                File.Create(colorFilePath).Close();
            }
            File.WriteAllText(colorFilePath, $"{subArgs}");
        }
    }

    private static void VIPColorCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = "";
        if (Options.ApplyVipList.GetValue() == 0)
        {
            Utils.SendMessage(GetString("VipColorCommandDisabled"), player.PlayerId);
            return;
        }
        if (!Utils.IsPlayerVIP(player.FriendCode))
        {
            Utils.SendMessage(GetString("VipColorCommandNoAccess"), player.PlayerId);
            return;
        }
        if (player.IsHost())
        {
            Utils.SendMessage(GetString("Message.CanNotUseByHost"), player.PlayerId);
            return;
        }
        if (!Options.GradientTagsOpt.GetBool())
        {
            subArgs = args.Length != 2 ? "" : args[1];
            if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
            {
                Logger.Msg($"{subArgs}", "vipcolor");
                Utils.SendMessage(GetString("VipColorInvalidHexCode"), player.PlayerId);
                return;
            }
            string colorFilePathh = $"{vipTagsFiles}/{player.FriendCode}.txt";
            if (!File.Exists(colorFilePathh))
            {
                Logger.Warn($"File Not exist, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                File.Create(colorFilePathh).Close();
            }

            File.WriteAllText(colorFilePathh, $"{subArgs}");
        }
        else
        {
            subArgs = args.Length < 3 ? "" : args[1] + " " + args[2];
            Regex regexx = new(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
            if (string.IsNullOrEmpty(subArgs) || !regexx.IsMatch(subArgs))
            {
                Logger.Msg($"{subArgs}", "vipcolor");
                Utils.SendMessage(GetString("VipColorInvalidGradientCode"), player.PlayerId);
                return;
            }
            string colorFilePathh = $"{vipTagsFiles}/{player.FriendCode}.txt";
            if (!File.Exists(colorFilePathh))
            {
                Logger.Msg($"File Not exist, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                File.Create(colorFilePathh).Close();
            }
            File.WriteAllText(colorFilePathh, $"{subArgs}");
        }
    }

    private static void ExeCommand(PlayerControl player, string text, string[] args)
    {
        if (!TagManager.CanUseExecuteCommand(player.FriendCode) && !player.IsHost())
        {
            Utils.SendMessage(GetString("ExecuteCommandNoAccess"), player.PlayerId);
            return;
        }
        if (args.Length < 2 || !int.TryParse(args[1], out int id)) return;
        var target = Utils.GetPlayerById(id);
        if (target != null)
        {
            target.SetDeathReason(PlayerState.DeathReason.etc);
            target.SetRealKiller(player);
            target.RpcExileV3();
            if (player.IsHost())
            {
                if (target.IsHost()) Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                else Utils.SendMessage(string.Format(GetString("Message.Executed"), target.Data.PlayerName));
                return;
            }
            Utils.SendMessage(string.Format(GetString("Message.ExecutedNonHost"), target.Data.PlayerName, player.Data.PlayerName));
        }
    }

    private static void KillCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) return;
        var target = Utils.GetPlayerById(id2);
        if (target != null)
        {
            target.RpcMurderPlayer(target);
            if (target.IsHost()) Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
            else Utils.SendMessage(string.Format(GetString("Message.Executed"), target.Data.PlayerName));

            _ = new LateTask(() =>
            {
                Utils.NotifyRoles(ForceLoop: false, NoCache: true);

            }, 0.2f, "Update NotifyRoles players after /kill");
        }
    }

    private static void ReviveCommand(PlayerControl player, string text, string[] args)
    {
        if (!player.FriendCode.CanUseDev())
        {
            Utils.SendMessage($"{GetString("InvalidPermissionCMD")}", player.PlayerId);
            return;
        }
        if (args.Length < 2 || !int.TryParse(args[1], out int id3)) return;
        var target1 = Utils.GetPlayerById(id3);
        if (target1 != null)
        {
            target1.RpcRevive();
            Utils.SendMessage(string.Format(GetString("Message.Revive"), target1.Data.PlayerName), player.PlayerId);
        }

    }

    private static void AddModCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte AddModPlayerId))
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }

        if (AddModPlayerId == 0)
        {
            Utils.SendMessage(GetString("CommandAddHost"), player.PlayerId);
            return;
        }

        var addModPlayerId = Utils.GetPlayerById(AddModPlayerId);
        if (addModPlayerId == null)
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }
        if (Utils.IsPlayerModerator(addModPlayerId.FriendCode))
        {
            Utils.SendMessage(GetString("PlayerAlreadyMod"), player.PlayerId);
            return;
        }
        if (addModPlayerId != null)
        {
            string moderatorFriendCode10 = addModPlayerId.FriendCode.ToString();
            string Message10 = $"{moderatorFriendCode10}";
            File.AppendAllText(modFiles, Message10 + Environment.NewLine);
            Utils.SendMessage(GetString("PlayerJoinModList"), player.PlayerId);
        }
    }

    private static void DeleteModCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte DeleteModPlayerId))
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }

        if (DeleteModPlayerId == 0)
        {
            Utils.SendMessage(GetString("CommandDeleteHost"), player.PlayerId);
            return;
        }

        var deleteModPlayerId = Utils.GetPlayerById(DeleteModPlayerId);
        if (deleteModPlayerId == null)
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }
        if (!Utils.IsPlayerModerator(deleteModPlayerId.FriendCode))
        {
            Utils.SendMessage(GetString("PlayerNotMod"), player.PlayerId);
            return;
        }
        if (deleteModPlayerId != null)
        {
            string moderatorFriendCode11 = deleteModPlayerId.FriendCode.ToString();
            File.WriteAllLines(modFiles, File.ReadAllLines(modFiles).Where(x => !x.Contains(moderatorFriendCode11)));
            Utils.SendMessage(GetString("PlayerDeleteFromModList"), player.PlayerId);
        }
    }

    private static void AddVIPCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte AddVipPlayerId))
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }

        if (AddVipPlayerId == 0)
        {
            Utils.SendMessage(GetString("CommandAddHost"), player.PlayerId);
            return;
        }

        var addVipPlayerId = Utils.GetPlayerById(AddVipPlayerId);
        if (addVipPlayerId == null)
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }
        if (Utils.IsPlayerVIP(addVipPlayerId.FriendCode))
        {
            Utils.SendMessage(GetString("PlayerAlreadyVip"), player.PlayerId);
            return;
        }
        if (addVipPlayerId != null)
        {
            string vipFriendCode10 = addVipPlayerId.FriendCode.ToString();
            string Message11 = $"{vipFriendCode10}";
            File.AppendAllText(vipFiles, Message11 + Environment.NewLine);
            Utils.SendMessage(GetString("PlayerJoinVipList"), player.PlayerId);
        }
    }

    private static void DeleteVIPCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte DeleteVipPlayerId))
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }

        if (DeleteVipPlayerId == 0)
        {
            Utils.SendMessage(GetString("CommandDeleteHost"), player.PlayerId);
            return;
        }

        var deleteVipPlayerId = Utils.GetPlayerById(DeleteVipPlayerId);
        if (deleteVipPlayerId == null)
        {
            Utils.SendMessage(GetString("CommandInvalidID"), player.PlayerId);
            return;
        }
        if (!Utils.IsPlayerVIP(deleteVipPlayerId.FriendCode))
        {
            Utils.SendMessage(GetString("PlayerNotVip"), player.PlayerId);
            return;
        }
        if (deleteVipPlayerId != null)
        {
            string vipFriendCode11 = deleteVipPlayerId.FriendCode.ToString();
            File.WriteAllLines(vipFiles, File.ReadAllLines(vipFiles).Where(x => !x.Contains(vipFriendCode11)));
            Utils.SendMessage(GetString("PlayerDeleteFromVipList"), player.PlayerId);
        }
    }

    private static void ColourCommand(PlayerControl player, string text, string[] args)
    {
        if (Options.PlayerCanSetColor.GetBool() || player.FriendCode.CanUseDev() || player.FriendCode.GetDevUser().ColorCmd || Utils.IsPlayerVIP(player.FriendCode) ||
            player.IsHost())
        {
            var subArgs = args.Length < 2 ? "" : args[1];
            var color = Utils.MsgToColor(subArgs);
            if (color == byte.MaxValue)
            {
                Utils.SendMessage(GetString("IllegalColor"), player.PlayerId, sendOption: SendOption.None);
                return;
            }
            player.RpcSetColor(color);
            Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), player.PlayerId, sendOption: SendOption.None);
        }
        else
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
        }
    }

    private static void QuitCommand(PlayerControl player, string text, string[] args)
    {
        if (player.IsHost())
        {
            Utils.SendMessage(GetString("Message.CanNotUseByHost"), player.PlayerId);
            return;
        }
        if (Options.PlayerCanUseQuitCommand.GetBool())
        {
            var subArgs = args.Length < 2 ? "" : args[1];
            var cid = player.PlayerId.ToString();
            cid = cid.Length != 1 ? cid.Substring(1, 1) : cid;
            if (subArgs.Equals(cid))
            {
                string name = player.GetRealName();
                Utils.SendMessage(string.Format(GetString("Message.PlayerQuitForever"), name));
                AmongUsClient.Instance.KickPlayer(player.GetClientId(), true);
            }
            else
            {
                Utils.SendMessage(string.Format(GetString("SureUse.quit"), cid), player.PlayerId);
            }
        }
        else
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
        }
    }

    private static void XfCommand(PlayerControl player, string text, string[] args)
    {
        foreach (var pc in Main.EnumeratePlayerControls())
        {
            if (pc.IsAlive()) continue;

            if (player.IsHost()) pc.SetName(pc.GetRealName(isMeeting: true));
            else pc.RpcSetNamePrivate(pc.GetRealName(isMeeting: true), player, true);
        }
        ChatUpdatePatch.DoBlockChat = false;
        Utils.SendMessage(GetString("Message.TryFixName"), player.PlayerId);
    }

    private static void IdCommand(PlayerControl player, string text, string[] args)
    {
        if (TagManager.ReadPermission(player.FriendCode) < 2 && (Options.ApplyModeratorList.GetValue() == 0 || !Utils.IsPlayerModerator(player.FriendCode))
            && !Options.EnableVoteCommand.GetBool() && !player.IsHost()) return;

        string msgText = GetString("PlayerIdList");
        foreach (var pc in Main.EnumeratePlayerControls())
        {
            if (pc == null) continue;
            msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
        }
        Utils.SendMessage(msgText, player.PlayerId);
    }

    private static void ChangeRoleCommand(PlayerControl player, string text, string[] args)
    {
        if (GameStates.IsHideNSeek) return;
        var subArgs = text.Remove(0, 11);
        var setRole = FixRoleNameInput(subArgs).ToLower().Trim().Replace(" ", string.Empty);
        Logger.Info(setRole, "changerole Input");
        foreach (var rl in CustomRolesHelper.AllRoles)
        {
            if (rl.IsVanilla()) continue;
            var roleName = GetString(rl.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
            if (setRole == roleName)
            {
                player.RpcSetCustomRoleV2(rl, true, true);
                Utils.SendMessage(string.Format("Debug Set your role to {0}", rl.GetActualRoleName()), player.PlayerId);
                Utils.NotifyRoles(SpecifyTarget: player, NoCache: true);
                Utils.MarkEveryoneDirtySettings();
                break;
            }
        }
    }

    private static void EndCommand(PlayerControl player, string text, string[] args)
    {
        if (!TagManager.CanUseEndCommand(player.FriendCode) && !player.IsHost())
        {
            Utils.SendMessage(GetString("EndCommandNoAccess"), player.PlayerId);
            return;
        }
        if (!player.IsHost()) Utils.SendMessage(string.Format(GetString("EndCommandEnded"), player.name));
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
        GameManager.Instance.LogicFlow.CheckEndCriteria();
    }

    private static void CosIdCommand(PlayerControl player, string text, string[] args)
    {
        var of = player.Data.DefaultOutfit;
        Logger.Warn($"ColorId: {of.ColorId}", "Get Cos Id");
        Logger.Warn($"PetId: {of.PetId}", "Get Cos Id");
        Logger.Warn($"HatId: {of.HatId}", "Get Cos Id");
        Logger.Warn($"SkinId: {of.SkinId}", "Get Cos Id");
        Logger.Warn($"VisorId: {of.VisorId}", "Get Cos Id");
        Logger.Warn($"NamePlateId: {of.NamePlateId}", "Get Cos Id");
    }

    private static void MeetingCommand(PlayerControl player, string text, string[] args)
    {
        if (GameStates.IsMeeting)
        {
            if (MeetingHud.Instance)
            {
                MeetingHud.Instance.RpcClose();
            }
        }
        else
        {
            player.NoCheckStartMeeting(null, force: true);
        }
    }

    private static void CSCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = text.Remove(0, 3);
        player.RPCPlayCustomSound(subArgs.Trim());
    }

    private static void SDCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = text.Remove(0, 3);
        if (args.Length < 1 || !int.TryParse(args[1], out int sound1)) return;
        RPC.PlaySoundRPC((Sounds)sound1, player.PlayerId);
    }

    private static void PollCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length == 2 && args[1] == GetString("Replay") && Pollvotes.Any() && PollMSG != string.Empty)
        {
            Utils.SendMessage(PollMSG);
            return;
        }

        PollMSG = string.Empty;
        Pollvotes.Clear();
        PollQuestions.Clear();
        PollVoted.Clear();
        Polltimer = 60f;

        MapPoll = args[1] == $"{GetString("MapPollTitle")}?";

        static System.Collections.IEnumerator StartPollCountdown()
        {
            if (!Pollvotes.Any() || !GameStates.IsLobby)
            {
                Pollvotes.Clear();
                PollQuestions.Clear();
                PollVoted.Clear();

                yield break;
            }
            bool playervoted = (Main.AllPlayerControls.Count - 1) > Pollvotes.Values.Sum();


            while (playervoted && Polltimer > 0f)
            {
                if (!Pollvotes.Any() || !GameStates.IsLobby)
                {
                    Pollvotes.Clear();
                    PollQuestions.Clear();
                    PollVoted.Clear();

                    yield break;
                }
                playervoted = (Main.AllPlayerControls.Count - 1) > Pollvotes.Values.Sum();
                Polltimer -= Time.deltaTime;
                yield return null;
            }

            if (!Pollvotes.Any() || !GameStates.IsLobby)
            {
                Pollvotes.Clear();
                PollQuestions.Clear();
                PollVoted.Clear();

                yield break;
            }

            Logger.Info($"FINNISHED!! playervote?: {!playervoted} polltime?: {Polltimer <= 0}", "/poll - StartPollCountdown");

            DetermineResults();
        }

        static void DetermineResults()
        {
            int basenum = Pollvotes.Values.Max();
            var winners = Pollvotes.Where(x => x.Value == basenum);

            string msg = "";

            Color32 clr = new(47, 234, 45, 255);
            var tytul = Utils.ColorString(clr, GetString("PollResultTitle"));

            if (winners.Count() == 1)
            {
                var losers = Pollvotes.Where(x => x.Key != winners.First().Key);
                msg = string.Format(GetString("Poll.Result"), $"{winners.First().Key}{PollQuestions[winners.First().Key]}", winners.First().Value);

                for (int i = 0; i < losers.Count(); i++)
                {
                    msg += $"\n{losers.ElementAt(i).Key} / {losers.ElementAt(i).Value} {PollQuestions[losers.ElementAt(i).Key]}";

                }
                msg += "</size>";

                var winnerId = winners.First().Key - 65;
                if (MapPoll)
                {
                    if (GameStates.IsNormalGame) Main.NormalOptions.MapId = (byte)winnerId;
                    else if (GameStates.IsHideNSeek) Main.HideNSeekOptions.MapId = (byte)winnerId;
                }

                Utils.SendMessage(msg, title: tytul);
            }
            else
            {
                var tienum = Pollvotes.Values.Max();
                var tied = Pollvotes.Where(x => x.Value == tienum);

                for (int i = 0; i < (tied.Count() - 1); i++)
                {
                    msg += "\n" + tied.ElementAt(i).Key + PollQuestions[tied.ElementAt(i).Key] + " & ";
                }
                msg += "\n" + tied.Last().Key + PollQuestions[tied.Last().Key];

                Utils.SendMessage(string.Format(GetString("Poll.Tied"), msg, tienum), title: tytul);
            }

            Pollvotes.Clear();
            PollQuestions.Clear();
            PollVoted.Clear();
        }


        if (Main.AllPlayerControls.Count < 3)
        {
            Utils.SendMessage(GetString("Poll.MissingPlayers"), player.PlayerId);
            return;
        }

        if (!GameStates.IsLobby)
        {
            Utils.SendMessage(GetString("Poll.OnlyInLobby"), player.PlayerId);
            return;
        }

        if (args.SkipWhile(x => !x.Contains('?')).ToArray().Length < 3 || !args.Any(x => x.Contains('?')))
        {
            Utils.SendMessage(GetString("PollUsage"), player.PlayerId);
            return;
        }
        var resultat = args.TakeWhile(x => !x.Contains('?')).Concat(args.SkipWhile(x => !x.Contains('?')).Take(1));

        string tytul = string.Join(" ", resultat.Skip(1));
        bool Longtitle = tytul.Length > 30;
        tytul = Utils.ColorString(Palette.PlayerColors[player.Data.DefaultOutfit.ColorId], tytul);
        var altTitle = Utils.ColorString(new Color32(151, 198, 230, 255), GetString("PollTitle"));

        var ClearTIT = args.ToList();
        ClearTIT.RemoveRange(0, resultat.ToArray().Length);

        var Questions = ClearTIT.ToArray();
        string msg = "";


        if (Longtitle) msg += "<voffset=-0.5em>" + tytul + "</voffset>\n\n";
        for (int i = 0; i < Math.Clamp(Questions.Length, 2, 20); i++)
        {
            msg += Utils.ColorString(RndCLR(), $"{char.ToUpper((char)(i + 65))}) {Questions[i]}\n");
            Pollvotes[char.ToUpper((char)(i + 65))] = 0;
            PollQuestions[char.ToUpper((char)(i + 65))] = $"<size=45%>〖 {Questions[i]} 〗</size>";
        }
        msg += $"\n{GetString("Poll.Begin")}";
        msg += $"\n<size=55%><i>{GetString("Poll.TimeInfo")}</i></size>";
        PollMSG = !Longtitle ? "<voffset=-0.5em>" + tytul + "</voffset>\n\n" + msg : msg;

        Logger.Info($"Poll message: {msg}", "MEssapoll");

        Utils.SendMessage(msg, title: !Longtitle ? tytul : altTitle);

        Main.Instance.StartCoroutine(StartPollCountdown());


        static Color32 RndCLR()
        {
            byte r, g, b;

            r = (byte)IRandom.Instance.Next(45, 185);
            g = (byte)IRandom.Instance.Next(45, 185);
            b = (byte)IRandom.Instance.Next(45, 185);

            return new Color32(r, g, b, 255);
        }
    }

    private static void PVCommand(PlayerControl player, string text, string[] args)
    {
        if (player.IsHost())
        {
            Utils.SendMessage(GetString("Message.CanNotUseByHost"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        if (!Pollvotes.Any())
        {
            Utils.SendMessage(GetString("Poll.Inactive"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        if (PollVoted.Contains(player.PlayerId))
        {
            Utils.SendMessage(GetString("Poll.AlreadyVoted"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        var subArgs = args.Length != 2 ? "" : args[1];
        char vote = ' ';

        if (int.TryParse(subArgs, out int integer) && (Pollvotes.Count - 1) >= integer)
        {
            vote = char.ToUpper((char)(integer + 65));
        }
        else if (!(char.TryParse(subArgs, out vote) && Pollvotes.ContainsKey(char.ToUpper(vote))))
        {
            Utils.SendMessage(GetString("Poll.VotingInfo"), player.PlayerId);
            return;
        }
        vote = char.ToUpper(vote);

        PollVoted.Add(player.PlayerId);
        Pollvotes[vote]++;
        Utils.SendMessage(string.Format(GetString("Poll.YouVoted"), vote, Pollvotes[vote]), player.PlayerId);
        Logger.Info($"The new value of {vote} is {Pollvotes[vote]}", "TestPV_CHAR");
    }

    private static void RPSCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.CanPlayMiniGames.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        var subArgs = args.Length != 2 ? "" : args[1];

        if (!GameStates.IsLobby && player.IsAlive())
        {
            Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
            return;
        }

        if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice))
        {
            Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
            return;
        }
        else if (playerChoice < 0 || playerChoice > 2)
        {
            Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
            return;
        }
        else
        {
            var rand = IRandom.Instance;
            int botChoice = rand.Next(0, 3);
            var rpsList = new List<string> { GetString("Rock"), GetString("Paper"), GetString("Scissors") };
            if (botChoice == playerChoice)
            {
                Utils.SendMessage(string.Format(GetString("RpsDraw"), rpsList[botChoice]), player.PlayerId);
            }
            else if ((botChoice == 0 && playerChoice == 2) ||
                     (botChoice == 1 && playerChoice == 0) ||
                     (botChoice == 2 && playerChoice == 1))
            {
                Utils.SendMessage(string.Format(GetString("RpsLose"), rpsList[botChoice]), player.PlayerId);
            }
            else
            {
                Utils.SendMessage(string.Format(GetString("RpsWin"), rpsList[botChoice]), player.PlayerId);
            }
        }
    }

    private static void CoinFlipCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.CanPlayMiniGames.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        if (!GameStates.IsLobby && player.IsAlive())
        {
            Utils.SendMessage(GetString("CoinflipCommandInfo"), player.PlayerId);
            return;
        }
        else
        {
            var rand = IRandom.Instance;
            int botChoice = rand.Next(1, 101);
            var coinSide = (botChoice < 51) ? GetString("Heads") : GetString("Tails");
            Utils.SendMessage(string.Format(GetString("CoinFlipResult"), coinSide), player.PlayerId);
        }
    }

    private static void GNOCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.CanPlayMiniGames.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        if (!GameStates.IsLobby && player.IsAlive())
        {
            Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        var subArgs = args.Length != 2 ? "" : args[1];
        if (subArgs == "" || !int.TryParse(subArgs, out int guessedNo))
        {
            Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        else if (guessedNo < 0 || guessedNo > 99)
        {
            Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        else
        {
            int targetNumber = Main.GuessNumber[player.PlayerId][0];
            if (Main.GuessNumber[player.PlayerId][0] == -1)
            {
                var rand = IRandom.Instance;
                Main.GuessNumber[player.PlayerId][0] = rand.Next(0, 100);
                targetNumber = Main.GuessNumber[player.PlayerId][0];
            }
            Main.GuessNumber[player.PlayerId][1]--;
            if (Main.GuessNumber[player.PlayerId][1] == 0 && guessedNo != targetNumber)
            {
                Main.GuessNumber[player.PlayerId][0] = -1;
                Main.GuessNumber[player.PlayerId][1] = 7;
                Utils.SendMessage(string.Format(GetString("GNoLost"), targetNumber), player.PlayerId);
                return;
            }
            else if (guessedNo < targetNumber)
            {
                Utils.SendMessage(string.Format(GetString("GNoLow"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                return;
            }
            else if (guessedNo > targetNumber)
            {
                Utils.SendMessage(string.Format(GetString("GNoHigh"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                return;
            }
            else
            {
                Utils.SendMessage(string.Format(GetString("GNoWon"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                Main.GuessNumber[player.PlayerId][0] = -1;
                Main.GuessNumber[player.PlayerId][1] = 7;
                return;
            }
        }
    }

    private static void RandCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.CanPlayMiniGames.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        var subArgs = args.Length != 3 ? "" : args[1];
        var subArgs2 = args.Length != 3 ? "" : args[2];

        if (!GameStates.IsLobby && player.IsAlive())
        {
            Utils.SendMessage(GetString("RandCommandInfo"), player.PlayerId);
            return;
        }
        if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice1) || subArgs2 == "" || !int.TryParse(subArgs2, out int playerChoice2))
        {
            Utils.SendMessage(GetString("RandCommandInfo"), player.PlayerId);
            return;
        }
        else
        {
            var rand = IRandom.Instance;
            int botResult = rand.Next(playerChoice1, playerChoice2 + 1);
            Utils.SendMessage(string.Format(GetString("RandResult"), botResult), player.PlayerId);
            return;
        }
    }

    private static void EightBallCommand(PlayerControl player, string text, string[] args)
    {
        if (!Options.CanPlayMiniGames.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId, sendOption: SendOption.None);
            return;
        }
        var rando = IRandom.Instance;
        int result = rando.Next(0, 16);
        string str = "";
        switch (result)
        {
            case 0:
                str = GetString("Yes");
                break;
            case 1:
                str = GetString("No");
                break;
            case 2:
                str = GetString("8BallMaybe");
                break;
            case 3:
                str = GetString("8BallTryAgainLater");
                break;
            case 4:
                str = GetString("8BallCertain");
                break;
            case 5:
                str = GetString("8BallNotLikely");
                break;
            case 6:
                str = GetString("8BallLikely");
                break;
            case 7:
                str = GetString("8BallDontCount");
                break;
            case 8:
                str = GetString("8BallStop");
                break;
            case 9:
                str = GetString("8BallPossibly");
                break;
            case 10:
                str = GetString("8BallProbably");
                break;
            case 11:
                str = GetString("8BallProbablyNot");
                break;
            case 12:
                str = GetString("8BallBetterNotTell");
                break;
            case 13:
                str = GetString("8BallCantPredict");
                break;
            case 14:
                str = GetString("8BallWithoutDoubt");
                break;
            case 15:
                str = GetString("8BallWithDoubt");
                break;
        }
        Utils.SendMessage("<align=\"center\"><size=150%>" + str + "</align></size>", player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medium), GetString("8BallTitle")));
    }

    private static void StartCommand(PlayerControl player, string text, string[] args)
    {
        var tagCanStart = TagManager.ReadPermission(player.FriendCode) >= 3 || player.IsHost();
        if (!tagCanStart && !Utils.IsPlayerModerator(player.FriendCode))
        {
            Utils.SendMessage(GetString("StartCommandNoAccess"), player.PlayerId);
            return;
        }
        if (!tagCanStart && (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowStartCommand.GetBool() == false))
        {
            Utils.SendMessage(GetString("StartCommandDisabled"), player.PlayerId);
            return;
        }
        if (GameStates.IsCountDown)
        {
            Utils.SendMessage(GetString("StartCommandCountdown"), player.PlayerId);
            return;
        }
        var subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !int.TryParse(subArgs, out int countdown))
        {
            countdown = 5;
        }
        else
        {
            countdown = int.Parse(subArgs);
        }
        if ((countdown < Options.StartCommandMinCountdown.CurrentValue || countdown > Options.StartCommandMaxCountdown.CurrentValue) && !player.IsHost())
        {
            Utils.SendMessage(string.Format(GetString("StartCommandInvalidCountdown"), Options.StartCommandMinCountdown.CurrentValue, Options.StartCommandMaxCountdown.CurrentValue), player.PlayerId);
            return;
        }
        GameStartManager.Instance.BeginGame();
        GameStartManager.Instance.countDownTimer = countdown;
        Utils.SendMessage(string.Format(GetString("StartCommandStarted"), player.name));
    }

    private static void DraftStartCommand(PlayerControl player, string text, string[] args)
    {
        DraftAssign.RemoveReSendDraftPoolMsg();
        if (!player.IsHost() && !player.FriendCode.CanUseDev() && !Utils.IsPlayerModerator(player.FriendCode))
        {
            Utils.SendMessage(GetString("StartDraftNoAccess"), player.PlayerId);
            return;
        }
        if (GameModeBase.GetGameMode() != CustomGameMode.Standard)
        {
            Utils.SendMessage(GetString("StartDraftWrongGameMode"), player.PlayerId);
            return;
        }
        if (!Options.DraftMode.GetBool())
        {
            Utils.SendMessage(GetString("Message.DraftModeDisabled"), player.PlayerId);
            return;
        }
        DraftAssign.StartSelect();
        _ = new LateTask(() =>
        {
            foreach (var player in Main.EnumeratePlayerControls())
            {
                DraftAssign.SendDraftPoolMsg(player);
            }
        }, 25f, "Re Send Draft Pool Msg");
    }

    private static void DraftCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int index)) return;
        DraftAssign.DraftedRoles(player, index);
    }

    private static void DraftDescriptionCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int index)) return;
        DraftAssign.DraftDescriptionRoles(player, index);
    }

    private static void SpamCommand(PlayerControl player, string text, string[] args)
    {
        ChatManager.SendQuickChatSpam();
        ChatManager.SendPreviousMessagesToAll();
    }

    private static void FixCommand(PlayerControl player, string text, string[] args)
    {
        if (!player.IsHost())
        {
            if (!Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.CanUseDev()) return;
        }

        if (args.Length < 2 || !byte.TryParse(args[1], out byte id)) return;

        var pc = id.GetPlayer();
        if (pc == null) return;

        pc.FixBlackScreen();

        if (Main.EnumeratePlayerControls().All(x => x.IsAlive()))
            Logger.SendInGame(GetString("FixBlackScreenWaitForDead"));
    }

    private static void AFKExemptCommand(PlayerControl player, string text, string[] args)
    {
        if (!player.IsHost())
        {
            if (!Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.CanUseDev()) return;
        }

        if (args.Length < 2 || !byte.TryParse(args[1], out byte afkId)) return;

        AFKDetector.ExemptedPlayers.Add(afkId);
        Utils.SendMessage("\n", player.PlayerId, string.Format(GetString("PlayerExemptedFromAFK"), afkId.GetPlayerName()));
    }

    private static void SpectateCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int index)) return;
        var pc = Utils.GetPlayerById((byte)index);
        if (!RoleAssign.SetRoles.ContainsKey((byte)index) || RoleAssign.SetRoles[(byte)index] != CustomRoles.GM)
        {
            RoleAssign.SetRoles[(byte)index] = CustomRoles.GM;
            Utils.SendMessage(GetString("PlayerJoinSpectateList"), player.PlayerId);
            if (pc.FriendCode.CanUseDev()) Utils.SendMessage(GetString("YouJoinSpectateList"), pc.PlayerId);
        }
        else
        {
            RoleAssign.SetRoles.Remove((byte)index);
            Utils.SendMessage(GetString("PlayerDeleteFromSpectateList"), player.PlayerId);
            if (pc.FriendCode.CanUseDev()) Utils.SendMessage(GetString("YouDeleteFromSpectateList"), pc.PlayerId);
        }
    }

    private static void EnableAllRolesCommand(PlayerControl player, string text, string[] args)
    {
        Options.CustomRoleSpawnChances.Values.DoIf(x => x.GetValue() == 0, x => x.SetValue(1));
        Utils.SendMessage(GetString("AllRolesEnabled"), player.PlayerId);
    }

    private static void PresetCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2) return;
        switch (args[1])
        {
            case "up":
            case "upload":
            case "上传":
                var checkOptions = TempCurrentOptions;
                TempCurrentOptions = OptionItem.AllOptions.ToDictionary(x => x.Id, x => x.GetValue());
                if (checkOptions == TempCurrentOptions)
                {
                    Utils.SendMessage(GetString("UploadSamePreset"), player.PlayerId);
                    break;
                }
                if (Utils.GetTimeStamp() <= LastUpload + 5)
                {
                    Utils.SendMessage(GetString("WaitUpload"), player.PlayerId);
                    break;
                }
                LastUpload = Utils.GetTimeStamp();
                Main.Instance.StartCoroutine(UploadCurrentPreset(player));
                Logger.Info("Upload Preset", "PresetCommand");
                break;
            case "load":
            case "加载":
                if (Utils.GetTimeStamp() <= LastUpload + 5)
                {
                    Utils.SendMessage(GetString("WaitUpload"), player.PlayerId);
                    break;
                }
                LastUpload = Utils.GetTimeStamp();
                Main.Instance.StartCoroutine(DownloadPreset(player, args[2]));
                break;
        }
    }

    private static void SetRoleCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2) return;

        var subArgs = string.Join(' ', args[1..]);

        if (!GuessManager.MsgToPlayerAndRole(subArgs, out byte resultId, out CustomRoles roleToSet, out _))
        {
            Utils.SendMessage(GetString("Message.SetRoleHelp"), player.PlayerId);
            return;
        }

        var targetPc = Utils.GetPlayerById(resultId);
        if (!targetPc) return;

        var shouldDevAssign = true;

        if (roleToSet is CustomRoles.GM or CustomRoles.Mini || roleToSet.GetCount() < 1 || roleToSet.GetMode() == 0)
        {
            shouldDevAssign = false;
        }

        if (roleToSet.IsGhostRole() || !shouldDevAssign || roleToSet.IsAddonAssignedMidGame() || (roleToSet.NotAssignInVanillaServer() && Main.CurrentServerIsVanilla))
        {
            Utils.SendMessage(string.Format(GetString("Message.SetRoleSelectFailed"), resultId.GetPlayerName(), roleToSet.ToColoredString()), player.PlayerId, sendOption: SendOption.None);
            return;
        }

        if (roleToSet.IsAdditionRole())
        {
            if (!AddonAssign.SetAddOns.ContainsKey(resultId)) AddonAssign.SetAddOns[resultId] = [];

            if (!AddonAssign.SetAddOns[resultId].Contains(roleToSet))
                AddonAssign.SetAddOns[resultId].Add(roleToSet);
        }
        else
            RoleAssign.SetRoles[resultId] = roleToSet;

        Utils.SendMessage(string.Format(GetString("Message.SetRoleSelected"), resultId.GetPlayerName(), roleToSet.ToColoredString()), player.PlayerId, sendOption: SendOption.None);

        if (targetPc.FriendCode.CanUseDev() && player.PlayerId != resultId)
        {
            Utils.SendMessage(string.Format(GetString("Message.SetRoleTestTip"), roleToSet.ToColoredString()), resultId);
        }
    }

    private static void MapPollCommand(PlayerControl player, string text, string[] args)
    {
        var map = string.Join(' ', Main.MapNamesValues);
        var msg = $"/poll {GetString("MapPollTitle")}? {map}";
        PollCommand(player, msg, msg.Split(' '));
    }

    private static IEnumerator UploadCurrentPreset(PlayerControl player)
    {
        var body = new PresetRequest
        {
            friend_code = player.Data.FriendCode,
            puid = player.GetClient().GetHashedPuid(),
            preset = TempCurrentOptions
        };
        var json = JsonSerializer.Serialize(body);
        var bodyRaw = Encoding.UTF8.GetBytes(json);
        var request = new UnityWebRequest("https://tone2.top/preset/upload", "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Logger.SendInGame(string.Format(GetString("PresetUploadError"), request.error));
            yield break;
        }
        PresetResponse response;
        try
        {
            response = JsonSerializer.Deserialize<PresetResponse>(request.downloadHandler.text);
        }
        catch
        {
            Logger.SendInGame(GetString("PresetUploadFailed"));
            yield break;
        }
        if (!string.IsNullOrEmpty(response.error))
        {
            Logger.SendInGame(string.Format(GetString("PresetUploadRejected"), request.error));
            yield break;
        }
        if (string.IsNullOrEmpty(response.preset_id))
        {
            Logger.SendInGame(GetString("PresetUploadNoId"));
            yield break;
        }
        Utils.SendMessage(string.Format(GetString("PresetUploadSuccess"), response.preset_id), player.PlayerId);
    }

    private static IEnumerator DownloadPreset(PlayerControl player, string preset_id)
    {
        var request = UnityWebRequest.Get($"https://tone2.top/preset/{preset_id}");
        request.timeout = 5;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Logger.SendInGame(string.Format(GetString("PresetDownloadError"), request.error));
            yield break;
        }
        PresetDownloadResponse response;
        try
        {
            response = JsonSerializer.Deserialize<PresetDownloadResponse>(request.downloadHandler.text);
        }
        catch
        {
            Logger.SendInGame(GetString("PresetParseFailed"));
            yield break;
        }
        if (!string.IsNullOrEmpty(response.error))
        {
            Logger.SendInGame(string.Format(GetString("PresetDownloadRejected"), response.error));
            yield break;
        }
        if (response.preset == null || string.IsNullOrEmpty(response.friend_code))
        {
            Logger.SendInGame(GetString("PresetInvalidResponse"));
            yield break;
        }
        TempCurrentOptions = response.preset;
        Main.Instance.StartCoroutine(LoadNewPreset(player));
    }

    public static IEnumerator LoadNewPreset(PlayerControl player)
    {
        int count = 0;
        foreach (var optionItem in OptionItem.AllOptions.ToArray())
        {
            if (TempCurrentOptions.TryGetValue(optionItem.Id, out var value))
            {
                if (optionItem.GetValue() == value) continue;

                optionItem.SetValue(value);
                count++;
                if (count >= 100)
                {
                    count = 0;
                    yield return null;
                }
            }
        }
        RPC.SyncCustomSettingsRPC();
        Utils.SendMessage(GetString("PresetDownloadSuccess"), player.PlayerId);
    }

    private static bool ImpostorChannel(PlayerControl pc, string msg, bool check = true)
    {
        //if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || !pc) return false;
        if (!pc.IsPlayerImpostorTeam() || !pc.GetCustomRole().IsImpostor()) return false;
        if (!Options.EnableImpostorChannel.GetBool()) return false;
        if (!pc.IsAlive()) return false;
        msg = msg.ToLower().Trim();
        if (check)
        {
            if (!GuessManager.CheckCommond(ref msg, "imp|伪装者", false)) return false;
        }

        if (string.IsNullOrEmpty(msg)) return false;

        if (AmongUsClient.Instance.AmHost || !pc.IsModded())
        {
            SendImpostorChannelMsg(pc, msg);
        }
        else
        {
            var message = new RpcSendChannelMsg(PlayerControl.LocalPlayer.NetId, msg, (int)SendTargetPatch.SendTargets.Imp);
            RpcUtils.LateBroadcastReliableMessage(message);
        }

        return true;
    }

    public static void SendImpostorChannelMsg(PlayerControl pc, string msg)
    {
        if (CustomRoles.Narc.RoleExist(true))
        {
            Utils.SendMessage(GetString("NarcInterference"), pc.PlayerId, noReplay: true);
            return;
        }

        Main.EnumerateAlivePlayerControls().Where(x => x.IsPlayerImpostorTeam() && x.GetCustomRole().IsImpostor())
            .Do(x => Utils.SendMessage(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ImpostorTONE), msg), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.ImpostorTONE), $"{GetString("MessageFromImpostor")} ~ <size=1.25>{pc.GetRealName(clientData: true)}</size>"), sendTo: x.PlayerId, noReplay: true));
    }

    private static bool CovenChannel(PlayerControl pc, string msg, bool check = true)
    {
        //if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || !pc) return false;
        if (!pc.IsPlayerCovenTeam() || !pc.GetCustomRole().IsCoven()) return false;
        if (!Options.EnableCovenChannel.GetBool()) return false;
        if (!pc.IsAlive()) return false;
        msg = msg.ToLower().Trim();
        if (check)
        {
            if (!GuessManager.CheckCommond(ref msg, "co|巫师", false)) return false;
        }

        if (string.IsNullOrEmpty(msg)) return false;

        if (AmongUsClient.Instance.AmHost || !pc.IsModded())
        {
            SendCovenChannelMsg(pc, msg);
        }
        else
        {
            var message = new RpcSendChannelMsg(PlayerControl.LocalPlayer.NetId, msg, (int)SendTargetPatch.SendTargets.Coven);
            RpcUtils.LateBroadcastReliableMessage(message);
        }

        return true;
    }

    public static void SendCovenChannelMsg(PlayerControl pc, string msg)
    {
        Main.EnumerateAlivePlayerControls().Where(x => x.IsPlayerCovenTeam() && x.GetCustomRole().IsCoven())
            .Do(x => Utils.SendMessage(Utils.ColorString(Utils.GetRoleColor(CustomRoles.WitchDoctor), msg), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.WitchDoctor), $"{GetString("MessageFromCoven")} ~ <size=1.25>{pc.GetRealName(clientData: true)}</size>"), sendTo: x.PlayerId, noReplay: true));
    }

    public class PresetRequest
    {
        public string friend_code { get; set; }
        public string puid { get; set; }
        public Dictionary<int, int> preset { get; set; }
    }

    public class PresetResponse
    {
        public string preset_id { get; set; }
        public string error { get; set; }
    }

    public class PresetDownloadResponse
    {
        public Dictionary<int, int> preset { get; set; }
        public string friend_code { get; set; }
        public string error { get; set; }
    }
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatUpdatePatch
{
    public static bool DoBlockChat = false;
    public static ChatController Instance;
    private static string[] CachedLetterOnlyHexColors = [];
    private static readonly Regex ColorTagRegex = new(@"<\s*(?:color\s*=\s*)?#([0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?)\s*>", RegexOptions.Compiled);
    private static readonly Dictionary<(int R, int G, int B), string> CachedColorReplacements = [];
    private static readonly char[] HexLetters = ['a', 'b', 'c', 'd', 'e', 'f'];
    static readonly Dictionary<string, (int r, int g, int b)> NamedColors = new()
    {
        { "red",    (255,   0,   0) },
        { "orange", (255, 165,   0) },
        { "yellow", (255, 255,   0) },
        { "green",  (  0, 255,   0) },
        { "blue",   (  0,   0, 255) },
        { "purple", (128,   0, 128) },
        { "white",  (255, 255, 255) },
        { "grey",   (128, 128, 128) },
        { "black",  (  0,   0,   0) }
    };
    public static void Postfix(ChatController __instance)
    {
        if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count == 0 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)) return;
        if (DoBlockChat) return;

        Instance ??= __instance;

        if (Main.DarkTheme.Value)
        {
            var chatBubble = __instance.chatBubblePool.Prefab.CastFast<ChatBubble>();
            chatBubble.TextArea.overrideColorTags = false;
            chatBubble.TextArea.color = Color.white;
            chatBubble.Background.color = Color.black;
        }

        var player = PlayerControl.LocalPlayer;
        if (GameStates.IsInGame || player.Data.IsDead)
        {
            player = Main.EnumerateAlivePlayerControls().ToArray().OrderBy(x => x.PlayerId).FirstOrDefault()
                     ?? Main.EnumeratePlayerControls().ToArray().OrderBy(x => x.PlayerId).FirstOrDefault()
                     ?? player;
        }
        //Logger.Info($"player is null? {player == null}", "ChatUpdatePatch");
        if (player == null) return;

        (string msg, byte sendTo, string title, SendOption sendOption) = Main.MessagesToSend[0];
        //Logger.Info($"MessagesToSend - sendTo: {sendTo} - title: {title}", "ChatUpdatePatch");

        if (sendTo != byte.MaxValue && GameStates.IsLobby)
        {
            var networkedPlayerInfo = Utils.GetPlayerInfoById(sendTo);
            if (networkedPlayerInfo != null)
            {
                if (networkedPlayerInfo.DefaultOutfit.ColorId == -1)
                {
                    var delaymessage = Main.MessagesToSend[0];
                    Main.MessagesToSend.RemoveAt(0);
                    Main.MessagesToSend.Add(delaymessage);
                    return;
                }
                // green beans color id is -1
            }
            // It is impossible to get null player here unless it quits
        }
        Main.MessagesToSend.RemoveAt(0);

        int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
        var name = player.Data.PlayerName;

        //__instance.freeChatField.textArea.characterLimit = 999;

        if (clientId == -1)
        {
            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg, false);
            player.SetName(name);
        }

        if (clientId == AmongUsClient.Instance.ClientId || sendTo == PlayerControl.LocalPlayer.PlayerId)
        {
            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg, false);
            player.SetName(name);
            return;
        }

        if (Main.CurrentServerIsVanilla)
        {
            msg = ReplaceHexColorsWithSafeColors(msg);
            msg = ReplaceDigitsOutsideRichText(msg);
        }

        var writer = CustomRpcSender.Create("MessagesToSend", sendOption);
        writer.StartMessage(clientId);
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.NetId)
            .Write(title)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(msg)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.NetId)
            .Write(player.Data.PlayerName)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();

        __instance.timeSinceLastMessage = 0f;

        static string ReplaceHexColorsWithSafeColors(string text) => ColorTagRegex.Replace(text, match =>
        {
            string hex = match.Groups[1].Value.ToLowerInvariant();

            string a = hex.Length == 8 ? hex[6..8] : string.Empty;
            if (!string.IsNullOrEmpty(a)) hex = hex[..6];

            if (hex.Length != 6 || !hex.Any(char.IsDigit)) return match.Value;

            int r = Convert.ToInt32(hex[..2], 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            var best = FindClosestSafeColor(r, g, b);

            return NamedColors.ContainsKey(best)
                ? $"<color={best}>"
                : $"<#{best}{a}>";
        });

        static string FindClosestSafeColor(int r, int g, int b)
        {
            if (CachedColorReplacements.TryGetValue((r, g, b), out string cache)) return cache;

            double bestDist = double.MaxValue;
            string bestValue = "white";

            foreach (var kvp in NamedColors)
            {
                (int cr, int cg, int cb) = kvp.Value;
                double d = ColorDistance(r, g, b, cr, cg, cb);

                if (d < bestDist)
                {
                    bestDist = d;
                    bestValue = kvp.Key;
                }
            }

            foreach (var hex in GenerateLetterOnlyHexColors())
            {
                int cr = Convert.ToInt32(hex[..2], 16);
                int cg = Convert.ToInt32(hex.Substring(2, 2), 16);
                int cb = Convert.ToInt32(hex.Substring(4, 2), 16);

                double d = ColorDistance(r, g, b, cr, cg, cb);

                if (d < bestDist)
                {
                    bestDist = d;
                    bestValue = hex;
                }
            }

            CachedColorReplacements[(r, g, b)] = bestValue;
            if (CachedColorReplacements.Count > 4096) CachedColorReplacements.Clear();
            return bestValue;
        }

        static double ColorDistance(int r1, int g1, int b1, int r2, int g2, int b2)
        {
            int dr = r1 - r2;
            int dg = g1 - g2;
            int db = b1 - b2;
            return dr * dr + dg * dg + db * db;
        }

        static string[] GenerateLetterOnlyHexColors()
        {
            if (CachedLetterOnlyHexColors.Length > 0)
                return CachedLetterOnlyHexColors;

            CachedLetterOnlyHexColors = new string[46656];
            int i = 0;

            foreach (char r1 in HexLetters)
                foreach (char r2 in HexLetters)
                    foreach (char g1 in HexLetters)
                        foreach (char g2 in HexLetters)
                            foreach (char b1 in HexLetters)
                                foreach (char b2 in HexLetters)
                                    CachedLetterOnlyHexColors[i++] = $"{r1}{r2}{g1}{g2}{b1}{b2}";

            return CachedLetterOnlyHexColors;
        }

        static string ReplaceDigitsOutsideRichText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || !IsTooManyDigits(text)) return text;

            StringBuilder sb = new(text.Length);
            bool insideTag = false;

            foreach (char c in text)
            {
                switch (c)
                {
                    case '<':
                        insideTag = true;
                        sb.Append(c);
                        continue;
                    case '>':
                        insideTag = false;
                        sb.Append(c);
                        continue;
                    case >= '0' and <= '9' when !insideTag:
                        sb.Append((char)('０' + (c - '0')));
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        static bool IsTooManyDigits(string text)
        {
            int count = 0;

            foreach (char c in text)
            {
                if (c is >= '0' and <= '9')
                {
                    count++;
                    if (count > 5) return true;
                }
            }

            return false;
        }
    }
}

[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
internal class UpdateCharCountPatch
{
    public static void Postfix(FreeChatInputField __instance)
    {
        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText(length <= 0 ? GetString("ThankYouForUsingTONE") : $"{length}/{__instance.textArea.characterLimit}");
        __instance.charCountText.enableWordWrapping = false;
        if (length < (AmongUsClient.Instance.AmHost ? 888 : 444))
            __instance.charCountText.color = Color.black;
        else if (length < (AmongUsClient.Instance.AmHost ? 1111 : 777))
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        else
            __instance.charCountText.color = Color.red;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
class RpcSendChatPatch
{
    public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
    {
        if (string.IsNullOrWhiteSpace(chatText))
        {
            __result = false;
            return false;
        }
        if (!GameStates.IsModHost)
        {
            __result = false;
            return true;
        }
        int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
        chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
        if (chatText.Contains("who", StringComparison.OrdinalIgnoreCase))
            DestroyableSingleton<UnityTelemetry>.Instance.SendWho();
        /*
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
        messageWriter.Write(chatText);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        */

        var message = new RpcSendChatMessage(__instance.NetId, chatText);
        RpcUtils.LateBroadcastReliableMessage(message);
        __result = true;
        return false;
    }
}
