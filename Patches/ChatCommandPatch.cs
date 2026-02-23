using AmongUs.InnerNet.GameDataMessages;
using Assets.CoreScripts;
using Hazel;
using System;
using System.Collections;
using System.IO;
using System.Text;
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
using static TONE.Translator;


namespace TONE;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
#if ANDROID
    private static readonly string modLogFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "ModLogs.txt");
    private static readonly string modTagsFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "Tags", "MOD_TAGS");
    private static readonly string sponsorTagsFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "Tags", "SPONSOR_TAGS");
    private static readonly string vipTagsFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "Tags", "VIP_TAGS");
    private static readonly string modFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "Moderators.txt");
    private static readonly string vipFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "VIP-List.txt");
#else
    private static readonly string modLogFiles = @"./TONE-DATA/ModLogs.txt";
    private static readonly string modTagsFiles = @"./TONE-DATA/Tags/MOD_TAGS";
    private static readonly string sponsorTagsFiles = @"./TONE-DATA/Tags/SPONSOR_TAGS";
    private static readonly string vipTagsFiles = @"./TONE-DATA/Tags/VIP_TAGS";
    private static readonly string modFiles = @"./TONE-DATA/Moderators.txt";
    private static readonly string vipFiles = @"./TONE-DATA/VIP-List.txt";
#endif

    private static readonly Dictionary<char, int> Pollvotes = [];
    private static readonly Dictionary<char, string> PollQuestions = [];
    private static readonly List<byte> PollVoted = [];
    private static float Polltimer = 120f;
    private static string PollMSG = "";

    public const string Csize = "85%"; // CustomRole Settings Font-Size
    public const string Asize = "75%"; // All Appended Addons Font-Size

    public static List<string> ChatHistory = [];

    private static bool WaitingToSend;

    public static bool Prefix(ChatController __instance)
    {
        if (__instance.quickChatField.visible == false && __instance.freeChatField.textArea.text == "") return false;
        if (!GameStates.IsModHost && !AmongUsClient.Instance.AmHost) return true;
        __instance.timeSinceLastMessage = 3f;
        var text = __instance.freeChatField.textArea.text;
        if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        string[] args = text.Trim().Split(' ');
        string subArgs = "";
        string subArgs2 = "";
        var canceled = false;
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
        if (Lovers.LoversMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (ImpostorChannel(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Jackal.JackalChannel(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Jailer.JailerChannel(PlayerControl.LocalPlayer, text)) goto Canceled;
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
        switch (args[0])
        {
            case "/dump":
            case "/导出日志":
            case "/日志":
            case "/导出":
                Utils.DumpLog();
                break;
            case "/v":
            case "/version":
            case "/versão":
            case "/版本":
                canceled = true;
                string version_text = "";
                var player = PlayerControl.LocalPlayer;
                var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
                var name = player?.Data?.PlayerName;
                try
                {
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key).ToArray())
                    {
                        var pc = Utils.GetClientById(kvp.Key)?.Character;
                        version_text += $"{kvp.Key}/{(pc?.PlayerId != null ? pc.PlayerId.ToString() : "null")}:{pc?.GetRealName(clientData: true) ?? "null"}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "")
                    {
                        player.SetName(title);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, version_text);
                        player.SetName(name);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message, "/version");
                    version_text = "Error while getting version : " + e.Message;
                    if (version_text != "")
                    {
                        player.SetName(title);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, version_text);
                        player.SetName(name);
                    }
                }
                break;

            default:
                Main.isChatCommand = false;
                break;
        }
        if (AmongUsClient.Instance.AmHost)
        {
            Main.isChatCommand = true;
            switch (args[0])
            {
                case "/ans":
                case "/asw":
                case "/answer":
                case "/回答":
                    Quizmaster.AnswerByChat(PlayerControl.LocalPlayer, args);
                    break;

                case "/qmquiz":
                case "/提问":
                    Quizmaster.ShowQuestion(PlayerControl.LocalPlayer);
                    break;

                case "/win":
                case "/winner":
                case "/vencedor":
                case "/胜利":
                case "/获胜":
                case "/赢":
                case "/胜利者":
                case "/获胜的人":
                case "/赢家":
                    canceled = true;
                    if (Main.winnerNameList.Count == 0) Utils.SendMessage(GetString("NoInfoExists"));
                    else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList));
                    break;

                case "/l":
                case "/lastresult":
                case "/fimdejogo":
                case "/上局信息":
                case "/信息":
                case "/情况":
                    canceled = true;
                    Utils.ShowKillLog();
                    Utils.ShowLastRoles();
                    Utils.ShowLastResult();
                    break;

                case "/gr":
                case "/gameresults":
                case "/resultados":
                case "/对局结果":
                case "/上局结果":
                case "/结果":
                    canceled = true;
                    Utils.ShowLastResult();
                    break;

                case "/kh":
                case "/killlog":
                case "/击杀日志":
                case "/击杀情况":
                    canceled = true;
                    Utils.ShowKillLog();
                    break;

                case "/rs":
                case "/sum":
                case "/rolesummary":
                case "/sumario":
                case "/sumário":
                case "/summary":
                case "/результат":
                case "/上局职业":
                case "/职业信息":
                case "/对局职业":
                    canceled = true;
                    Utils.ShowLastRoles();
                    break;

                case "/ghostinfo":
                case "/幽灵职业介绍":
                case "/鬼魂职业介绍":
                case "/幽灵职业":
                case "/鬼魂职业":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.GhostRoleInfo"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/apocinfo":
                case "/apocalypseinfo":
                case "/灾厄中立职业介绍":
                case "/灾厄中立介绍":
                case "/灾厄中立":
                case "/灾厄类中立职业介绍":
                case "/灾厄类中立介绍":
                case "/灾厄类中立":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.ApocalypseInfo"), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Apocalypse), GetString("ApocalypseInfoTitle")));
                    break;

                case "/coveninfo":
                case "/covinfo":
                case "/巫师阵营职业介绍":
                case "/巫师阵营介绍":
                case "/巫师阵营":
                case "/巫师介绍":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.CovenInfo"), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Coven), GetString("CovenInfoTitle")));
                    break;

                case "/rn":
                case "/rename":
                case "/renomear":
                case "/переименовать":
                case "/重命名":
                case "/命名为":
                    canceled = true;
                    if (args.Length < 1) break;
                    if (args.Skip(1).Join(delimiter: " ").Length is > 10 or < 1 || args.Skip(1).Join(delimiter: " ")[0] == '<') // <#ffffff>E is a valid name without this
                    {
                        Utils.SendMessage(GetString("Message.AllowNameLength"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        var temp = args.Skip(1).Join(delimiter: " ");
                        Main.HostRealName = temp;
                        Main.AllPlayerNames[PlayerControl.LocalPlayer.PlayerId] = temp;
                        Utils.SendMessage(string.Format(GetString("Message.SetName"), temp), PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;

                case "/hn":
                case "/hidename":
                case "/semnome":
                case "/隐藏名字":
                case "/藏名":
                    canceled = true;
                    Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                    GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
                        ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                            ? $"<color={Main.HideColor.Value}>TONE</color>"
                            : $"<color={Main.ModColor}>TONE</color>";
                    break;

                case "/level":
                case "/nível":
                case "/nivel":
                case "/等级":
                case "/等级设置为":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    Utils.SendMessage(string.Format(GetString("Message.SetLevel"), subArgs), PlayerControl.LocalPlayer.PlayerId);
                    _ = int.TryParse(subArgs, out int input);
                    if (input is < 1 or > 999)
                    {
                        Utils.SendMessage(GetString("Message.AllowLevelRange"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    var number = Convert.ToUInt32(input);
                    PlayerControl.LocalPlayer.RpcSetLevel(number - 1);
                    break;

                case "/n":
                case "/now":
                case "/atual":
                case "/设置":
                case "/系统设置":
                case "/模组设置":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                        case "funções":
                            Utils.ShowActiveRoles();
                            break;
                        case "a":
                        case "all":
                        case "tudo":
                            Utils.ShowAllActiveSettings();
                            break;
                        default:
                            Utils.ShowActiveSettings();
                            break;
                    }
                    break;

                case "/dis":
                case "/disconnect":
                case "/desconectar":
                case "/断连":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
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
                            __instance.AddChat(PlayerControl.LocalPlayer, "crew | imp");
                            if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Brazilian)
                            {
                                __instance.AddChat(PlayerControl.LocalPlayer, "tripulante | impostor");
                            }
                            cancelVal = "/dis";
                            break;
                    }
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
                    break;

                case "/r":
                case "/role":
                case "/р":
                case "/роль":
                    canceled = true;
                    if (text.Contains("/role") || text.Contains("/роль"))
                        subArgs = text.Remove(0, 5);
                    else
                        subArgs = text.Remove(0, 2);
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/f":
                case "/factions":
                case "/faction":
                    canceled = true;
                    var impCount = $"{GetString("NumberOfImpostors")}: {GameOptionsManager.Instance.GameHostOptions.NumImpostors}";
                    if (Options.UseVariableImp.GetBool()) impCount = $"{GetString("ImpRolesMinPlayer")}: {Options.ImpRolesMinPlayer.GetInt()}\n{GetString("ImpRolesMaxPlayer")}: {Options.ImpRolesMaxPlayer.GetInt()}";
                    var nnkCount = $"{GetString("NonNeutralKillingRolesMinPlayer")}: {Options.NonNeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NonNeutralKillingRolesMaxPlayer")}: {Options.NonNeutralKillingRolesMaxPlayer.GetInt()}";
                    var nkCount = $"{GetString("NeutralKillingRolesMinPlayer")}: {Options.NeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NeutralKillingRolesMaxPlayer")}: {Options.NeutralKillingRolesMaxPlayer.GetInt()}";
                    var apocCount = $"{GetString("NeutralApocalypseRolesMinPlayer")}: {Options.NeutralApocalypseRolesMinPlayer.GetInt()}\n{GetString("NeutralApocalypseRolesMaxPlayer")}: {Options.NeutralApocalypseRolesMaxPlayer.GetInt()}";
                    var covCount = $"{GetString("CovenRolesMinPlayer")}: {Options.CovenRolesMinPlayer.GetInt()}\n{GetString("CovenRolesMaxPlayer")}: {Options.CovenRolesMaxPlayer.GetInt()}";
                    var addonCount = $"{GetString("NoLimitAddonsNumMax")}: {Options.NoLimitAddonsNumMax.GetInt()}";
                    Utils.SendMessage($"{impCount}\n{nnkCount}\n{nkCount}\n{apocCount}\n{covCount}\n{addonCount}", PlayerControl.LocalPlayer.PlayerId, $"<color={Main.ModColor}>{GetString("FactionSettingsTitle")}</color>");
                    break;
                case "/up":
                case "/指定":
                case "/成为":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp)
                    {
                        Utils.SendMessage($"{GetString("InvalidPermissionCMD")}", PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (!Options.EnableUpMode.GetBool())
                    {
                        Utils.SendMessage(string.Format(GetString("Message.YTPlanDisabled"), GetString("EnableYTPlan")), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId, isUp: true);
                    break;

                //case "/setbasic":
                //    canceled = true;
                //    if (GameStates.IsLobby)
                //    {
                //        break;
                //    }
                //    PlayerControl.LocalPlayer.RpcChangeRoleBasis(CustomRoles.PhantomTONE);
                //    break;

                case "/setplayers":
                case "/maxjogadores":
                case "/设置最大玩家数":
                case "/设置最大玩家数量":
                case "/设置玩家数":
                case "/设置玩家数量":
                case "/玩家数":
                case "/玩家数量":
                case "/玩家":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    var numbereer = Convert.ToByte(subArgs);
                    if (numbereer > 15 && GameStates.IsVanillaServer)
                    {
                        Utils.SendMessage(GetString("Message.MaxPlayersFailByRegion"));
                        break;
                    }
                    Utils.SendMessage(GetString("Message.MaxPlayers") + numbereer);
                    if (GameStates.IsNormalGame)
                        GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers = numbereer;

                    else if (GameStates.IsHideNSeek)
                        GameOptionsManager.Instance.currentHideNSeekGameOptions.MaxPlayers = numbereer;
                    break;

                case "/h":
                case "/help":
                case "/ajuda":
                case "/хелп":
                case "/хэлп":
                case "/помощь":
                case "/帮助":
                case "/教程":
                    canceled = true;
                    Utils.ShowHelp(PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/icon":
                case "/icons":
                case "/符号":
                case "/标志":
                    {
                        Utils.SendMessage(GetString("Command.icons"), PlayerControl.LocalPlayer.PlayerId, GetString("IconsTitle"));
                        break;
                    }

                case "/sicon":
                case "/sicons":
                case "/settingicons":
                case "/settingsicons":
                case "/设置符号":
                case "/设置标志":
                    {
                        Utils.SendMessage(GetString("Command.sicons"), PlayerControl.LocalPlayer.PlayerId, GetString("IconsTitle"));
                        break;
                    }

                case "/iconhelp":
                case "/符号帮助":
                case "/标志帮助":
                    {
                        Utils.SendMessage(GetString("Command.icons"), title: GetString("IconsTitle"));
                        break;
                    }

                case "/kc":
                case "/kcount":
                case "/количество":
                case "/убийцы":
                case "/存活阵营":
                case "/阵营":
                case "/存活阵营信息":
                case "/阵营信息":
                    if (GameStates.IsLobby) break;

                    if (!Options.EnableKillerLeftCommand.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var sub = new StringBuilder();

                    switch (Options.CurrentGameMode)
                    {
                        case CustomGameMode.Standard:
                            var allAlivePlayers = Main.EnumerateAlivePlayerControls();
                            int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor) && !pc.Is(CustomRoles.Narc));
                            int madnum = allAlivePlayers.Count(pc => (pc.GetCustomRole().IsMadmate() && !pc.Is(CustomRoles.Narc)) || pc.Is(CustomRoles.Madmate));
                            int neutralnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNK());
                            int apocnum = allAlivePlayers.Count(pc => pc.IsNeutralApocalypse() || pc.IsTransformedNeutralApocalypse());
                            int covnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Coven));

                            sub.Append(string.Format(GetString("Remaining.ImpostorCount"), impnum));

                            if (Options.ShowMadmatesInLeftCommand.GetBool())
                                sub.Append(string.Format("\n\r" + GetString("Remaining.MadmateCount"), madnum));

                            if (Options.ShowApocalypseInLeftCommand.GetBool())
                                sub.Append(string.Format("\n\r" + GetString("Remaining.ApocalypseCount"), apocnum));

                            if (Options.ShowCovenInLeftCommand.GetBool())
                                sub.Append(string.Format("\n\r" + GetString("Remaining.CovenCount"), covnum));

                            sub.Append(string.Format("\n\r" + GetString("Remaining.NeutralCount"), neutralnum));
                            break;

                        case CustomGameMode.FFA:
                            FFAManager.AppendFFAKcount(sub);
                            break;

                        case CustomGameMode.SpeedRun:
                            SpeedRun.AppendSpeedRunKcount(sub);
                            break;

                        case CustomGameMode.TagMode:
                            TagMode.AppendTagModeKcount(sub);
                            break;
                    }

                    Utils.SendMessage(sub.ToString(), PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/vote":
                case "/投票":
                case "/票":
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (subArgs == "" || !int.TryParse(subArgs, out int arg))
                        break;
                    var plr = Utils.GetPlayerById(arg);

                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (!Options.EnableVoteCommand.GetBool())
                    {
                        Utils.SendMessage(GetString("VoteDisabled"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (Options.ShouldVoteCmdsSpamChat.GetBool())
                    {
                        canceled = true;
                    }

                    if (arg != 253) // skip
                    {
                        if (plr == null || !plr.IsAlive())
                        {
                            Utils.SendMessage(GetString("VoteDead"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                    }
                    if (!PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("CannotVoteWhenDead"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (GameStates.IsMeeting)
                    {
                        PlayerControl.LocalPlayer.RpcCastVote((byte)arg);
                    }
                    break;

                case "/d":
                case "/death":
                case "/morto":
                case "/умер":
                case "/причина":
                case "/死亡原因":
                case "/死亡":
                    canceled = true;
                    Logger.Info($"PlayerControl.LocalPlayer.PlayerId: {PlayerControl.LocalPlayer.PlayerId}", "/death command");
                    if (GameStates.IsLobby)
                    {
                        Logger.Info("IsLobby", "/death command");
                        Utils.SendMessage(text: GetString("Message.CanNotUseInLobby"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (PlayerControl.LocalPlayer.IsAlive())
                    {
                        Logger.Info("IsAlive", "/death command");
                        Utils.SendMessage(string.Format(GetString("DeathCmd.NotDead"), PlayerControl.LocalPlayer.GetRealName(), PlayerControl.LocalPlayer.GetCustomRole().ToColoredString()), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                    {
                        Logger.Info("DeathReason.Vote", "/death command");
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Ejected"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].deathReason == PlayerState.DeathReason.Shrouded)
                    {
                        Logger.Info("DeathReason.Shrouded", "/death command");
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Shrouded"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].deathReason == PlayerState.DeathReason.FollowingSuicide)
                    {
                        Logger.Info("DeathReason.FollowingSuicide", "/death command");
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Lovers"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        Logger.Info("GetRealKiller()", "/death command");
                        var killer = PlayerControl.LocalPlayer.GetRealKiller(out var MurderRole);
                        string killerName = killer == null ? "N/A" : killer.GetRealName(clientData: true);
                        string killerRole = killer == null ? "N/A" : Utils.GetRoleName(MurderRole);
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(PlayerControl.LocalPlayer.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killerName + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{killerRole}</color>" + "</b>", sendTo: PlayerControl.LocalPlayer.PlayerId);

                        break;
                    }


                case "/m":
                case "/myrole":
                case "/minhafunção":
                case "/м":
                case "/мояроль":
                case "/身份":
                case "/我":
                case "/我的身份":
                case "/我的职业":
                    canceled = true;
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    if (GameStates.IsInGame)
                    {
                        var lp = PlayerControl.LocalPlayer;
                        var Des = lp.PetActivatedAbility() ? lp.GetRoleInfo(true) + $"<size=70%>{GetString("SupportsPetMessage")}</size>" : lp.GetRoleInfo(true);
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
                    else
                        Utils.SendMessage((PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/me":
                case "/我的权限":
                case "/权限":
                    canceled = true;
                    subArgs = text.Length == 3 ? string.Empty : text.Remove(0, 3);
                    string Devbox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                    string UpBox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                    string ColorBox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().ColorCmd ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";

                    if (string.IsNullOrEmpty(subArgs))
                    {
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser().GetUserType(), Devbox, UpBox, ColorBox)}");
                    }
                    else
                    {
                        if (byte.TryParse(subArgs, out byte meid))
                        {
                            if (meid != PlayerControl.LocalPlayer.PlayerId)
                            {
                                var targetplayer = Utils.GetPlayerById(meid);
                                if (targetplayer != null && targetplayer.GetClient() != null)
                                {
                                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandTargetInfo"), targetplayer.PlayerId, targetplayer.GetRealName(clientData: true), targetplayer.GetClient().FriendCode, targetplayer.GetClient().GetHashedPuid(), targetplayer.FriendCode.GetDevUser().GetUserType())}");
                                }
                                else
                                {
                                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{(GetString("Message.MeCommandInvalidID"))}");
                                }
                            }
                            else
                            {
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser().GetUserType(), Devbox, UpBox, ColorBox)}");
                            }
                        }
                        else
                        {
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{(GetString("Message.MeCommandInvalidID"))}");
                        }
                    }
                    break;

                case "/t":
                case "/template":
                case "/шаблон":
                case "/пример":
                case "/模板":
                case "/模板信息":
                    canceled = true;
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/mw":
                case "/messagewait":
                case "/消息等待时间":
                case "/消息冷却":
                    canceled = true;
                    if (args.Length > 1 && int.TryParse(args[1], out int sec))
                    {
                        Main.MessageWait.Value = sec;
                        Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                    }
                    else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                    break;

                case "/tpout":
                case "/传送出":
                case "/传出":
                    canceled = true;
                    if (!GameStates.IsLobby) break;
                    if (!Options.PlayerCanUseTP.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    PlayerControl.LocalPlayer.RpcTeleport(new Vector2(0.1f, 3.8f));
                    break;
                case "/tpin":
                case "/传进":
                case "/传送进":
                    canceled = true;
                    if (!GameStates.IsLobby) break;
                    if (!Options.PlayerCanUseTP.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    PlayerControl.LocalPlayer.RpcTeleport(new Vector2(-0.2f, 1.3f));
                    break;

                case "/say":
                case "/s":
                case "/с":
                case "/сказать":
                case "/说":
                    canceled = true;
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")} ~ <size=1.25>{PlayerControl.LocalPlayer.GetRealName(clientData: true)}</size></color>");
                    break;

                case "/mid":
                case "/玩家列表":
                case "/玩家信息":
                case "/玩家编号列表":
                    canceled = true;
                    string msgText1 = GetString("PlayerIdList");
                    foreach (var pc in Main.EnumeratePlayerControls())
                    {
                        if (pc == null) continue;
                        msgText1 += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                    }
                    Utils.SendMessage(msgText1, PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/ban":
                case "/banir":
                case "/бан":
                case "/забанить":
                case "/封禁":
                    canceled = true;

                    string banReason = "";
                    if (args.Length < 3)
                    {
                        Utils.SendMessage(GetString("BanCommandNoReason"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        subArgs = args[1];
                        banReason = string.Join(" ", args.Skip(2));
                    }
                    //subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte banPlayerId))
                    {
                        Utils.SendMessage(GetString("BanCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (banPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("BanCommandBanHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var bannedPlayer = Utils.GetPlayerById(banPlayerId);
                    if (bannedPlayer == null)
                    {
                        Utils.SendMessage(GetString("BanCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    // Ban the specified player
                    AmongUsClient.Instance.KickPlayer(bannedPlayer.GetClientId(), true);
                    string bannedPlayerName = bannedPlayer.GetRealName();
                    string textToSend1 = $"{bannedPlayerName} {GetString("BanCommandBanned")}{PlayerControl.LocalPlayer.name} \nReason: {banReason}\n";
                    if (GameStates.IsInGame)
                    {
                        textToSend1 += $" {GetString("BanCommandBannedRole")} {GetString(bannedPlayer.GetCustomRole().ToString())}";
                    }
                    Utils.SendMessage(textToSend1);
                    //string moderatorName = PlayerControl.LocalPlayer.GetRealName().ToString();
                    //int startIndex = moderatorName.IndexOf("♥</color>") + "♥</color>".Length;
                    //moderatorName = moderatorName.Substring(startIndex);
                    //string extractedString = 
                    string moderatorFriendCode = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string bannedPlayerFriendCode = bannedPlayer.FriendCode.ToString();
                    string modLogname = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n1) ? n1 : "";
                    string banlogname = Main.AllPlayerNames.TryGetValue(bannedPlayer.PlayerId, out var n11) ? n11 : "";
                    string logMessage = $"[{DateTime.Now}] {moderatorFriendCode},{modLogname} Banned: {bannedPlayerFriendCode},{banlogname} Reason: {banReason}";
                    File.AppendAllText(modLogFiles, logMessage + Environment.NewLine);
                    break;

                case "/warn":
                case "/aviso":
                case "/варн":
                case "/пред":
                case "/предупредить":
                case "/警告":
                case "/提醒":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte warnPlayerId))
                    {
                        Utils.SendMessage(GetString("WarnCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (warnPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("WarnCommandWarnHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var warnedPlayer = Utils.GetPlayerById(warnPlayerId);
                    if (warnedPlayer == null)
                    {
                        Utils.SendMessage(GetString("WarnCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    // warn the specified player
                    string textToSend2 = "";
                    string warnReason = "Reason : Not specified\n";
                    string warnedPlayerName = warnedPlayer.GetRealName();
                    //textToSend2 = $" {warnedPlayerName} {GetString("WarnCommandWarned")} ~{player.name}";
                    if (args.Length > 2)
                    {
                        warnReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
                    }
                    else
                    {
                        Utils.SendMessage(GetString("WarnExample"), PlayerControl.LocalPlayer.PlayerId);
                    }
                    textToSend2 = $" {warnedPlayerName} {GetString("WarnCommandWarned")} {warnReason} ~{PlayerControl.LocalPlayer.name}";
                    Utils.SendMessage(textToSend2);
                    //string moderatorName1 = PlayerControl.LocalPlayer.GetRealName().ToString();
                    //int startIndex1 = moderatorName1.IndexOf("♥</color>") + "♥</color>".Length;
                    //moderatorName1 = moderatorName1.Substring(startIndex1);
                    string modLogname1 = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n2) ? n2 : "";
                    string warnlogname = Main.AllPlayerNames.TryGetValue(warnedPlayer.PlayerId, out var n12) ? n12 : "";

                    string moderatorFriendCode1 = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string warnedPlayerFriendCode = warnedPlayer.FriendCode.ToString();
                    string warnedPlayerHashPuid = warnedPlayer.GetClient().GetHashedPuid();
                    string logMessage1 = $"[{DateTime.Now}] {moderatorFriendCode1},{modLogname1} Warned: {warnedPlayerFriendCode},{warnedPlayerHashPuid},{warnlogname} Reason: {warnReason}";
                    File.AppendAllText(modLogFiles, logMessage1 + Environment.NewLine);

                    break;

                case "/kick":
                case "/expulsar":
                case "/кик":
                case "/кикнуть":
                case "/выгнать":
                case "/踢出":
                case "/踢":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte kickPlayerId))
                    {
                        Utils.SendMessage(GetString("KickCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (kickPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("KickCommandKickHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var kickedPlayer = Utils.GetPlayerById(kickPlayerId);
                    if (kickedPlayer == null)
                    {
                        Utils.SendMessage(GetString("KickCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    // Kick the specified player
                    AmongUsClient.Instance.KickPlayer(kickedPlayer.GetClientId(), false);
                    string kickedPlayerName = kickedPlayer.GetRealName();
                    string kickReason = "Reason : Not specified\n";
                    if (args.Length > 2)
                        kickReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
                    else
                    {
                        Utils.SendMessage("Use /kick [id] [reason] in future. \nExample :-\n /kick 5 not following rules", PlayerControl.LocalPlayer.PlayerId);
                    }
                    string textToSend = $"{kickedPlayerName} {GetString("KickCommandKicked")} {PlayerControl.LocalPlayer.name} \n {kickReason}";

                    if (GameStates.IsInGame)
                    {
                        textToSend += $" {GetString("KickCommandKickedRole")} {GetString(kickedPlayer.GetCustomRole().ToString())}";
                    }
                    Utils.SendMessage(textToSend);
                    //string moderatorName2 = PlayerControl.LocalPlayer.GetRealName().ToString();
                    //int startIndex2 = moderatorName2.IndexOf("♥</color>") + "♥</color>".Length;
                    //moderatorName2 = moderatorName2.Substring(startIndex2);

                    string modLogname2 = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n3) ? n3 : "";
                    string kicklogname = Main.AllPlayerNames.TryGetValue(kickedPlayer.PlayerId, out var n13) ? n13 : "";

                    string moderatorFriendCode2 = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string kickedPlayerFriendCode = kickedPlayer.FriendCode.ToString();
                    string kickedPlayerHashPuid = kickedPlayer.GetClient().GetHashedPuid();
                    string logMessage2 = $"[{DateTime.Now}] {moderatorFriendCode2},{modLogname2} Kicked: {kickedPlayerFriendCode},{kickedPlayerHashPuid},{kicklogname} Reason: {kickReason}";
                    File.AppendAllText(modLogFiles, logMessage2 + Environment.NewLine);

                    break;

                case "/tagcolor":
                case "/tagcolour":
                case "/标签颜色":
                case "/附加名称颜色":
                    canceled = true;
                    string name = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n) ? n : "";
                    if (name == "") break;
                    if (!name.Contains('\r') && PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag())
                    {
                        if (!GameStates.IsLobby)
                        {
                            Utils.SendMessage(GetString("ColorCommandNoLobby"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        subArgs = args.Length != 2 ? "" : args[1];
                        if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                        {
                            Logger.Msg($"{subArgs}", "tagcolor");
                            Utils.SendMessage(GetString("TagColorInvalidHexCode"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        string tagColorFilePath = $"{sponsorTagsFiles}/{PlayerControl.LocalPlayer.FriendCode}.txt";
                        if (!File.Exists(tagColorFilePath))
                        {
                            Logger.Msg($"File Not exist, creating file at {tagColorFilePath}", "tagcolor");
                            File.Create(tagColorFilePath).Close();
                        }
                        File.WriteAllText(tagColorFilePath, $"{subArgs}");
                    }
                    break;

                case "/exe":
                case "/уничтожить":
                case "/повесить":
                case "/казнить":
                case "/казнь":
                case "/мут":
                case "/驱逐":
                case "/驱赶":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                    var player = Utils.GetPlayerById(id);
                    if (player != null)
                    {
                        player.Data.IsDead = true;
                        player.SetDeathReason(PlayerState.DeathReason.etc);
                        player.SetRealKiller(PlayerControl.LocalPlayer);
                        Main.PlayerStates[player.PlayerId].SetDead();
                        player.RpcExileV2();
                        MurderPlayerPatch.AfterPlayerDeathTasks(PlayerControl.LocalPlayer, player, GameStates.IsMeeting);

                        if (player.IsHost()) Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                        else Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    }
                    break;

                case "/kill":
                case "/matar":
                case "/убить":
                case "/击杀":
                case "/杀死":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
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
                    break;

                case "/re":
                case "/revive":
                case "/复活":
                    canceled = true;
                    if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev)
                    {
                        Utils.SendMessage($"{GetString("InvalidPermissionCMD")}", PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int id3)) break;
                    var target1 = Utils.GetPlayerById(id3);
                    if (target1 != null)
                    {
                        target1.RpcRevive();
                        Utils.SendMessage(string.Format(GetString("Message.Revive"), target1.Data.PlayerName), PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;

                case "/addmod":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte AddModPlayerId))
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (AddModPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("CommandAddHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var addModPlayerId = Utils.GetPlayerById(AddModPlayerId);
                    if (addModPlayerId == null)
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (Utils.IsPlayerModerator(addModPlayerId.FriendCode))
                    {
                        Utils.SendMessage(GetString("PlayerAlreadyMod"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (addModPlayerId != null)
                    {
                        string moderatorFriendCode10 = addModPlayerId.FriendCode.ToString();
                        string Message10 = $"{moderatorFriendCode10}";
                        File.AppendAllText(modFiles, Message10 + Environment.NewLine);
                        Utils.SendMessage(GetString("PlayerJoinModList"), PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;

                case "/deletemod":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte DeleteModPlayerId))
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (DeleteModPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("CommandDeleteHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var deleteModPlayerId = Utils.GetPlayerById(DeleteModPlayerId);
                    if (deleteModPlayerId == null)
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (!Utils.IsPlayerModerator(deleteModPlayerId.FriendCode))
                    {
                        Utils.SendMessage(GetString("PlayerNotMod"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (deleteModPlayerId != null)
                    {
                        string moderatorFriendCode11 = deleteModPlayerId.FriendCode.ToString();
                        File.WriteAllLines(modFiles, File.ReadAllLines(modFiles).Where(x => !x.Contains(moderatorFriendCode11)));
                        Utils.SendMessage(GetString("PlayerDeleteFromModList"), PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;

                case "/addvip":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte AddVipPlayerId))
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (AddVipPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("CommandAddHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var addVipPlayerId = Utils.GetPlayerById(AddVipPlayerId);
                    if (addVipPlayerId == null)
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (Utils.IsPlayerVIP(addVipPlayerId.FriendCode))
                    {
                        Utils.SendMessage(GetString("PlayerAlreadyVip"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (addVipPlayerId != null)
                    {
                        string vipFriendCode10 = addVipPlayerId.FriendCode.ToString();
                        string Message11 = $"{vipFriendCode10}";
                        File.AppendAllText(vipFiles, Message11 + Environment.NewLine);
                        Utils.SendMessage(GetString("PlayerJoinVipList"), PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;

                case "/deletevip":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte DeleteVipPlayerId))
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (DeleteVipPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("CommandDeleteHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var deleteVipPlayerId = Utils.GetPlayerById(DeleteVipPlayerId);
                    if (deleteVipPlayerId == null)
                    {
                        Utils.SendMessage(GetString("CommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (!Utils.IsPlayerVIP(deleteVipPlayerId.FriendCode))
                    {
                        Utils.SendMessage(GetString("PlayerNotVip"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (deleteVipPlayerId != null)
                    {
                        string vipFriendCode11 = deleteVipPlayerId.FriendCode.ToString();
                        File.WriteAllLines(vipFiles, File.ReadAllLines(vipFiles).Where(x => !x.Contains(vipFriendCode11)));
                        Utils.SendMessage(GetString("PlayerDeleteFromVipList"), PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;

                case "/colour":
                case "/color":
                case "/cor":
                case "/цвет":
                case "/颜色":
                case "/更改颜色":
                case "/修改颜色":
                case "/换颜色":
                    canceled = true;
                    if (GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    subArgs = args.Length < 2 ? "" : args[1];
                    var color = Utils.MsgToColor(subArgs, true);
                    if (color == byte.MaxValue)
                    {
                        Utils.SendMessage(GetString("IllegalColor"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    PlayerControl.LocalPlayer.RpcSetColor(color);
                    Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/quit":
                case "/qt":
                case "/sair":
                case "/退出":
                case "/退":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.CanNotUseByHost"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/xf":
                case "/修复":
                case "/修":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    foreach (var pc in Main.EnumeratePlayerControls())
                    {
                        if (pc.IsAlive()) continue;
                        pc.SetName(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                    //Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
                    Utils.SendMessage(GetString("Message.TryFixName"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/id":
                case "/айди":
                case "/编号":
                case "/玩家编号":
                    canceled = true;
                    string msgText = GetString("PlayerIdList");
                    foreach (var pc in Main.EnumeratePlayerControls())
                    {
                        if (pc == null) continue;
                        msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                    }
                    Utils.SendMessage(msgText, PlayerControl.LocalPlayer.PlayerId);
                    break;

                /*
                case "/qq":
                    canceled = true;
                    if (Main.newLobby) Cloud.ShareLobby(true);
                    else Utils.SendMessage("很抱歉，每个房间车队姬只会发一次", PlayerControl.LocalPlayer.PlayerId);
                    break;
                */

                case "/setrole":
                case "/设置的职业":
                case "/指定的职业":
                    canceled = true;
                    subArgs = text.Remove(0, 8);
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug);
                    break;

                case "/changerole":
                case "/mudarfunção":
                case "/改变职业":
                case "/修改职业":
                    canceled = true;
                    if (GameStates.IsHideNSeek) break;
                    if (!GameStates.IsInGame) break;
                    if (GameStates.IsOnlineGame && !PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug) break;
                    subArgs = text.Remove(0, 11);
                    var setRole = FixRoleNameInput(subArgs).ToLower().Trim().Replace(" ", string.Empty);
                    Logger.Info(setRole, "changerole Input");
                    foreach (var rl in CustomRolesHelper.AllRoles)
                    {
                        if (rl.IsVanilla()) continue;
                        var roleName = GetString(rl.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
                        //Logger.Info(roleName, "2");
                        if (setRole == roleName)
                        {
                            PlayerControl.LocalPlayer.GetRoleClass()?.OnRemove(PlayerControl.LocalPlayer.PlayerId);
                            PlayerControl.LocalPlayer.RpcChangeRoleBasis(rl);
                            PlayerControl.LocalPlayer.RpcSetCustomRole(rl);
                            PlayerControl.LocalPlayer.GetRoleClass().OnAdd(PlayerControl.LocalPlayer.PlayerId);
                            Utils.SendMessage(string.Format("Debug Set your role to {0}", rl.GetActualRoleName()), PlayerControl.LocalPlayer.PlayerId);
                            Utils.NotifyRoles(SpecifyTarget: PlayerControl.LocalPlayer, NoCache: true);
                            Utils.MarkEveryoneDirtySettings();
                            break;
                        }
                    }
                    break;

                case "/end":
                case "/encerrar":
                case "/завершить":
                case "/结束":
                case "/结束游戏":
                    canceled = true;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                    break;
                case "/cosid":
                case "/装扮编号":
                case "/衣服编号":
                    canceled = true;
                    var of = PlayerControl.LocalPlayer.Data.DefaultOutfit;
                    Logger.Warn($"ColorId: {of.ColorId}", "Get Cos Id");
                    Logger.Warn($"PetId: {of.PetId}", "Get Cos Id");
                    Logger.Warn($"HatId: {of.HatId}", "Get Cos Id");
                    Logger.Warn($"SkinId: {of.SkinId}", "Get Cos Id");
                    Logger.Warn($"VisorId: {of.VisorId}", "Get Cos Id");
                    Logger.Warn($"NamePlateId: {of.NamePlateId}", "Get Cos Id");
                    break;

                case "/mt":
                case "/hy":
                case "/强制过会议":
                case "/强制跳过会议":
                case "/过会议":
                case "/结束会议":
                case "/强制结束会议":
                case "/跳过会议":
                    canceled = true;
                    if (GameStates.IsMeeting)
                    {
                        if (MeetingHud.Instance)
                        {
                            MeetingHud.Instance.RpcClose();
                        }
                    }
                    else
                    {
                        PlayerControl.LocalPlayer.NoCheckStartMeeting(null, force: true);
                    }
                    break;

                case "/cs":
                case "/播放声音":
                case "/播放音效":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    PlayerControl.LocalPlayer.RPCPlayCustomSound(subArgs.Trim());
                    break;

                case "/sd":
                case "/播放音效给":
                case "/播放声音给":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (args.Length < 1 || !int.TryParse(args[1], out int sound1)) break;
                    RPC.PlaySoundRPC((Sounds)sound1, PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/poll":
                case "/发起投票":
                case "/执行投票":
                    canceled = true;


                    if (args.Length == 2 && args[1] == GetString("Replay") && Pollvotes.Any() && PollMSG != string.Empty)
                    {
                        Utils.SendMessage(PollMSG);
                        break;
                    }

                    PollMSG = string.Empty;
                    Pollvotes.Clear();
                    PollQuestions.Clear();
                    PollVoted.Clear();
                    Polltimer = 120f;

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

                        Color32 clr = new(47, 234, 45, 255); //Main.PlayerColors.First(x => x.Key == PlayerControl.LocalPlayer.PlayerId).Value;
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
                        Utils.SendMessage(GetString("Poll.MissingPlayers"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Poll.OnlyInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (args.SkipWhile(x => !x.Contains('?')).ToArray().Length < 3 || !args.Any(x => x.Contains('?')))
                    {
                        Utils.SendMessage(GetString("PollUsage"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    var resultat = args.TakeWhile(x => !x.Contains('?')).Concat(args.SkipWhile(x => !x.Contains('?')).Take(1));

                    string tytul = string.Join(" ", resultat.Skip(1));
                    bool Longtitle = tytul.Length > 30;
                    tytul = Utils.ColorString(Palette.PlayerColors[PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId], tytul);
                    var altTitle = Utils.ColorString(new Color32(151, 198, 230, 255), GetString("PollTitle"));

                    var ClearTIT = args.ToList();
                    ClearTIT.RemoveRange(0, resultat.ToArray().Length);

                    var Questions = ClearTIT.ToArray();
                    string msg = "";


                    if (Longtitle) msg += "<voffset=-0.5em>" + tytul + "</voffset>\n\n";
                    for (int i = 0; i < Math.Clamp(Questions.Length, 2, 5); i++)
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

                    break;

                case "/rps":
                case "/剪刀石头布":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    canceled = true;
                    subArgs = args.Length != 2 ? "" : args[1];

                    if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("RpsCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice))
                    {
                        Utils.SendMessage(GetString("RpsCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (playerChoice < 0 || playerChoice > 2)
                    {
                        Utils.SendMessage(GetString("RpsCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        var rand = IRandom.Instance;
                        int botChoice = rand.Next(0, 3);
                        var rpsList = new List<string> { GetString("Rock"), GetString("Paper"), GetString("Scissors") };
                        if (botChoice == playerChoice)
                        {
                            Utils.SendMessage(string.Format(GetString("RpsDraw"), rpsList[botChoice]), PlayerControl.LocalPlayer.PlayerId);
                        }
                        else if ((botChoice == 0 && playerChoice == 2) ||
                                 (botChoice == 1 && playerChoice == 0) ||
                                 (botChoice == 2 && playerChoice == 1))
                        {
                            Utils.SendMessage(string.Format(GetString("RpsLose"), rpsList[botChoice]), PlayerControl.LocalPlayer.PlayerId);
                        }
                        else
                        {
                            Utils.SendMessage(string.Format(GetString("RpsWin"), rpsList[botChoice]), PlayerControl.LocalPlayer.PlayerId);
                        }
                        break;
                    }
                case "/coinflip":
                case "/抛硬币":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    canceled = true;

                    if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("CoinFlipCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        var rand = IRandom.Instance;
                        int botChoice = rand.Next(1, 101);
                        var coinSide = (botChoice < 51) ? GetString("Heads") : GetString("Tails");
                        Utils.SendMessage(string.Format(GetString("CoinFlipResult"), coinSide), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                case "/gno":
                case "/猜数字":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    canceled = true;
                    if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("GNoCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (subArgs == "" || !int.TryParse(subArgs, out int guessedNo))
                    {
                        Utils.SendMessage(GetString("GNoCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (guessedNo < 0 || guessedNo > 99)
                    {
                        Utils.SendMessage(GetString("GNoCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        int targetNumber = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                        if (Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] == -1)
                        {
                            var rand = IRandom.Instance;
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] = rand.Next(0, 100);
                            targetNumber = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                        }
                        Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]--;
                        if (Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1] == 0 && guessedNo != targetNumber)
                        {
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] = -1;
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1] = 7;
                            //targetNumber = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                            Utils.SendMessage(string.Format(GetString("GNoLost"), targetNumber), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else if (guessedNo < targetNumber)
                        {
                            Utils.SendMessage(string.Format(GetString("GNoLow"), Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else if (guessedNo > targetNumber)
                        {
                            Utils.SendMessage(string.Format(GetString("GNoHigh"), Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else
                        {
                            Utils.SendMessage(string.Format(GetString("GNoWon"), Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]), PlayerControl.LocalPlayer.PlayerId);
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] = -1;
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1] = 7;
                            break;
                        }

                    }
                case "/rand":
                case "/XY数字":
                case "/范围游戏":
                case "/猜范围":
                case "/范围":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    canceled = true;
                    subArgs = args.Length != 3 ? "" : args[1];
                    subArgs2 = args.Length != 3 ? "" : args[2];

                    if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("RandCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice1) || subArgs2 == "" || !int.TryParse(subArgs2, out int playerChoice2))
                    {
                        Utils.SendMessage(GetString("RandCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        var rand = IRandom.Instance;
                        int botResult = rand.Next(playerChoice1, playerChoice2 + 1);
                        Utils.SendMessage(string.Format(GetString("RandResult"), botResult), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                case "/8ball":
                case "/8号球":
                case "/幸运球":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    canceled = true;
                    var rando = IRandom.Instance;
                    int result = rando.Next(0, 16);
                    string str = "";
                    switch (result)
                    {
                        case 0:
                            str = GetString("8BallYes");
                            break;
                        case 1:
                            str = GetString("8BallNo");
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
                    Utils.SendMessage("<align=\"center\"><size=150%>" + str + "</align></size>", PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medium), GetString("8BallTitle")));
                    break;
                case "/start":
                case "/开始":
                case "/старт":
                    canceled = true;
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (GameStates.IsCountDown)
                    {
                        Utils.SendMessage(GetString("StartCommandCountdown"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !int.TryParse(subArgs, out int countdown))
                    {
                        countdown = 5;
                    }
                    else
                    {
                        countdown = int.Parse(subArgs);
                    }
                    if (countdown < 0 || countdown > 99)
                    {
                        Utils.SendMessage(string.Format(GetString("StartCommandInvalidCountdown"), 0, 99), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    GameStartManager.Instance.BeginGame();
                    GameStartManager.Instance.countDownTimer = countdown;
                    Utils.SendMessage(string.Format(GetString("StartCommandStarted"), PlayerControl.LocalPlayer.name));
                    Logger.Info("Game Starting", "ChatCommand");
                    break;
                case "/deck":
                    canceled = true;
                    DeckCommand(PlayerControl.LocalPlayer, text, args);
                    break;
                case "/ds":
                case "/draftstart":
                    canceled = true;
                    DraftStartCommand(PlayerControl.LocalPlayer, text, args);
                    break;
                case "/draft":
                    canceled = true;
                    DraftCommand(PlayerControl.LocalPlayer, text, args);
                    break;
                case "/dd":
                case "/draftdescription":
                    canceled = true;
                    DraftDescriptionCommand(PlayerControl.LocalPlayer, text, args);
                    break;
                case "/spam":
                    canceled = true;
                    ChatManager.SendQuickChatSpam();
                    ChatManager.SendPreviousMessagesToAll();
                    break;

                case "/fix" 
                or "/blackscreenfix" 
                or "/fixblackscreen":
                    canceled = true;
                    FixCommand(PlayerControl.LocalPlayer, text, args);
                    break;

                case "/afkexempt":
                    canceled = true;
                    AFKExemptCommand(PlayerControl.LocalPlayer, text, args);
                    break;

                case "/spectate":
                case "/观战":
                    canceled = true;
                    SpectateCommand(PlayerControl.LocalPlayer, text, args);
                    break;

                default:
                    Main.isChatCommand = false;
                    break;
            }
        }
        goto Skip;
    Canceled:
        Main.isChatCommand = false;
        canceled = true;
    Skip:
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
        if (canceled)
        {
            Logger.Info("Command Canceled", "ChatCommand");
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(cancelVal);

            __instance.quickChatMenu.Clear();
            __instance.quickChatField.Clear();
        }

        if (!canceled && AmongUsClient.Instance.AmHost && ChatUpdatePatch.TempReviveHostRunning)
        {
            if (!WaitingToSend) Main.Instance.StartCoroutine(Wait());
            return false;
            
            IEnumerator Wait()
            {
                WaitingToSend = true;
                while (ChatUpdatePatch.TempReviveHostRunning && AmongUsClient.Instance.AmHost) yield return null;
                yield return new WaitForSecondsRealtime(0.5f);
                if (GameStates.IsEnded || GameStates.IsLobby) yield break;
                WaitingToSend = false;
                if (HudManager.InstanceExists) HudManager.Instance.Chat.SendChat();
            }
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
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                {
                    Utils.SendMessage(GetString("ModeDescribe.FFA"), playerId);
                    return;
                }
            case CustomGameMode.SpeedRun:
                {
                    Utils.SendMessage(GetString("ModeDescribe.SpeedRun"), playerId);
                    return;
                }
            case CustomGameMode.TagMode:
                {
                    Utils.SendMessage(GetString("ModeDescribe.TagMode"), playerId);
                    return;
                }
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
            Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), playerId);
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
            if (result.IsGhostRole() || !shouldDevAssign || result.IsAddonAssignedMidGame())
            {
                Utils.SendMessage(string.Format(GetString("Message.YTPlanSelectFailed"), Translator.GetActualRoleName(result)), playerId);
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

            Utils.SendMessage(string.Format(GetString("Message.YTPlanSelected"), Translator.GetActualRoleName(result)), playerId);
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


        var Des = result.GetStaticRoleClass().IsMethodOverridden("OnPet") && Options.UsePets.GetBool() ? result.GetInfoLong() + $"<size=70%>{GetString("SupportsPetMessage")}</size>" 
           : result.GetInfoLong();
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
            text = "/" + text[4..].TrimStart();
        }
        //if (!text.StartsWith("/")) return;
        string[] args = text.Split(' ');
        string subArgs = "";
        string subArgs2 = "";

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
        if (Lovers.LoversMsg(player, text)) { canceled = true; Logger.Info($"Is Lovers Private Chat", "OnReceiveChat"); return; }
        if (ImpostorChannel(player, text)) { canceled = true; Logger.Info($"Is Impostor Channel", "OnReceiveChat"); return; }
        if (Jackal.JackalChannel(player, text)) { canceled = true; Logger.Info($"Is Jackal Channel", "OnReceiveChat"); return; }
        if (Jailer.JailerChannel(player, text)) { canceled = true; Logger.Info($"Is Jailer Channel", "OnReceiveChat"); return; }

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

        switch (args[0])
        {
            case "/r":
            case "/role":
            case "/р":
            case "/роль":
                Logger.Info($"Command '/r' was activated", "OnReceiveChat");
                if (text.Contains("/role") || text.Contains("/роль"))
                    subArgs = text.Remove(0, 5);
                else
                    subArgs = text.Remove(0, 2);
                SendRolesInfo(subArgs, player.PlayerId, isDev: player.FriendCode.GetDevUser().DeBug);
                break;

            case "/m":
            case "/myrole":
            case "/minhafunção":
            case "/м":
            case "/мояроль":
            case "/身份":
            case "/我":
            case "/我的身份":
            case "/我的职业":
                Logger.Info($"Command '/m' was activated", "OnReceiveChat");
                var role = player.GetCustomRole();
                if (GameStates.IsInGame)
                {
                    var Des = player.PetActivatedAbility() ? player.GetRoleInfo(true) + $"<size=70%>{GetString("SupportsPetMessage")}</size>" : player.GetRoleInfo(true);
                    var title = $"<color=#ffffff>" + role.GetRoleTitle() + "</color>\n";
                    var Conf = new StringBuilder();
                    var Sub = new StringBuilder();
                    var rlHex = Utils.GetRoleColorCode(role);
                    var SubTitle = $"<color={rlHex}>" + GetString("YourAddon") + "</color>\n";

                    if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                        Utils.ShowChildrenSettings(opt, ref Conf);
                    var cleared = Conf.ToString();
                    var Setting = $"<color={rlHex}>{GetString(role.ToString())} {GetString("Settings:")}</color>\n";
                    Conf.Clear().Append($"<color=#ffffff>" + $"<size={Csize}>" + Setting + cleared + "</size>" + "</color>");

                    foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles.ToArray())
                    {
                        Sub.Append($"\n\n" + $"<size={Asize}>" + Utils.GetRoleTitle(subRole) + Utils.GetInfoLong(subRole) + "</size>");

                    }
                    if (Sub.ToString() != string.Empty)
                    {
                        var ACleared = Sub.ToString().Remove(0, 2);
                        ACleared = ACleared.Length > 1200 ? $"<size={Asize}>" + ACleared.RemoveHtmlTags() + "</size>" : ACleared;
                        Sub.Clear().Append(ACleared);
                    }

                    Utils.SendMessage(Des, player.PlayerId, title, noReplay: true);
                    Utils.SendMessage("", player.PlayerId, Conf.ToString(), noReplay: true);
                    if (Sub.ToString() != string.Empty) Utils.SendMessage(Sub.ToString(), player.PlayerId, SubTitle, noReplay: true);

                    Logger.Info($"Command '/m' should be send message", "OnReceiveChat");
                }
                else
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                break;

            case "/h":
            case "/help":
            case "/ajuda":
            case "/хелп":
            case "/хэлп":
            case "/помощь":
            case "/帮助":
            case "/教程":
                Utils.ShowHelpToClient(player.PlayerId);
                break;

            case "/ans":
            case "/asw":
            case "/answer":
            case "/回答":
                Quizmaster.AnswerByChat(player, args);
                break;

            case "/qmquiz":
            case "/提问":
                Quizmaster.ShowQuestion(player);
                break;

            case "/l":
            case "/lastresult":
            case "/fimdejogo":
            case "/上局信息":
            case "/信息":
            case "/情况":
                Utils.ShowKillLog(player.PlayerId);
                Utils.ShowLastRoles(player.PlayerId);
                Utils.ShowLastResult(player.PlayerId);
                break;

            case "/gr":
            case "/gameresults":
            case "/resultados":
            case "/对局结果":
            case "/上局结果":
            case "/结果":
                Utils.ShowLastResult(player.PlayerId);
                break;

            case "/kh":
            case "/killlog":
            case "/击杀日志":
            case "/击杀情况":
                Utils.ShowKillLog(player.PlayerId);
                break;

            case "/rs":
            case "/sum":
            case "/rolesummary":
            case "/sumario":
            case "/sumário":
            case "/summary":
            case "/результат":
            case "/上局职业":
            case "/职业信息":
            case "/对局职业":
                Utils.ShowLastRoles(player.PlayerId);
                break;

            case "/ghostinfo":
            case "/幽灵职业介绍":
            case "/鬼魂职业介绍":
            case "/幽灵职业":
            case "/鬼魂职业":
                if (GameStates.IsInGame)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }
                Utils.SendMessage(GetString("Message.GhostRoleInfo"), player.PlayerId);
                break;

            case "/apocinfo":
            case "/apocalypseinfo":
            case "/灾厄中立职业介绍":
            case "/灾厄中立介绍":
            case "/灾厄中立":
            case "/灾厄类中立职业介绍":
            case "/灾厄类中立介绍":
            case "/灾厄类中立":
                Utils.SendMessage(GetString("Message.ApocalypseInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Apocalypse), GetString("ApocalypseInfoTitle")));
                break;

            case "/coveninfo":
            case "/covinfo":
                Utils.SendMessage(GetString("Message.CovenInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Coven), GetString("CovenInfoTitle")));
                break;

            case "/rn":
            case "/rename":
            case "/renomear":
            case "/переименовать":
            case "/重命名":
            case "/命名为":
                if (Options.PlayerCanSetName.GetBool() || player.FriendCode.GetDevUser().IsDev || player.FriendCode.GetDevUser().NameCmd || TagManager.ReadPermission(player.FriendCode) >= 1)
                {
                    if (GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                        break;
                    }
                    if (args.Length < 1) break;
                    if (args.Skip(1).Join(delimiter: " ").Length is > 10 or < 1 || args.Skip(1).Join(delimiter: " ")[0] == '<') // <#ffffff>E is a valid name without this
                    {
                        Utils.SendMessage(GetString("Message.AllowNameLength"), player.PlayerId);
                        break;
                    }
                    Main.AllPlayerNames[player.PlayerId] = args.Skip(1).Join(delimiter: " ");
                    Utils.SendMessage(string.Format(GetString("Message.SetName"), args.Skip(1).Join(delimiter: " ")), player.PlayerId);
                    break;
                }
                else
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                }
                break;

            case "/n":
            case "/now":
            case "/atual":
            case "/设置":
            case "/系统设置":
            case "/模组设置":
                subArgs = args.Length < 2 ? "" : args[1];
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
                break;

            case "/f":
            case "/factions":
                canceled = true;
                var impCount = $"{GetString("NumberOfImpostors")}: {GameOptionsManager.Instance.GameHostOptions.NumImpostors}";
                var nnkCount = $"{GetString("NonNeutralKillingRolesMinPlayer")}: {Options.NonNeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NonNeutralKillingRolesMaxPlayer")}: {Options.NonNeutralKillingRolesMaxPlayer.GetInt()}";
                var nkCount = $"{GetString("NeutralKillingRolesMinPlayer")}: {Options.NeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NeutralKillingRolesMaxPlayer")}: {Options.NeutralKillingRolesMaxPlayer.GetInt()}";
                var apocCount = $"{GetString("NeutralApocalypseRolesMinPlayer")}: {Options.NeutralApocalypseRolesMinPlayer.GetInt()}\n{GetString("NeutralApocalypseRolesMaxPlayer")}: {Options.NeutralApocalypseRolesMaxPlayer.GetInt()}";
                var covCount = $"{GetString("CovenRolesMinPlayer")}: {Options.CovenRolesMinPlayer.GetInt()}\n{GetString("CovenRolesMaxPlayer")}: {Options.CovenRolesMaxPlayer.GetInt()}";
                var addonCount = $"{GetString("NoLimitAddonsNumMax")}: {Options.NoLimitAddonsNumMax.GetInt()}";
                Utils.SendMessage($"{impCount}\n{nnkCount}\n{nkCount}\n{apocCount}\n{covCount}\n{addonCount}", player.PlayerId, $"<color={Main.ModColor}>{GetString("FactionSettingsTitle")}</color>");
                break;
            case "/up":
            case "/指定":
            case "/成为":
                _ = text.Remove(0, 3);
                if (!Options.EnableUpMode.GetBool())
                {
                    Utils.SendMessage(string.Format(GetString("Message.YTPlanDisabled"), GetString("EnableYTPlan")), player.PlayerId);
                    break;
                }
                else
                {
                    Utils.SendMessage(GetString("Message.OnlyCanBeUsedByHost"), player.PlayerId);
                    break;
                }

            case "/win":
            case "/winner":
            case "/vencedor":
            case "/胜利":
            case "/获胜":
            case "/赢":
            case "/胜利者":
            case "/获胜的人":
            case "/赢家":
                if (Main.winnerNameList.Count == 0) Utils.SendMessage(GetString("NoInfoExists"), player.PlayerId);
                else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList), player.PlayerId);
                break;


            case "/pv":
                canceled = true;
                if (!Pollvotes.Any())
                {
                    Utils.SendMessage(GetString("Poll.Inactive"), player.PlayerId);
                    break;
                }
                if (PollVoted.Contains(player.PlayerId))
                {
                    Utils.SendMessage(GetString("Poll.AlreadyVoted"), player.PlayerId);
                    break;
                }

                subArgs = args.Length != 2 ? "" : args[1];
                char vote = ' ';

                if (int.TryParse(subArgs, out int integer) && (Pollvotes.Count - 1) >= integer)
                {
                    vote = char.ToUpper((char)(integer + 65));
                }
                else if (!(char.TryParse(subArgs, out vote) && Pollvotes.ContainsKey(char.ToUpper(vote))))
                {
                    Utils.SendMessage(GetString("Poll.VotingInfo"), player.PlayerId);
                    break;
                }
                vote = char.ToUpper(vote);

                PollVoted.Add(player.PlayerId);
                Pollvotes[vote]++;
                Utils.SendMessage(string.Format(GetString("Poll.YouVoted"), vote, Pollvotes[vote]), player.PlayerId);
                Logger.Info($"The new value of {vote} is {Pollvotes[vote]}", "TestPV_CHAR");

                break;

            case "/icon":
            case "/icons":
            case "/符号":
            case "/标志":
                {
                    Utils.SendMessage(GetString("Command.icons"), player.PlayerId, GetString("IconsTitle"));
                    break;
                }

            case "/kc":
            case "/kcount":
            case "/количество":
            case "/убийцы":
            case "/存活阵营":
            case "/阵营":
            case "/存活阵营信息":
            case "/阵营信息":
                if (GameStates.IsLobby) break;

                if (!Options.EnableKillerLeftCommand.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }

                var sub = new StringBuilder();
                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.Standard:
                        var allAlivePlayers = Main.EnumerateAlivePlayerControls();
                        int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor) && !pc.Is(CustomRoles.Narc));
                        int madnum = allAlivePlayers.Count(pc => (pc.GetCustomRole().IsMadmate() && !pc.Is(CustomRoles.Narc)) || pc.Is(CustomRoles.Madmate));
                        int apocnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNA());
                        int neutralnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNK());
                        int covnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Coven));

                        sub.Append(string.Format(GetString("Remaining.ImpostorCount"), impnum));

                        if (Options.ShowMadmatesInLeftCommand.GetBool())
                            sub.Append(string.Format("\n\r" + GetString("Remaining.MadmateCount"), madnum));

                        if (Options.ShowApocalypseInLeftCommand.GetBool())
                            sub.Append(string.Format("\n\r" + GetString("Remaining.ApocalypseCount"), apocnum));

                        if (Options.ShowCovenInLeftCommand.GetBool())
                            sub.Append(string.Format("\n\r" + GetString("Remaining.CovenCount"), covnum));

                        sub.Append(string.Format("\n\r" + GetString("Remaining.NeutralCount"), neutralnum));
                        break;

                    case CustomGameMode.FFA:
                        FFAManager.AppendFFAKcount(sub);
                        break;

                    case CustomGameMode.SpeedRun:
                        SpeedRun.AppendSpeedRunKcount(sub);
                        break;

                    case CustomGameMode.TagMode:
                        TagMode.AppendTagModeKcount(sub);
                        break;
                }

                Utils.SendMessage(sub.ToString(), player.PlayerId);
                break;

            case "/d":
            case "/death":
            case "/morto":
            case "/умер":
            case "/причина":
            case "/死亡原因":
            case "/死亡":
                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                else if (player.IsAlive())
                {
                    Utils.SendMessage(string.Format(GetString("DeathCmd.NotDead"), player.GetRealName(), player.GetCustomRole().ToColoredString()), player.PlayerId);
                    break;
                }
                else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                {
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Ejected"), player.PlayerId);
                    break;
                }
                else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Shrouded)
                {
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Shrouded"), player.PlayerId);
                    break;
                }
                else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.FollowingSuicide)
                {
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Lovers"), player.PlayerId);
                    break;
                }
                else
                {
                    var killer = player.GetRealKiller(out var MurderRole);
                    string killerName = killer == null ? "N/A" : killer.GetRealName(clientData: true);
                    string killerRole = killer == null ? "N/A" : Utils.GetRoleName(MurderRole);
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(player.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killerName + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{killerRole}</color>" + "</b>", player.PlayerId);
                    break;
                }

            case "/t":
            case "/template":
            case "/шаблон":
            case "/пример":
            case "/模板":
            case "/模板信息":
                if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                break;

            case "/colour":
            case "/color":
            case "/cor":
            case "/цвет":
            case "/颜色":
            case "/更改颜色":
            case "/修改颜色":
            case "/换颜色":
                if (Options.PlayerCanSetColor.GetBool() || player.FriendCode.GetDevUser().IsDev || player.FriendCode.GetDevUser().ColorCmd || Utils.IsPlayerVIP(player.FriendCode))
                {
                    if (GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                        break;
                    }
                    subArgs = args.Length < 2 ? "" : args[1];
                    var color = Utils.MsgToColor(subArgs);
                    if (color == byte.MaxValue)
                    {
                        Utils.SendMessage(GetString("IllegalColor"), player.PlayerId);
                        break;
                    }
                    player.RpcSetColor(color);
                    Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), player.PlayerId);
                }
                else
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                }
                break;

            case "/quit":
            case "/qt":
            case "/sair":
            case "/退出":
            case "/退":
                if (Options.PlayerCanUseQuitCommand.GetBool())
                {
                    subArgs = args.Length < 2 ? "" : args[1];
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
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                }
                break;

            case "/id":
            case "/айди":
            case "/编号":
            case "/玩家编号":
                if (TagManager.ReadPermission(player.FriendCode) < 2 && (Options.ApplyModeratorList.GetValue() == 0 || !Utils.IsPlayerModerator(player.FriendCode))
                    && !Options.EnableVoteCommand.GetBool()) break;

                string msgText = GetString("PlayerIdList");
                foreach (var pc in Main.EnumeratePlayerControls())
                {
                    if (pc == null) continue;
                    msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                }
                Utils.SendMessage(msgText, player.PlayerId);
                break;

            case "/mid":
            case "/玩家列表":
            case "/玩家信息":
            case "/玩家编号列表":
                //canceled = true;
                var tagCanUse = TagManager.ReadPermission(player.FriendCode) >= 2;
                //checking if modlist on or not
                //checking if player is has necessary privellege or not
                if (!tagCanUse && !Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("midCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!tagCanUse && Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("midCommandDisabled"), player.PlayerId);
                    break;
                }
                string msgText1 = GetString("PlayerIdList");
                foreach (var pc in Main.EnumeratePlayerControls())
                {
                    if (pc == null) continue;
                    msgText1 += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                }
                Utils.SendMessage(msgText1, player.PlayerId);
                break;

            case "/ban":
            case "/banir":
            case "/бан":
            case "/забанить":
            case "/封禁":
                //canceled = true;
                var tagCanBan = TagManager.ReadPermission(player.FriendCode) >= 5;
                // Check if the ban command is enabled in the settings
                if (!tagCanBan && Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("BanCommandDisabled"), player.PlayerId);
                    break;
                }

                // Check if the player has the necessary privileges to use the command
                if (!tagCanBan && !Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev)
                {
                    Utils.SendMessage(GetString("BanCommandNoAccess"), player.PlayerId);
                    break;
                }
                string banReason;
                if (args.Length < 3)
                {
                    Utils.SendMessage(GetString("BanCommandNoReason"), player.PlayerId);
                    break;
                }
                else
                {
                    subArgs = args[1];
                    banReason = string.Join(" ", args.Skip(2));
                }
                //subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte banPlayerId))
                {
                    Utils.SendMessage(GetString("BanCommandInvalidID"), player.PlayerId);
                    break;
                }

                if (banPlayerId == 0)
                {
                    Utils.SendMessage(GetString("BanCommandBanHost"), player.PlayerId);
                    break;
                }

                var bannedPlayer = Utils.GetPlayerById(banPlayerId);
                if (bannedPlayer == null)
                {
                    Utils.SendMessage(GetString("BanCommandInvalidID"), player.PlayerId);
                    break;
                }

                // Prevent moderators from banning other moderators
                if (Utils.IsPlayerModerator(bannedPlayer.FriendCode) || TagManager.ReadPermission(bannedPlayer.FriendCode) >= 5)
                {
                    Utils.SendMessage(GetString("BanCommandBanMod"), player.PlayerId);
                    break;
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
                //string moderatorName = player.GetRealName().ToString();
                //int startIndex = moderatorName.IndexOf("♥</color>") + "♥</color>".Length;
                //moderatorName = moderatorName.Substring(startIndex);
                //string extractedString = 
                string modLogname = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n1) ? n1 : "";
                string banlogname = Main.AllPlayerNames.TryGetValue(bannedPlayer.PlayerId, out var n11) ? n11 : "";
                string moderatorFriendCode = player.FriendCode.ToString();
                string bannedPlayerFriendCode = bannedPlayer.FriendCode.ToString();
                string bannedPlayerHashPuid = bannedPlayer.GetClient().GetHashedPuid();
                string logMessage = $"[{DateTime.Now}] {moderatorFriendCode},{modLogname} Banned: {bannedPlayerFriendCode},{bannedPlayerHashPuid},{banlogname} Reason: {banReason}";
                File.AppendAllText(modLogFiles, logMessage + Environment.NewLine);
                break;

            case "/warn":
            case "/aviso":
            case "/варн":
            case "/пред":
            case "/предупредить":
            case "/警告":
            case "/提醒":
                var tagCanWarn = TagManager.ReadPermission(player.FriendCode) >= 2;
                if (!tagCanWarn && Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("WarnCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!tagCanWarn && !Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev)
                {
                    Utils.SendMessage(GetString("WarnCommandNoAccess"), player.PlayerId);
                    break;
                }
                subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte warnPlayerId))
                {
                    Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId);
                    break;
                }
                if (warnPlayerId == 0)
                {
                    Utils.SendMessage(GetString("WarnCommandWarnHost"), player.PlayerId);
                    break;
                }

                var warnedPlayer = Utils.GetPlayerById(warnPlayerId);
                if (warnedPlayer == null)
                {
                    Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId);
                    break;
                }

                // Prevent moderators from warning other moderators
                if (Utils.IsPlayerModerator(warnedPlayer.FriendCode) || TagManager.ReadPermission(warnedPlayer.FriendCode) >= 2)
                {
                    Utils.SendMessage(GetString("WarnCommandWarnMod"), player.PlayerId);
                    break;
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
                //string moderatorName1 = player.GetRealName().ToString();
                //int startIndex1 = moderatorName1.IndexOf("♥</color>") + "♥</color>".Length;
                //moderatorName1 = moderatorName1.Substring(startIndex1);
                string modLogname1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n2) ? n2 : "";
                string warnlogname = Main.AllPlayerNames.TryGetValue(warnedPlayer.PlayerId, out var n12) ? n12 : "";
                string moderatorFriendCode1 = player.FriendCode.ToString();
                string warnedPlayerFriendCode = warnedPlayer.FriendCode.ToString();
                string warnedPlayerHashPuid = warnedPlayer.GetClient().GetHashedPuid();
                string logMessage1 = $"[{DateTime.Now}] {moderatorFriendCode1},{modLogname1} Warned: {warnedPlayerFriendCode},{warnedPlayerHashPuid},{warnlogname} Reason: {warnReason}";
                File.AppendAllText(modLogFiles, logMessage1 + Environment.NewLine);

                break;
            case "/kick":
            case "/expulsar":
            case "/кик":
            case "/кикнуть":
            case "/выгнать":
            case "/踢出":
            case "/踢":
                var tagCanKick = TagManager.ReadPermission(player.FriendCode) >= 4;
                // Check if the kick command is enabled in the settings
                if (!tagCanKick && Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("KickCommandDisabled"), player.PlayerId);
                    break;
                }

                // Check if the player has the necessary privileges to use the command
                if (!tagCanKick && !Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev)
                {
                    Utils.SendMessage(GetString("KickCommandNoAccess"), player.PlayerId);
                    break;
                }

                subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte kickPlayerId))
                {
                    Utils.SendMessage(GetString("KickCommandInvalidID"), player.PlayerId);
                    break;
                }

                if (kickPlayerId == 0)
                {
                    Utils.SendMessage(GetString("KickCommandKickHost"), player.PlayerId);
                    break;
                }

                var kickedPlayer = Utils.GetPlayerById(kickPlayerId);
                if (kickedPlayer == null)
                {
                    Utils.SendMessage(GetString("KickCommandInvalidID"), player.PlayerId);
                    break;
                }

                // Prevent moderators from kicking other moderators
                if (Utils.IsPlayerModerator(kickedPlayer.FriendCode) || TagManager.ReadPermission(kickedPlayer.FriendCode) >= 4)
                {
                    Utils.SendMessage(GetString("KickCommandKickMod"), player.PlayerId);
                    break;
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
                //string moderatorName2 = player.GetRealName().ToString();
                //int startIndex2 = moderatorName2.IndexOf("♥</color>") + "♥</color>".Length;
                //moderatorName2 = moderatorName2.Substring(startIndex2);
                string modLogname2 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n3) ? n3 : "";
                string kicklogname = Main.AllPlayerNames.TryGetValue(kickedPlayer.PlayerId, out var n13) ? n13 : "";

                string moderatorFriendCode2 = player.FriendCode.ToString();
                string kickedPlayerFriendCode = kickedPlayer.FriendCode.ToString();
                string kickedPlayerHashPuid = kickedPlayer.GetClient().GetHashedPuid();
                string logMessage2 = $"[{DateTime.Now}] {moderatorFriendCode2},{modLogname2} Kicked: {kickedPlayerFriendCode},{kickedPlayerHashPuid},{kicklogname} Reason: {kickReason}";
                File.AppendAllText(modLogFiles, logMessage2 + Environment.NewLine);

                break;
            case "/modcolor":
            case "/modcolour":
            case "/模组端颜色":
            case "/模组颜色":
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("ColorCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("ColorCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("ColorCommandNoLobby"), player.PlayerId);
                    break;
                }
                if (!Options.GradientTagsOpt.GetBool())
                {
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "modcolor");
                        Utils.SendMessage(GetString("ColorInvalidHexCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePath = $"{modTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePath))
                    {
                        Logger.Warn($"File Not exist, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                        File.Create(colorFilePath).Close();
                    }

                    File.WriteAllText(colorFilePath, $"{subArgs}");
                    break;
                }
                else
                {
                    subArgs = args.Length < 3 ? "" : args[1] + " " + args[2];
                    Regex regex = new(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
                    if (string.IsNullOrEmpty(subArgs) || !regex.IsMatch(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "modcolor");
                        Utils.SendMessage(GetString("ColorInvalidGradientCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePath = $"{modTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePath))
                    {
                        Logger.Msg($"File Not exist, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                        File.Create(colorFilePath).Close();
                    }
                    //Logger.Msg($"File exists, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                    //Logger.Msg($"{subArgs}","modcolor");
                    File.WriteAllText(colorFilePath, $"{subArgs}");
                    break;
                }
            case "/vipcolor":
            case "/vipcolour":
            case "/VIP玩家颜色":
            case "/VIP颜色":
                if (Options.ApplyVipList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("VipColorCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!Utils.IsPlayerVIP(player.FriendCode))
                {
                    Utils.SendMessage(GetString("VipColorCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("VipColorCommandNoLobby"), player.PlayerId);
                    break;
                }
                if (!Options.GradientTagsOpt.GetBool())
                {
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "vipcolor");
                        Utils.SendMessage(GetString("VipColorInvalidHexCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePathh = $"{vipTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePathh))
                    {
                        Logger.Warn($"File Not exist, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                        File.Create(colorFilePathh).Close();
                    }

                    File.WriteAllText(colorFilePathh, $"{subArgs}");
                    break;
                }
                else
                {
                    subArgs = args.Length < 3 ? "" : args[1] + " " + args[2];
                    Regex regexx = new(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
                    if (string.IsNullOrEmpty(subArgs) || !regexx.IsMatch(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "vipcolor");
                        Utils.SendMessage(GetString("VipColorInvalidGradientCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePathh = $"{vipTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePathh))
                    {
                        Logger.Msg($"File Not exist, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                        File.Create(colorFilePathh).Close();
                    }
                    //Logger.Msg($"File exists, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                    //Logger.Msg($"{subArgs}","modcolor");
                    File.WriteAllText(colorFilePathh, $"{subArgs}");
                    break;
                }
            case "/tagcolor":
            case "/tagcolour":
            case "/标签颜色":
            case "/附加名称颜色":
                string name1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n) ? n : "";
                if (name1 == "") break;
                if (!name1.Contains('\r') && player.FriendCode.GetDevUser().HasTag())
                {
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("ColorCommandNoLobby"), player.PlayerId);
                        break;
                    }
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "tagcolor");
                        Utils.SendMessage(GetString("TagColorInvalidHexCode"), player.PlayerId);
                        break;
                    }
                    string tagColorFilePath = $"{sponsorTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(tagColorFilePath))
                    {
                        Logger.Msg($"File Not exist, creating file at {tagColorFilePath}", "tagcolor");
                        File.Create(tagColorFilePath).Close();
                    }

                    File.WriteAllText(tagColorFilePath, $"{subArgs}");
                }
                break;

            case "/xf":
            case "/修复":
            case "/修":
                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                foreach (var pc in Main.EnumeratePlayerControls())
                {
                    if (pc.IsAlive()) continue;

                    pc.RpcSetNamePrivate(pc.GetRealName(isMeeting: true), player, true);
                }
                ChatUpdatePatch.DoBlockChat = false;
                //Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
                Utils.SendMessage(GetString("Message.TryFixName"), player.PlayerId);
                break;

            case "/tpout":
            case "/传送出":
            case "/传出":
                if (!GameStates.IsLobby) break;
                if (!Options.PlayerCanUseTP.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
                player.RpcTeleport(new Vector2(0.1f, 3.8f));
                break;
            case "/tpin":
            case "/传进":
            case "/传送进":
                if (!GameStates.IsLobby) break;
                if (!Options.PlayerCanUseTP.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }

                player.RpcTeleport(new Vector2(-0.2f, 1.3f));
                break;

            case "/vote":
            case "/投票":
            case "/票":
                subArgs = args.Length != 2 ? "" : args[1];
                if (subArgs == "" || !int.TryParse(subArgs, out int arg))
                    break;
                var plr = Utils.GetPlayerById(arg);

                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }


                if (!Options.EnableVoteCommand.GetBool())
                {
                    Utils.SendMessage(GetString("VoteDisabled"), player.PlayerId);
                    break;
                }
                if (Options.ShouldVoteCmdsSpamChat.GetBool())
                {
                    canceled = true;
                    ChatManager.SendPreviousMessagesToAll();
                }

                if (arg != 253) // skip
                {
                    if (plr == null || !plr.IsAlive())
                    {
                        Utils.SendMessage(GetString("VoteDead"), player.PlayerId);
                        break;
                    }
                }
                if (!player.IsAlive())
                {
                    Utils.SendMessage(GetString("CannotVoteWhenDead"), player.PlayerId);
                    break;
                }
                if (GameStates.IsMeeting)
                {
                    player.RpcCastVote((byte)arg);
                }
                break;

            case "/say":
            case "/s":
            case "/с":
            case "/сказать":
            case "/说":
                if (player.FriendCode.GetDevUser().IsDev)
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color={Main.ModColor}>{GetString("MessageFromDev")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                }
                else if (player.FriendCode.IsDevUser())
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#4bc9b0>{GetString("MessageFromSponsor")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                }
                else if (Utils.IsPlayerModerator(player.FriendCode) || TagManager.CanUseSayCommand(player.FriendCode))
                {
                    if (!TagManager.CanUseSayCommand(player.FriendCode) && (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowSayCommand.GetBool() == false))
                    {
                        Utils.SendMessage(GetString("SayCommandDisabled"), player.PlayerId);
                        break;
                    }
                    else
                    {
                        var modTitle = (Utils.IsPlayerModerator(player.FriendCode) || TagManager.ReadPermission(player.FriendCode) >= 2) ? $"<color=#8bbee0>{GetString("MessageFromModerator")}" : $"<color=#ffff00>{GetString("MessageFromVIP")}";
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"{modTitle} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                        //string moderatorName3 = player.GetRealName().ToString();
                        //int startIndex3 = moderatorName3.IndexOf("♥</color>") + "♥</color>".Length;
                        //moderatorName3 = moderatorName3.Substring(startIndex3);
                        string modLogname3 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n4) ? n4 : "";

                        string moderatorFriendCode3 = player.FriendCode.ToString();
                        string logMessage3 = $"[{DateTime.Now}] {moderatorFriendCode3},{modLogname3} used /s: {args.Skip(1).Join(delimiter: " ")}";
                        File.AppendAllText(modLogFiles, logMessage3 + Environment.NewLine);

                    }
                }
                break;
            case "/rps":
            case "/剪刀石头布":
                //canceled = true;
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
                subArgs = args.Length != 2 ? "" : args[1];

                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
                    break;
                }

                if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice))
                {
                    Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
                    break;
                }
                else if (playerChoice < 0 || playerChoice > 2)
                {
                    Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
                    break;
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
                    break;
                }
            case "/coinflip":
            case "/抛硬币":
                //canceled = true;
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }

                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("CoinflipCommandInfo"), player.PlayerId);
                    break;
                }
                else
                {
                    var rand = IRandom.Instance;
                    int botChoice = rand.Next(1, 101);
                    var coinSide = (botChoice < 51) ? GetString("Heads") : GetString("Tails");
                    Utils.SendMessage(string.Format(GetString("CoinFlipResult"), coinSide), player.PlayerId);
                    break;
                }
            case "/gno":
            case "/猜数字":
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
                //canceled = true;
                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId);
                    break;
                }
                subArgs = args.Length != 2 ? "" : args[1];
                if (subArgs == "" || !int.TryParse(subArgs, out int guessedNo))
                {
                    Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId);
                    break;
                }
                else if (guessedNo < 0 || guessedNo > 99)
                {
                    Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId);
                    break;
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
                        //targetNumber = Main.GuessNumber[player.PlayerId][0];
                        Utils.SendMessage(string.Format(GetString("GNoLost"), targetNumber), player.PlayerId);
                        break;
                    }
                    else if (guessedNo < targetNumber)
                    {
                        Utils.SendMessage(string.Format(GetString("GNoLow"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                        break;
                    }
                    else if (guessedNo > targetNumber)
                    {
                        Utils.SendMessage(string.Format(GetString("GNoHigh"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                        break;
                    }
                    else
                    {
                        Utils.SendMessage(string.Format(GetString("GNoWon"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                        Main.GuessNumber[player.PlayerId][0] = -1;
                        Main.GuessNumber[player.PlayerId][1] = 7;
                        break;
                    }
                }
            case "/rand":
            case "/XY数字":
            case "/范围游戏":
            case "/猜范围":
            case "/范围":
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
                subArgs = args.Length != 3 ? "" : args[1];
                subArgs2 = args.Length != 3 ? "" : args[2];

                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("RandCommandInfo"), player.PlayerId);
                    break;
                }
                if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice1) || subArgs2 == "" || !int.TryParse(subArgs2, out int playerChoice2))
                {
                    Utils.SendMessage(GetString("RandCommandInfo"), player.PlayerId);
                    break;
                }
                else
                {
                    var rand = IRandom.Instance;
                    int botResult = rand.Next(playerChoice1, playerChoice2 + 1);
                    Utils.SendMessage(string.Format(GetString("RandResult"), botResult), player.PlayerId);
                    break;
                }
            case "/8ball":
            case "/8号球":
            case "/幸运球":
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
                canceled = true;
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
                break;
            case "/me":
            case "/我的权限":
            case "/权限":

                string Devbox = player.FriendCode.GetDevUser().DeBug ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                string UpBox = player.FriendCode.GetDevUser().IsUp ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                string ColorBox = player.FriendCode.GetDevUser().ColorCmd ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";

                subArgs = text.Length == 3 ? string.Empty : text.Remove(0, 3);
                if (string.IsNullOrEmpty(subArgs))
                {
                    Utils.SendMessage((player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), player.PlayerId, player.GetRealName(clientData: true), player.GetClient().FriendCode, player.GetClient().GetHashedPuid(), player.FriendCode.GetDevUser().GetUserType(), Devbox, UpBox, ColorBox)}", player.PlayerId);
                }
                else
                {
                    var tagCanMe = TagManager.ReadPermission(player.FriendCode) >= 2;
                    if ((Options.ApplyModeratorList.GetValue() == 0 || !Utils.IsPlayerModerator(player.FriendCode)) && !tagCanMe && !player.FriendCode.GetDevUser().IsDev)
                    {
                        Utils.SendMessage(GetString("Message.MeCommandNoPermission"), player.PlayerId);
                        break;
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
                break;

            case "/start":
            case "/开始":
            case "/старт":
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }
                var tagCanStart = TagManager.ReadPermission(player.FriendCode) >= 3;
                if (!tagCanStart && !Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("StartCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!tagCanStart && (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowStartCommand.GetBool() == false))
                {
                    Utils.SendMessage(GetString("StartCommandDisabled"), player.PlayerId);
                    break;
                }
                if (GameStates.IsCountDown)
                {
                    Utils.SendMessage(GetString("StartCommandCountdown"), player.PlayerId);
                    break;
                }
                subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !int.TryParse(subArgs, out int countdown))
                {
                    countdown = 5;
                }
                else
                {
                    countdown = int.Parse(subArgs);
                }
                if (countdown < Options.StartCommandMinCountdown.CurrentValue || countdown > Options.StartCommandMaxCountdown.CurrentValue)
                {
                    Utils.SendMessage(string.Format(GetString("StartCommandInvalidCountdown"), Options.StartCommandMinCountdown.CurrentValue, Options.StartCommandMaxCountdown.CurrentValue), player.PlayerId);
                    break;
                }
                GameStartManager.Instance.BeginGame();
                GameStartManager.Instance.countDownTimer = countdown;
                Utils.SendMessage(string.Format(GetString("StartCommandStarted"), player.name));
                break;
            case "/end":
            case "/encerrar":
            case "/завершить":
            case "/结束":
            case "/结束游戏":
                if (!TagManager.CanUseEndCommand(player.FriendCode))
                {
                    Utils.SendMessage(GetString("EndCommandNoAccess"), player.PlayerId);
                    break;

                }
                Utils.SendMessage(string.Format(GetString("EndCommandEnded"), player.name));
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                GameManager.Instance.LogicFlow.CheckEndCriteria();
                break;
            case "/deck":
                DeckCommand(player, text, args);
                break;
            case "/ds":
            case "/draftstart":
                DraftStartCommand(player, text, args);
                break;
            case "/draft":
                DraftCommand(player, text, args);
                break;
            case "/dd":
            case "/draftdescription":
                DraftDescriptionCommand(player, text, args);
                break;
            case "/exe":
            case "/уничтожить":
            case "/повесить":
            case "/казнить":
            case "/казнь":
            case "/мут":
            case "/驱逐":
            case "/驱赶":
                if (!TagManager.CanUseExecuteCommand(player.FriendCode))
                {
                    Utils.SendMessage(GetString("ExecuteCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                var target = Utils.GetPlayerById(id);
                if (target != null)
                {
                    target.Data.IsDead = true;
                    target.SetDeathReason(PlayerState.DeathReason.etc);
                    target.SetRealKiller(player);
                    Main.PlayerStates[target.PlayerId].SetDead();
                    target.RpcExileV2();
                    MurderPlayerPatch.AfterPlayerDeathTasks(target, target, GameStates.IsMeeting);
                    Utils.SendMessage(string.Format(GetString("Message.ExecutedNonHost"), target.Data.PlayerName, player.Data.PlayerName));
                }
                break;

            case "/fix" 
            or "/blackscreenfix" 
            or "/fixblackscreen":
                FixCommand(player, text, args);
                break;

            case "/afkexempt":
                AFKExemptCommand(player, text, args);
                break;

            default:
                if (SpamManager.CheckSpam(player, text)) return;
                break;
        }
    }

    private static void DeckCommand(PlayerControl player, string text, string[] args)
    {
        if (GameStates.IsLobby)
        {
            Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
            return;
        }
        if (!Options.EnableGameTimeLimit.GetBool())
        {
            Utils.SendMessage(GetString("Message.GameTimeLimitDisabled"), player.PlayerId);
            return;            
        }
        Utils.SendMessage(string.Format(GetString("ShowGameTime"), (int)(Options.GameTimeLimit.GetFloat() - Main.GameTimer)), player.PlayerId);
    }

    private static void DraftStartCommand(PlayerControl player, string text, string[] args)
    {
        if (!GameStates.IsLobby)
        {
            Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
            return;
        }
        if (!player.IsHost() && !player.FriendCode.GetDevUser().IsDev && !Utils.IsPlayerModerator(player.FriendCode))
        {
            Utils.SendMessage(GetString("StartDraftNoAccess"), player.PlayerId);
            return;            
        }
        if (Options.CurrentGameMode != CustomGameMode.Standard)
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
    }

    private static void DraftCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int index)) return;
        if (!GameStates.IsLobby)
        {
            Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
            return;
        }
        DraftAssign.DraftedRoles(player, index);
    }

    private static void DraftDescriptionCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int index)) return;
        if (!GameStates.IsLobby)
        {
            Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
            return;
        }
        DraftAssign.DraftDescriptionRoles(player, index);
    }

    private static void FixCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            if (!Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev) return;
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
        if (!AmongUsClient.Instance.AmHost)
        {
            if (!Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev) return;
        }

        if (args.Length < 2 || !byte.TryParse(args[1], out byte afkId)) return;

        AFKDetector.ExemptedPlayers.Add(afkId);
        Utils.SendMessage("\n", player.PlayerId, string.Format(GetString("PlayerExemptedFromAFK"), afkId.GetPlayerName()));
    }

    private static void SpectateCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int index)) return;
        if (!GameStates.IsLobby)
        {
            Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
            return;
        }
        var pc = Utils.GetPlayerById((byte)index);
        if (!RoleAssign.SetRoles.ContainsKey((byte)index) || RoleAssign.SetRoles[(byte)index] != CustomRoles.GM)
        {
            RoleAssign.SetRoles[(byte)index] = CustomRoles.GM;
            Utils.SendMessage(GetString("PlayerJoinSpectateList"), player.PlayerId);
            if (pc.FriendCode.GetDevUser().IsDev) Utils.SendMessage(GetString("YouJoinSpectateList"), pc.PlayerId);
        }
        else
        {
            RoleAssign.SetRoles.Remove((byte)index);
            Utils.SendMessage(GetString("PlayerDeleteFromSpectateList"), player.PlayerId);
            if (pc.FriendCode.GetDevUser().IsDev) Utils.SendMessage(GetString("YouDeleteFromSpectateList"), pc.PlayerId);
        }
    }

    private static bool ImpostorChannel(PlayerControl pc, string msg, bool check = true)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null) return false;
        if (!pc.IsPlayerImpostorTeam() || !pc.GetCustomRole().IsImpostor()) return false;
        if (!Options.EnableImpostorChannel.GetBool()) return false;
        if (!pc.IsAlive()) return false;
        msg = msg.ToLower().Trim();
        if (check)
        {
            if (!GuessManager.CheckCommond(ref msg, "imp|伪装者", false)) return false;
        }

        if (string.IsNullOrEmpty(msg)) return false;

        if (CustomRoles.Narc.RoleExist(true))
        {
            Utils.SendMessage(GetString("NarcInterference"), pc.PlayerId, noReplay: true);
            return true;
        }

        Main.EnumerateAlivePlayerControls().Where(x => x.IsPlayerImpostorTeam() && x.GetCustomRole().IsImpostor())
            .Do(x => Utils.SendMessage(msg, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.ImpostorTONE), $"{GetString("MessageFromImpostor")} ~ <size=1.25>{pc.GetRealName(clientData: true)}</size>"), sendTo: x.PlayerId, noReplay: true));

        return true;
    }
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatUpdatePatch
{
    public static bool DoBlockChat = false;
    public static ChatController Instance;
    public static bool TempReviveHostRunning = false;
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
        if ((GameStates.IsInGame || player.Data.IsDead) && !Main.CurrentServerIsVanilla)
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

        if (player.AmOwner && !player.IsAlive() && !TempReviveHostRunning)
        {
            player.Data.IsDead = false;
            player.Data.SendGameData();
            TempReviveHostRunning = true;

            _ = new LateTask(() =>
            {
                if (!GameStates.IsEnded && !GameStates.IsLobby)
                {
                    player.Data.IsDead = true;
                    player.Data.SendGameData();
                }
                TempReviveHostRunning = false;
            }, 1f);
        }

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
