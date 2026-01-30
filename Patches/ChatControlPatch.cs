using System;
using AmongUs.Data;
using TMPro;
using TONE.Roles.Crewmate;
using UnityEngine;

namespace TONE;

// Credit: TONX
[HarmonyPatch(typeof(ChatController))]
public static class SendTargetPatch
{
    public enum SendTargets
    {
        Default,
        Lovers,
        Imp,
        Jackal,
        Jailer
    }
    public static SendTargets SendTarget = SendTargets.Default;
    public static GameObject SendTargetShower;
    [HarmonyPatch(nameof(ChatController.Awake)), HarmonyPostfix]
    public static void Awake_Postfix(ChatController __instance)
    {
        __instance.freeChatField.textArea.SetText("");
        __instance.freeChatField.textArea.AllowPaste = true;
        __instance.freeChatField.UpdateCharCount();
        if (SendTargetShower != null) return;
        SendTargetShower = UnityEngine.Object.Instantiate(__instance.freeChatField.charCountText.gameObject, __instance.freeChatField.charCountText.transform.parent);
        SendTargetShower.name = "TONE Send Target Shower";
        SendTargetShower.transform.localPosition = new Vector3(1.95f, 0.5f, 0f);
        SendTargetShower.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 0.1f);
        var tmp = SendTargetShower.GetComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.outlineWidth = 1f;
    }
    [HarmonyPatch(nameof(ChatController.Update)), HarmonyPostfix]
    public static void Update_Postfix(ChatController __instance)
    {
        if (SendTargetShower == null) return;
        string text = Translator.GetString($"SendTargets.{Enum.GetName(SendTarget)}");
        if (GameStates.IsInGame && __instance.IsOpenOrOpening && AmongUsClient.Instance.AmHost)
        {
            text += "<size=75%>" + Translator.GetString("SendTargetSwitchNotice") + "</size>";
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                var enumLength = Enum.GetValues(typeof(SendTargets)).Length;
                var current = (int)SendTarget;
                var next = (current + 1) % enumLength;

                for (int i = 0; i < enumLength; i++)
                {
                    SendTargets candidate = (SendTargets)next;

                    if (CanSwitchToTarget(candidate))
                    {
                        SendTarget = candidate;
                        break;
                    }

                    next = (next + 1) % enumLength;
                }
            }
        }
        else SendTarget = SendTargets.Default;
        SendTargetShower?.GetComponent<TextMeshPro>()?.SetText(text);
        SendTargetShower?.SetActive(!SendTargetShower.transform.parent.parent.FindChild("RateMessage (TMP)").gameObject.activeSelf);
    }
    private static bool CanSwitchToTarget(SendTargets target)
    {
        if (target == SendTargets.Default)
            return true;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return false;

        switch (target)
        {
            case SendTargets.Lovers:
                return localPlayer.Is(CustomRoles.Lovers);

            case SendTargets.Imp:
                return localPlayer.IsPlayerImpostorTeam();

            case SendTargets.Jackal:
                return localPlayer.Is(CustomRoles.Jackal) || localPlayer.Is(CustomRoles.Sidekick) || localPlayer.Is(CustomRoles.Recruit);

            case SendTargets.Jailer:
                return localPlayer.Is(CustomRoles.Jailer) || Jailer.IsTarget(localPlayer.PlayerId);

            default:
                return false;
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatControllerUpdatePatch
{
    public static int CurrentHistorySelection = -1;

    private static SpriteRenderer QuickChatIcon;
    private static SpriteRenderer OpenBanMenuIcon;
    private static SpriteRenderer OpenKeyboardIcon;

    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost && DataManager.Settings.Multiplayer.ChatMode == InnerNet.QuickChatModes.QuickChatOnly)
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat;
    }
    public static void Postfix(ChatController __instance)
    {
        if (Main.DarkTheme.Value)
        {
            var backgroundColor = new Color32(40, 40, 40, byte.MaxValue);

            // free chat
            __instance.freeChatField.background.color = backgroundColor;
            __instance.freeChatField.textArea.compoText.Color(Color.white);
            __instance.freeChatField.textArea.outputText.color = Color.white;

            // quick chat
            __instance.quickChatField.background.color = backgroundColor;
            __instance.quickChatField.text.color = Color.white;

            if (QuickChatIcon == null)
                QuickChatIcon = GameObject.Find("QuickChatIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                QuickChatIcon.sprite = Utils.LoadSprite("TONE.Resources.Images.DarkQuickChat.png", 100f);

            if (OpenBanMenuIcon == null)
                OpenBanMenuIcon = GameObject.Find("OpenBanMenuIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenBanMenuIcon.sprite = Utils.LoadSprite("TONE.Resources.Images.DarkReport.png", 100f);

            if (OpenKeyboardIcon == null)
                OpenKeyboardIcon = GameObject.Find("OpenKeyboardIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenKeyboardIcon.sprite = Utils.LoadSprite("TONE.Resources.Images.DarkKeyboard.png", 100f);
        }
        else
        {
            __instance.freeChatField.textArea.outputText.color = Color.black;
        }

        if (SendTargetPatch.SendTarget != SendTargetPatch.SendTargets.Default)
        {
            var backgroundColor = new Color32(40, 40, 40, byte.MaxValue);
            __instance.freeChatField.textArea.outputText.color = Color.black;
            if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Lovers)
            {
                backgroundColor = Utils.GetRoleColor(CustomRoles.Lovers);
            }
            else if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Imp)
            {
                backgroundColor = Utils.GetRoleColor(CustomRoles.ImpostorTONE);
            }
            else if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Jackal)
            {
                backgroundColor = Utils.GetRoleColor(CustomRoles.Jackal);
            }
            else if (SendTargetPatch.SendTarget == SendTargetPatch.SendTargets.Jailer)
            {
                __instance.freeChatField.textArea.outputText.color = Color.white;
                backgroundColor = Utils.GetRoleColor(CustomRoles.Knight);
            }
            __instance.freeChatField.background.color = backgroundColor;
            __instance.quickChatField.background.color = backgroundColor;
        }

        if (!__instance.freeChatField.textArea.hasFocus) return;
        if (!GameStates.IsModHost) return;

        __instance.freeChatField.textArea.characterLimit = AmongUsClient.Instance.AmHost ? 2000 : 1200;


        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + GUIUtility.systemCopyBuffer);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
            __instance.freeChatField.textArea.SetText("");
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatCommands.ChatHistory.Any())
        {
            CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatCommands.ChatHistory.Count - 1);
            __instance.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && ChatCommands.ChatHistory.Any())
        {
            CurrentHistorySelection++;
            if (CurrentHistorySelection < ChatCommands.ChatHistory.Count)
                __instance.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
            else __instance.freeChatField.textArea.SetText("");
        }
    }
}
