using AmongUs.Data;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static TOHE.Translator;

namespace TOHE;

public static class TemplateManager
{
#if ANDROID
    private static readonly string TEMPLATE_FILE_PATH = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "template.txt");
#else
    private static readonly string TEMPLATE_FILE_PATH = "./TONE-DATA/template.txt";
#endif

    private static readonly Dictionary<string, Func<string>> _replaceDictionaryNormalOptions = new()
    {
        ["RoomCode"] = () => InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId),
        ["HostName"] = () => DataManager.Player.Customization.Name,
        ["AmongUsVersion"] = () => UnityEngine.Application.version,
        ["InternalVersion"] = () => Main.PluginVersion,
        ["ModVersion"] = () => Main.PluginDisplayVersion,
        ["Map"] = () => Constants.MapNames[Main.NormalOptions.MapId],
        ["NumEmergencyMeetings"] = () => Main.NormalOptions.NumEmergencyMeetings.ToString(),
        ["EmergencyCooldown"] = () => Main.NormalOptions.EmergencyCooldown.ToString(),
        ["DiscussionTime"] = () => Main.NormalOptions.DiscussionTime.ToString(),
        ["VotingTime"] = () => Main.NormalOptions.VotingTime.ToString(),
        ["PlayerSpeedMod"] = () => Main.NormalOptions.PlayerSpeedMod.ToString(),
        ["CrewLightMod"] = () => Main.NormalOptions.CrewLightMod.ToString(),
        ["ImpostorLightMod"] = () => Main.NormalOptions.ImpostorLightMod.ToString(),
        ["KillCooldown"] = () => Main.NormalOptions.KillCooldown.ToString(),
        ["NumCommonTasks"] = () => Main.NormalOptions.NumCommonTasks.ToString(),
        ["NumLongTasks"] = () => Main.NormalOptions.NumLongTasks.ToString(),
        ["NumShortTasks"] = () => Main.NormalOptions.NumShortTasks.ToString(),
        ["Date"] = () => DateTime.Now.ToShortDateString(),
        ["Time"] = () => DateTime.Now.ToShortTimeString(),
        ["PlayerName"] = () => "",
        ["LobbyTimer"] = () =>
        {
            if (GameStates.IsLobby)
            {
                int timer = (int)GameStartManagerPatch.timer;
                int minutes = timer / 60;
                int seconds = timer % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
            else
            {
                return string.Empty;
            }
        }
    };

    private static readonly Dictionary<string, Func<string>> _replaceDictionaryHideNSeekOptions = new()
    {
        ["RoomCode"] = () => InnerNet.GameCode.IntToGameNameV2(AmongUsClient.Instance.GameId),
        ["HostName"] = () => DataManager.Player.Customization.Name,
        ["AmongUsVersion"] = () => UnityEngine.Application.version,
        ["InternalVersion"] = () => Main.PluginVersion,
        ["ModVersion"] = () => Main.PluginDisplayVersion,
        ["Map"] = () => Constants.MapNames[Main.HideNSeekOptions.MapId],
        ["PlayerSpeedMod"] = () => Main.HideNSeekOptions.PlayerSpeedMod.ToString(),
        ["Date"] = () => DateTime.Now.ToShortDateString(),
        ["Time"] = () => DateTime.Now.ToShortTimeString(),
        ["PlayerName"] = () => "",
        ["LobbyTimer"] = () =>
        {
            if (GameStates.IsLobby)
            {
                int timer = (int)GameStartManagerPatch.timer;
                int minutes = timer / 60;
                int seconds = timer % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
            else
            {
                return string.Empty;
            }
        }
    };

    public static void Init()
    {
        CreateIfNotExists();
    }

    public static void CreateIfNotExists()
    {

        try
        {
            string fileName;
            string name = CultureInfo.CurrentCulture.Name;
            if (name.Length >= 2)
                fileName = name switch // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
                {
                    var lang when lang.StartsWith("ru") => "Russian",
                    "zh-Hans" or "zh" or "zh-CN" or "zn-SG" => "SChinese",
                    "zh-Hant" or "zh-HK" or "zh-MO" or "zh-TW" => "TChinese",
                    "pt-BR" => "Brazilian",
                    "es-419" => "Latam",
                    var lang when lang.StartsWith("es") => "Spanish",
                    var lang when lang.StartsWith("fr") => "French",
                    "ja" or "ja-JP" => "Japanese",
                    var lang when lang.StartsWith("nl") => "Dutch",
                    var lang when lang.StartsWith("it") => "Italian",
                    _ => "English"
                };
            else fileName = "English";

#if ANDROID
            string dataDirectory = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA");
            string defaultTemplatePath = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "Default_Teamplate.txt");
#else
        string dataDirectory = @"TONE-DATA";
        string defaultTemplatePath = @"./TONE-DATA/Default_Teamplate.txt";
#endif

            if (!Directory.Exists(dataDirectory)) Directory.CreateDirectory(dataDirectory);
            var defaultTemplateMsg = GetResourcesTxt($"TOHE.Resources.Config.template.{fileName}.txt");

            if (!File.Exists(defaultTemplatePath))
            {
                Logger.Warn("Creating Default_Template.txt", "TemplateManager");
                using FileStream fs = File.Create(defaultTemplatePath);
            }
            File.WriteAllText(defaultTemplatePath, defaultTemplateMsg);

            if (!File.Exists(TEMPLATE_FILE_PATH))
            {
                if (File.Exists(@"./template.txt")) File.Move(@"./template.txt", TEMPLATE_FILE_PATH);
                else
                {
                    Logger.Warn($"Creating a new Template file from: {fileName}", "TemplateManager");
                    File.WriteAllText(TEMPLATE_FILE_PATH, defaultTemplateMsg);
                }
            }
            else
            {
                var text = File.ReadAllText(TEMPLATE_FILE_PATH, Encoding.GetEncoding("UTF-8"));
                File.WriteAllText(TEMPLATE_FILE_PATH, text.Replace("5PNwUaN5", "hkk2p9ggv4"));
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "TemplateManager");
        }
    }

    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static void SendTemplate(string str = "", byte playerId = 0xff, bool noErr = false)
    {
        CreateIfNotExists();
        using StreamReader sr = new(TEMPLATE_FILE_PATH, Encoding.GetEncoding("UTF-8"));
        string text;
        string[] tmp = [];
        List<string> sendList = [];
        HashSet<string> tags = [];
        Func<string> playerName = () => "";
        if (playerId != 0xff)
        {
            playerName = () => Main.AllPlayerNames[playerId];
        }

        _replaceDictionaryNormalOptions["PlayerName"] = playerName;
        _replaceDictionaryHideNSeekOptions["PlayerName"] = playerName;

        while ((text = sr.ReadLine()) != null)
        {
            tmp = text.Split(":");
            if (tmp.Length > 1 && tmp[1] != "")
            {
                tags.Add(tmp[0]);
                if (tmp[0].ToLower() == str.ToLower()) sendList.Add(tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n"));
            }
        }
        if (sendList.Count == 0 && !noErr)
        {
            if (playerId == 0xff)
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, string.Format(GetString("Message.TemplateNotFoundHost"), str, tags.Join(delimiter: ", ")));
            else Utils.SendMessage(string.Format(GetString("Message.TemplateNotFoundClient"), str), playerId, noReplay: true);
        }
        else for (int i = 0; i < sendList.Count; i++)
            {
                if (str == "welcome" && playerId != 0xff)
                {
                    var player = Utils.GetPlayerById(playerId);
                    if (player == null) continue;
                    Utils.SendMessage(ApplyReplaceDictionary(sendList[i]), playerId, string.Format($"<color=#aaaaff>{GetString("OnPlayerJoinMsgTitle")}</color>", Utils.ColorString(Palette.PlayerColors.Length > player.cosmetics.ColorId ? Palette.PlayerColors[player.cosmetics.ColorId] : UnityEngine.Color.white, player.GetRealName())));
                }
                else Utils.SendMessage(ApplyReplaceDictionary(sendList[i]), playerId);
            }
    }

    private static string ApplyReplaceDictionary(string text)
    {
        try
        {
            if (GameStates.IsNormalGame)
            {
                foreach (var kvp in _replaceDictionaryNormalOptions.ToArray())
                {
                    text = Regex.Replace(text, "{{" + kvp.Key + "}}", kvp.Value.Invoke() ?? "", RegexOptions.IgnoreCase);
                }
            }
            else if (GameStates.IsHideNSeek)
            {
                foreach (var kvp in _replaceDictionaryHideNSeekOptions.ToArray())
                {
                    text = Regex.Replace(text, "{{" + kvp.Key + "}}", kvp.Value.Invoke() ?? "", RegexOptions.IgnoreCase);
                }
            }
            return text;
        }
        catch //(Exception ex)
        {
            //Logger.Exception(ex, "TemplateManager.ApplyReplaceDictionary");
            return text;
        }
    }
    private static string TryGetTitle(string Text, out bool Contains)
    {
        int start = Text.IndexOf("<title>");
        int end = Text.IndexOf("</title>");
        var contains = start != -1 && end != -1 && start < end;
        Contains = contains;
        string title = "";

        if (contains)
        {
            title = Text.Substring(start, end);
            title = title.Replace("<title>", "");
            title = title.Replace("</title>", "");

        }


        return title;
    }
}