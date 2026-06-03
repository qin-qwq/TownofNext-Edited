using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections;
using System.IO;
using System.Text.Json;
using UnityEngine.Networking;

namespace TONE;

// 参考：https://github.com/Yumenopai/TownOfHost_Y
[HarmonyPatch]
public class ModNews
{
    public int Number;
    public int BeforeNumber;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement
        {
            Number = Number,
            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Language = (uint)DataManager.Settings.Language.CurrentLanguage,
            Date = Date,
            Id = "ModNews"
        };

        return result;
    }
    public static List<ModNews> AllModNews = [];
    public static string ModNewsURL = "https://raw.githubusercontent.com/qin-qwq/TownofNext-Edited/refs/heads/main/Resources/Announcements/modNews-";
    static bool downloaded = false;
    public ModNews(int Number, string Title, string SubTitle, string ShortTitle, string Text, string Date)
    {
        this.Number = Number;
        this.Title = Title;
        this.SubTitle = SubTitle;
        this.ShortTitle = ShortTitle;
        this.Text = Text;
        this.Date = Date;
        AllModNews.Add(this);
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    public static void StartPostfix(MainMenuManager __instance)
    {
        static IEnumerator FetchBlacklist()
        {
            Logger.Info("Fetching Mod News from GitHub", "ModNews");
            if (downloaded)
            {
                yield break;
            }
            downloaded = true;
            ModNewsURL += TranslationController.Instance.currentLanguage.languageID switch
            {
                SupportedLangs.German => "de_DE.json",
                SupportedLangs.Latam => "es_419.json",
                SupportedLangs.Spanish => "es_ES.json",
                SupportedLangs.Filipino => "fil_PH.json",
                SupportedLangs.French => "fr_FR.json",
                SupportedLangs.Italian => "it_IT.json",
                SupportedLangs.Japanese => "ja_JP.json",
                SupportedLangs.Korean => "ko_KR.json",
                SupportedLangs.Dutch => "nl_NL.json",
                SupportedLangs.Brazilian => "pt_BR.json",
                SupportedLangs.Russian => "ru_RU.json",
                SupportedLangs.SChinese => "zh_CN.json",
                SupportedLangs.TChinese => "zh_TW.json",
                _ => "en_US.json", //English and any other unsupported language
            };
            var request = UnityWebRequest.Get(ModNewsURL);
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                downloaded = false;
                Logger.Error("ModNews Error Fetch:" + request.responseCode.ToString(), "ModNews");
                LoadModNewsFromResources();
                yield break;
            }

            try
            {
                using var jsonDocument = JsonDocument.Parse(request.downloadHandler.text);
                var newsArray = jsonDocument.RootElement.GetProperty("News");

                foreach (var newsElement in newsArray.EnumerateArray())
                {
                    var number = int.Parse(newsElement.GetProperty("Number").GetString());
                    var title = newsElement.GetProperty("Title").GetString();
                    var subTitle = newsElement.GetProperty("Subtitle").GetString();
                    var shortTitle = newsElement.GetProperty("Short").GetString();
                    var body = newsElement.GetProperty("Body").GetString();
                    var dateString = newsElement.GetProperty("Date").GetString();
                    // Create ModNews object
                    ModNews _ = new(number, title, subTitle, shortTitle, body, dateString);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "ModNews");
                Logger.Error("Failed to load mod info from github, load from local instead", "ModNews");
                // Use local Mod news instead
                LoadModNewsFromResources();
            }
        }
        __instance.StartCoroutine(FetchBlacklist().WrapToIl2Cpp());
    }

    private static void LoadModNewsFromResources()
    {
        string filename = TranslationController.Instance.currentLanguage.languageID switch
        {
            SupportedLangs.German => "de_DE.json",
            SupportedLangs.Latam => "es_419.json",
            SupportedLangs.Spanish => "es_ES.json",
            SupportedLangs.Filipino => "fil_PH.json",
            SupportedLangs.French => "fr_FR.json",
            SupportedLangs.Italian => "it_IT.json",
            SupportedLangs.Japanese => "ja_JP.json",
            SupportedLangs.Korean => "ko_KR.json",
            SupportedLangs.Dutch => "nl_NL.json",
            SupportedLangs.Brazilian => "pt_BR.json",
            SupportedLangs.Russian => "ru_RU.json",
            SupportedLangs.SChinese => "zh_CN.json",
            SupportedLangs.TChinese => "zh_TW.json",
            _ => "en_US.json", //English and any other unsupported language
        };

        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using Stream resourceStream = assembly.GetManifestResourceStream("TONE.Resources.Announcements.modNews-" + filename);
        using StreamReader reader = new(resourceStream);
        using var jsonDocument = JsonDocument.Parse(reader.ReadToEnd());
        var newsArray = jsonDocument.RootElement.GetProperty("News");

        foreach (var newsElement in newsArray.EnumerateArray())
        {
            var number = int.Parse(newsElement.GetProperty("Number").GetString());
            var title = newsElement.GetProperty("Title").GetString();
            var subTitle = newsElement.GetProperty("Subtitle").GetString();
            var shortTitle = newsElement.GetProperty("Short").GetString();
            var body = newsElement.GetProperty("Body").GetString();
            var dateString = newsElement.GetProperty("Date").GetString();
            // Create ModNews object
            ModNews _ = new(number, title, subTitle, shortTitle, body, dateString);
        }
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static void SetModAnnouncements_Prefix([HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        if (AllModNews.Count == 0)
        {
            Logger.Warn("AllModNews: 0", "ModNews");
            return;
        }

        List<Announcement> finalAllNews = AllModNews.ConvertAll(n => n.ToAnnouncement());
        finalAllNews.AddRange(aRange.Where(news => AllModNews.All(x => x.Number != news.Number)));
        finalAllNews.Sort((a1, a2) => DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)));

        aRange = new Il2CppReferenceArray<Announcement>(finalAllNews.Count);

        for (var i = 0; i < finalAllNews.Count; i++)
            aRange[i] = finalAllNews[i];
    }
}