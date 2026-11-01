using System;
using UnityEngine;

namespace TONE;

[HarmonyPatch]
public class MainMenuButtonHoverAnimation
{
    private static readonly Dictionary<PassiveButton, MainMenuLiquidGlass> AllButtons = new();
    private static readonly HashSet<PassiveButton> PendingButtons = new();

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    private static void Start_Prefix()
    {
        foreach (var glass in AllButtons.Values)
            glass.Dispose();
        AllButtons.Clear();
        PendingButtons.Clear();
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Start_Postfix(MainMenuManager __instance)
    {
        RefreshButtons(GameObject.Find("Main Buttons"));
        RegisterButton(__instance.creditsButton);
        RegisterButton(__instance.howToPlayButton);
        if (MainMenuManagerStartPatch.ToneLogo != null)
            RefreshButtons(MainMenuManagerStartPatch.ToneLogo.gameObject);
    }

    public static void RefreshButtons(GameObject obj)
    {
        if (obj == null) return;
        foreach (var button in obj.GetComponentsInChildren<PassiveButton>(true))
            RegisterButton(button);
    }

    public static void RegisterButton(PassiveButton button)
    {
        if (button == null || AllButtons.ContainsKey(button)) return;
        var glass = MainMenuLiquidGlass.Create(button);
        if (glass == null) return;

        AllButtons.Add(button, glass);
        // Keep the game's selection, sound and controller listeners intact.
        button.OnMouseOver ??= new();
        button.OnMouseOut ??= new();
        button.OnClick ??= new();
        button.OnMouseOver.AddListener((Action)(() => glass.Hovered = true));
        button.OnMouseOut.AddListener((Action)(() => glass.Hovered = false));
        button.OnClick.AddListener((Action)glass.Press);
    }

    // Let callers finish changing the scale of dynamically created buttons first.
    public static void QueueButton(PassiveButton button) => PendingButtons.Add(button);

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
    private static void Update_Postfix()
    {
        foreach (var button in PendingButtons)
            RegisterButton(button);
        PendingButtons.Clear();

        var camera = Camera.main;
        bool useMouse = OperatingSystem.IsWindows();
        foreach (var entry in AllButtons)
        {
            var button = entry.Key;
            if (button == null) continue;
            entry.Value.Update(camera, useMouse);
        }
    }
}