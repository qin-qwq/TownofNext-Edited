using AmongUs.GameOptions;
using TONE.Patches;
using UnityEngine;

namespace TONE;

[HarmonyPatch(typeof(MatchInfoGuide), nameof(MatchInfoGuide.CreateNormalModeSettings))]
class MatchInfoGuideCreateNormalModeSettingsPatch
{
    public static bool Prefix(MatchInfoGuide __instance)
    {
        int num = 0;
        CreateImpSettingsEntry(__instance);
        SetMinMaxOptions(__instance, Options.NonNeutralKillingRolesMinPlayer, Options.NonNeutralKillingRolesMaxPlayer, "#7f8c8d", "NonNeutralKilling");
        SetMinMaxOptions(__instance, Options.NeutralKillingRolesMinPlayer, Options.NeutralKillingRolesMaxPlayer, "#7f8c8d", "NeutralKilling");
        SetMinMaxOptions(__instance, Options.NeutralApocalypseRolesMinPlayer, Options.NeutralApocalypseRolesMaxPlayer, "#ff174f", "NeutralApocalypse");
        SetMinMaxOptions(__instance, Options.CovenRolesMinPlayer, Options.CovenRolesMaxPlayer, "#ac42f2", "Coven");
        __instance.CreateSettingsEntry(StringNames.GameKillCooldown, GameManager.Instance.AllGameSettingData[StringNames.GameKillCooldown].GetValueString(Options.DefaultKillCooldown));
        __instance.CreateSettingsEntry(StringNames.GameEmergencyCooldown, GameManager.Instance.AllGameSettingData[StringNames.GameEmergencyCooldown].GetValueString(GameManager.Instance.LogicOptions.GetEmergencyCooldown()));
        __instance.CreateSettingsEntry(StringNames.GameVisualTasks, __instance.GetBoolString(GameManager.Instance.LogicOptions.GetVisualTasks()));
        __instance.CreateSettingsEntry(StringNames.GameAnonymousVotes, __instance.GetBoolString(GameManager.Instance.LogicOptions.GetAnonymousVotes()));
        __instance.CreateSettingsEntry(StringNames.GameConfirmImpostor, __instance.GetBoolString(Options.CEMode.GetInt() != 0));
        __instance.CreateSettingsEntry(StringNames.GameTaskBarMode, GameManager.Instance.LogicOptions.GetTaskBarMode().ToString());
        __instance.transform.FindChild("MatchInfoParent").FindChild("SettingsPanel").GetComponentInChildren<Scroller>().SetYBoundsMax(Mathf.Clamp(Mathf.Ceil(1.5f), 0.0f, 999f));
        foreach (var allRole in CustomRolesHelper.AllRoles)
        {
            if (!allRole.IsVanilla() && allRole.IsEnable() && !allRole.IsAdditionRole() && !allRole.OtherGameModesRole())
            {
                CreateTONERoleEntry(__instance, allRole);
                ++num;
            }
        }
        if (num == 0)
            __instance.rolesEnabledMessage.SetActive(true);
        __instance.MatchInfoRoleScroller.SetYBoundsMax(Mathf.Clamp(Mathf.Ceil(num / 2f) + __instance.RoleEntryBoundsModifier, 0.0f, 999f));
        __instance.MatchInfoRoleMaskArea.material.SetInt(PlayerMaterial.MaskLayer, 50);
        __instance.matchInfoSettingsMaskArea.material.SetInt(PlayerMaterial.MaskLayer, 50);
        __instance.CreatePlayerEntries();
        return false;
    }

    public static void CreateImpSettingsEntry(MatchInfoGuide __instance)
    {
        var variable = Options.UseVariableImp.GetBool();
        var min = Options.ImpRolesMinPlayer.GetInt();
        var max = Options.ImpRolesMaxPlayer.GetInt();
        var value = variable ? min == max ? min.ToString() : $"{min}-{max}" : Main.NormalOptions.NumImpostors.ToString();
        __instance.CreateSettingsEntry(StringNames.GameNumImpostors, value);
    }

    public static void SetMinMaxOptions(MatchInfoGuide __instance, OptionItem minOption, OptionItem maxOption, string color, string faction)
    {
        var min = minOption.GetInt();
        var max = maxOption.GetInt();
        var value = min == max ? max.ToString() : $"{min}-{max}";
        CreateFactionSettingsEntry(__instance, $"<color={color}>{Translator.GetString($"FactionSetting.{faction}")}</color>", value);
    }

    public static void CreateFactionSettingsEntry(MatchInfoGuide __instance, string settingName, string value)
    {
        var gameObject = Object.Instantiate(__instance.MatchInfoSettingPrefab, __instance.settingsScrollArea);
        var component = gameObject.GetComponent<MatchInfoGuideSettingLabel>();
        if (component)
            component.SetInfo(settingName, value);
        __instance.NormalModeSettings.Add(gameObject);
    }

    public static void CreateTONERoleEntry(MatchInfoGuide __instance, CustomRoles role)
    {
        var matchInfoRolePanel = Object.Instantiate(__instance.MatchInfoRolePanelPrefab, __instance.settingsTabs[2].GetComponent<Scroller>().Inner);
        matchInfoRolePanel.roleName.text = role.ToColoredString();
        matchInfoRolePanel.roleDescription.text = Translator.GetString($"{role}Info");
        matchInfoRolePanel.roleIcon.sprite = LobbyViewSettingsPanePatch.GetRoleIcon(role, true);
        matchInfoRolePanel.roleCount.text = $"{role.GetCount()} at {role.GetMode()}%";
        matchInfoRolePanel.roleIcon.material.SetInt(PlayerMaterial.MaskLayer, 50);
        matchInfoRolePanel.roleName.fontMaterial.SetFloat(matchInfoRolePanel.STENCIL_NAME, 50f);
        matchInfoRolePanel.roleDescription.fontMaterial.SetFloat(matchInfoRolePanel.STENCIL_NAME, 50f);
        matchInfoRolePanel.roleCount.fontMaterial.SetFloat(matchInfoRolePanel.STENCIL_NAME, 50f);
    }
}

[HarmonyPatch(typeof(MatchInfoGuide), nameof(MatchInfoGuide.CreateHnSModeSettings))]
class MatchInfoGuideCreateHnSModeSettingsPatch
{
    public static bool Prefix(MatchInfoGuide __instance)
    {
        __instance.CreatePlayerEntries();
        __instance.CreateSettingsEntry(StringNames.GameNumImpostors, Options.NumImpostorsHnS.GetInt().ToString());
        __instance.CreateHnSSettingsEntry(StringNames.MaxTimeInVent, GameManager.Instance.AllGameSettingData[StringNames.MaxTimeInVent].GetValueString(GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.CrewmateTimeInVent)));
        __instance.CreateHnSSettingsEntry(StringNames.FinalEscapeTime, GameManager.Instance.AllGameSettingData[StringNames.FinalEscapeTime].GetValueString(GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.FinalEscapeTime)));
        __instance.CreateHnSSettingsEntry(StringNames.SeekerFinalSpeed, GameManager.Instance.AllGameSettingData[StringNames.SeekerFinalSpeed].GetValueString(GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.SeekerFinalSpeed)));
        __instance.CreateHnSSettingsEntry(StringNames.SeekerFinalMap, __instance.GetBoolString(GameOptionsManager.Instance.CurrentGameOptions.GetBool(BoolOptionNames.SeekerFinalMap)));
        __instance.CreateHnSSettingsEntry(StringNames.SeekerPings, __instance.GetBoolString(GameOptionsManager.Instance.CurrentGameOptions.GetBool(BoolOptionNames.SeekerPings)));
        __instance.CreateHnSSettingsEntry(StringNames.MaxPingTime, GameManager.Instance.AllGameSettingData[StringNames.MaxPingTime].GetValueString(GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.MaxPingTime)));
        __instance.matchInfoSettingsMaskArea.material.SetInt(PlayerMaterial.MaskLayer, 50);
        __instance.TabButtons[2].gameObject.SetActive(false);
        return false;
    }
}

[HarmonyPatch(typeof(MatchInfoHudButton), nameof(MatchInfoHudButton.Update))]
class MatchInfoHudButtonUpdatePatch
{
    public static bool Prefix(MatchInfoHudButton __instance)
    {
        if (HudManager.Instance.Chat.isActiveAndEnabled)
            __instance.aspectPosition.DistanceFromEdge = MatchInfoHudButton.adjustedDistanceFromEdge;
        else
            __instance.aspectPosition.DistanceFromEdge = MatchInfoHudButton.defaultDistanceFromEdge;

        return false;
    }
}
