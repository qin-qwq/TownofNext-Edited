using AmongUs.Data;
using System;
using TMPro;
using TONE.Patches;
using TONE.Roles.AddOns.Common;
using TONE.Roles.Crewmate;
using TONE.Roles.Neutral;
using UnityEngine;

namespace TONE;

// Code based off of https://github.com/TownOfNext/TownOfNext/blob/main/src/Patches/ChatControlPatch.cs
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
    public static float lastSwipeTime = 0f;

    [HarmonyPatch(nameof(ChatController.Awake)), HarmonyPostfix]
    public static void Awake_Postfix(ChatController __instance)
    {
        __instance.freeChatField.textArea.SetText("");
        __instance.freeChatField.textArea.AllowPaste = true;
        __instance.freeChatField.UpdateCharCount();
        if (SendTargetShower != null) return;
        SendTargetShower = Object.Instantiate(__instance.freeChatField.charCountText.gameObject, __instance.freeChatField.charCountText.transform.parent);
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
        if (GameStates.IsInGame && __instance.IsOpenOrOpening)
        {
            var notice = OperatingSystem.IsAndroid() ? Translator.GetString("SendTargetSwitchNoticeAndroid") : Translator.GetString("SendTargetSwitchNotice");
            text += "<size=75%>" + notice + "</size>";

            var shouldSwitch = false;

            if (Input.GetKeyDown(KeyCode.LeftControl))
                shouldSwitch = true;

            if (!shouldSwitch && Input.touchSupported && Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Moved && touch.deltaPosition.y > 5f)
                    {
                        if (Time.time - lastSwipeTime > 0.2f)
                        {
                            shouldSwitch = true;
                            lastSwipeTime = Time.time;
                        }
                        break;
                    }
                }
            }

            if (shouldSwitch)
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
                return localPlayer.Is(CustomRoles.Lovers) && Lovers.PrivateChat.GetBool();

            case SendTargets.Imp:
                return localPlayer.IsPlayerImpostorTeam() && localPlayer.GetCustomRole().IsImpostor() && Options.EnableImpostorChannel.GetBool();

            case SendTargets.Jackal:
                return (localPlayer.Is(CustomRoles.Jackal) || localPlayer.Is(CustomRoles.Sidekick) || localPlayer.Is(CustomRoles.Recruit)) && Jackal.EnableJackalChannel.GetBool();

            case SendTargets.Jailer:
                return (localPlayer.Is(CustomRoles.Jailer) || Jailer.IsTarget(localPlayer.PlayerId)) && Jailer.EnableJailerChannel.GetBool();

            default:
                return false;
        }
    }
    public static void GetChannel(PlayerControl __instance, string msg, SendTargets target)
    {
        switch (target)
        {
            case SendTargets.Lovers:
                Lovers.SendLoversChannelMsg(__instance, msg);
                break;
            case SendTargets.Imp:
                ChatCommands.SendImpostorChannelMsg(__instance, msg);
                break;
            case SendTargets.Jackal:
                Jackal.SendJackalChannelMsg(__instance, msg);
                break;
            case SendTargets.Jailer:
                Jailer.SendJailerChannelMsg(__instance, msg);
                break;
            default:
                Logger.Error($"Not exist {(int)target}", "RPC.SendChannelMsg");
                break;
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

            if (!TextBoxPatch.IsInvalidCommand)
            {
                __instance.freeChatField.textArea.compoText.Color(Color.white);
                __instance.freeChatField.textArea.outputText.color = Color.white;
            }

            // free chat
            __instance.freeChatField.background.color = backgroundColor;

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
            if (!TextBoxPatch.IsInvalidCommand)
            {
                __instance.freeChatField.textArea.outputText.color = Color.black;
            }
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

        if (Input.GetKeyDown(KeyCode.Tab)) TextBoxPatch.OnTabPress(__instance);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
        {
            TextBoxPatch.Pasting = true;
            __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + GUIUtility.systemCopyBuffer.Trim());
        }

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
