using System.IO;

namespace TONE;

public class DevUser(string code = "", string color = "null", string userType = "null", string tag = "null", bool isUp = false, bool isDev = false, bool deBug = false, bool colorCmd = false, bool nameCmd = false, string upName = "未认证用户")
{
    public string Code { get; set; } = code;
    public string Color { get; set; } = color;
    public string UserType { get; set; } = userType;
    public string Tag { get; set; } = tag;
    public bool IsUp { get; set; } = isUp;
    public bool IsDev { get; set; } = isDev;
    public bool DeBug { get; set; } = deBug;
    public bool ColorCmd { get; set; } = colorCmd;
    public bool NameCmd { get; set; } = nameCmd;
    public string UpName { get; set; } = upName;

    public bool HasTag() => Tag != "null";
    public string GetTag()
    {
#if ANDROID
        string tagColorFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "TONE-DATA", "Tags", "SPONSOR_TAGS", $"{Code}.txt");
#else
        string tagColorFilePath = @$"./TONE-DATA/Tags/SPONSOR_TAGS/{Code}.txt";

#endif
        if (Color == "null" || Color == string.Empty) return $"{Tag}";
        var startColor = Color.TrimStart('#');

        if (File.Exists(tagColorFilePath))
        {
            var ColorCode = File.ReadAllText(tagColorFilePath);
            if (Utils.CheckColorHex(ColorCode)) startColor = ColorCode;
        }
        string t1;
        t1 = Tag == "#Dev" ? Translator.GetString("Developer") : Tag;
        return $"<color=#{startColor}>{t1}</color>";
    }
}

public static class DevManager
{
    private readonly static DevUser DefaultDevUser = new();
    public readonly static List<DevUser> DevUserList = [];
    public static bool IsDevUser(this string code) => DevUserList.Any(x => x.Code == code);
    public static DevUser GetDevUser(this string code) => code.IsDevUser() ? DevUserList.Find(x => x.Code == code) : DefaultDevUser;
    public static string GetUserType(this DevUser user)
    {
        string rolename = "Crewmate";

        if (user.UserType != "null" && user.UserType != string.Empty)
        {
            switch (user.UserType)
            {
                case "s_cr":
                    rolename = "<color=#ff0000>Contributor</color>";
                    break;
                case "s_bo":
                    rolename = "<color=#7f00ff>Booster</color>";
                    break;
                case "s_tr":
                    rolename = "<color=#f46e6e>Tester</color>";
                    break;
                case "s_jc":
                    rolename = "<color=#f46e6e>Junior Contributor</color>";
                    break;

                default:
                    if (user.UserType.StartsWith("s_"))
                    {
                        rolename = "<color=#ffff00>Sponsor</color>";
                    }
                    else if (user.UserType.StartsWith("t_"))
                    {
                        rolename = "<color=#00ffff>Translator</color>";
                    }
                    break;
            }
        }

        return rolename;
    }

}
