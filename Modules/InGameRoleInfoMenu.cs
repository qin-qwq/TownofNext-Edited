using System.Text;
using TMPro;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.Translator;

namespace TONE.Modules;

public static class InGameRoleInfoMenu
{
    public static bool Showing => Fill != null && Fill.active && Menu != null && Menu.active;

    public static GameObject Fill;
    public static SpriteRenderer FillSP => Fill.GetComponent<SpriteRenderer>();

    public static GameObject Menu;

    public static GameObject MainInfo;
    public static GameObject AddonsInfo;
    public static TextMeshPro MainInfoTMP => MainInfo.GetComponent<TextMeshPro>();
    public static TextMeshPro AddonsInfoTMP => AddonsInfo.GetComponent<TextMeshPro>();

    public static void Init()
    {
        var DOBScreen = AccountManager.Instance.transform.FindChild("DOBEnterScreen");

        Fill = new("TONE Role Info Menu Fill") { layer = 5 };
        Fill.transform.SetParent(HudManager.Instance.transform.parent, true);
        Fill.transform.localPosition = new(0f, 0f, -980f);
        Fill.transform.localScale = new(20f, 10f, 1f);
        Fill.AddComponent<SpriteRenderer>().sprite = DOBScreen.FindChild("Fill").GetComponent<SpriteRenderer>().sprite;
        FillSP.color = new(0f, 0f, 0f, 0.75f);

        Menu = Object.Instantiate(DOBScreen.FindChild("InfoPage").gameObject, HudManager.Instance.transform.parent);
        Menu.name = "TONE Role Info Menu Page";
        Menu.transform.SetLocalZ(-990f);

        Object.Destroy(Menu.transform.FindChild("Title Text").gameObject);
        Object.Destroy(Menu.transform.FindChild("BackButton").gameObject);
        Object.Destroy(Menu.transform.FindChild("EvenMoreInfo").gameObject);

        MainInfo = Menu.transform.FindChild("InfoText_TMP").gameObject;
        MainInfo.name = "Main Role Info";
        MainInfo.DestroyTranslator();
        MainInfo.transform.localPosition = new(-2.3f, 0.8f, 4f);
        MainInfo.GetComponent<RectTransform>().sizeDelta = new(4.5f, 10f);
        MainInfoTMP.alignment = TextAlignmentOptions.Left;
        MainInfoTMP.fontSize = 1.75f;

        AddonsInfo = Object.Instantiate(MainInfo, MainInfo.transform.parent);
        AddonsInfo.name = "Addons Info";
        AddonsInfo.DestroyTranslator();
        AddonsInfo.transform.SetLocalX(2.3f);
        AddonsInfo.transform.localScale = new(0.7f, 0.7f, 0.7f);

    }

    public static void SetRoleInfoRef(PlayerControl player)
    {
        if (player == null) return;

        if (!Fill || !Menu) Init();

        CustomRoles role = player.GetCustomRole();
        StringBuilder sb = new();
        StringBuilder titleSb = new();
        StringBuilder settings = new();
        StringBuilder addons = new();
        settings.Append("<size=75%>");
        titleSb.Append($"{role.ToColoredString()} {Utils.GetRoleMode(role)}");
        sb.Append("<size=90%>");
        sb.Append(player.GetRoleInfo(true).TrimStart());
        sb.Append("\n\n");
        if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt)) Utils.ShowChildrenSettings(opt, ref sb, command: false);

        settings.Append("</size>");
        if (settings.Length > 0) addons.Append($"{settings}\n\n");
        if (player.PetActivatedAbility()) sb.Append($"<size=50%>{GetString("SupportsPetMessage")}</size>");

        string searchStr = GetString(role.ToString());
        sb.Replace(searchStr, role.ToColoredString());
        sb.Replace(searchStr.ToLower(), role.ToColoredString());
        sb.Append("</size>");
        List<CustomRoles> subRoles = Main.PlayerStates[player.PlayerId].SubRoles;
        if (subRoles.Count > 0) addons.Append(GetString("YourAddon"));

        addons.Append("<size=75%>");

        subRoles.ForEach(subRole =>
        {
            addons.Append($"\n\n{subRole.ToColoredString()} {Utils.GetRoleMode(subRole)} {GetString($"{subRole}InfoLong")}");
            string searchSubStr = GetString(subRole.ToString());
            addons.Replace(searchSubStr, subRole.ToColoredString());
            addons.Replace(searchSubStr.ToLower(), subRole.ToColoredString());
        });

        addons.Append("</size>");

        sb.Insert(0, $"{titleSb}\n");

        MainInfoTMP.text = sb.ToString();
        AddonsInfoTMP.text = addons.ToString();
    }

    public static void Show()
    {
        if (!Fill || !Menu) Init();
        Fill?.SetActive(true);
        Menu?.SetActive(true);
    }
    public static void Hide()
    {
        Fill?.SetActive(false);
        Menu?.SetActive(false);
    }
}
