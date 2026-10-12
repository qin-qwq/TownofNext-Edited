using AmongUs.GameOptions;
using TMPro;
using UnityEngine;

namespace TONE.Patches;

// https://github.com/tukasa0001/TownOfHost/blob/main/Patches/ActionButtonPatch.cs

[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
public static class SabotageButtonDoClickPatch
{
    public static bool Prefix()
    {
        if (!PlayerControl.LocalPlayer.inVent && GameManager.Instance.SabotagesEnabled())
        {
            DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
            {
                Mode = MapOptions.Modes.Sabotage
            });
        }

        return false;
    }
}

[HarmonyPatch(typeof(ActionButton), nameof(ActionButton.SetUsesRemaining))]
internal static class ActionButtonSetUsesRemainingPatch
{
    public static SpriteRenderer KillUsesRemainingSprite;

    public static bool Prefix(ActionButton __instance, int num)
    {
        if (__instance is not KillButton killButton) return true;

        if (!KillUsesRemainingSprite)
        {
            KillUsesRemainingSprite = Object.Instantiate(HudManager.Instance.AbilityButton.usesRemainingSprite, killButton.transform);
        }

        KillUsesRemainingSprite.gameObject.SetActive(true);
        var UsesRemainingText = KillUsesRemainingSprite.transform.GetChild(0).GetComponent<TextMeshPro>();
        UsesRemainingText.text = num.ToString();

        return false;
    }
}

[HarmonyPatch(typeof(ActionButton), nameof(ActionButton.SetInfiniteUses))]
internal static class ActionButtonSetInfiniteUsesPatch
{
    public static bool Prefix(ActionButton __instance)
    {
        if (__instance is not KillButton killButton) return true;

        if (ActionButtonSetUsesRemainingPatch.KillUsesRemainingSprite)
        {
            ActionButtonSetUsesRemainingPatch.KillUsesRemainingSprite.gameObject.SetActive(false);
        }

        return false;
    }
}

[HarmonyPatch(typeof(ActionButton), nameof(ActionButton.SetFillUp))]
internal static class ActionButtonSetFillUpPatch
{
    public static void Postfix(ActionButton __instance, [HarmonyArgument(0)] float timer)
    {
        if (__instance.isCoolingDown && timer is <= 90f and > 0f && !PlayerControl.LocalPlayer.shapeshifting)
        {
            RoleTypes roleType = PlayerControl.LocalPlayer.GetCustomRole().GetRoleTypes();

            bool usingAbility = roleType switch
            {
                RoleTypes.Engineer => PlayerControl.LocalPlayer.inVent,
                RoleTypes.Shapeshifter => Main.CheckShapeshift.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out bool shifted) && shifted,
                _ => false
            };

            Color color = usingAbility ? new Color32(255, 129, 166, 255) : Color.white;
            __instance.cooldownTimerText.text = Utils.ColorString(color, Mathf.CeilToInt(timer).ToString());
            __instance.cooldownTimerText.gameObject.SetActive(true);
        }
    }
}