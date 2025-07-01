using UnityEngine;
using HarmonyLib;
using InnerNet;

namespace TOHE;
// https://github.com/g0aty/SickoMenu/blob/main/hooks/LobbyBehaviour.cpp
[HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
public static class MoreLobbyInfo_GameContainer_SetupGameInfo_Postfix
{
    /// <summary>
    /// Show more information when finding a game:
    /// host name (e.g. Astral), lobby code (e.g. KLHCEG), host platform (e.g. Epic), and lobby age in minutes (e.g. 4:20)
    /// </summary>
    /// <param name="__instance">The <c>GameContainer</c> instance.</param>
    public static void Postfix(GameContainer __instance)
    {
        var trueHostName = __instance.gameListing.TrueHostName;

        // The Crewmate icon gets aligned properly with this
        const string separator = "<#0000>000000000000000</color>";

        var age = __instance.gameListing.Age;
        var lobbyTime = $"Age: {age / 60}:{(age % 60 < 10 ? "0" : "")}{age % 60}";

        var platformId = __instance.gameListing.Platform switch
        {
            Platforms.StandaloneEpicPC => "Epic",
            Platforms.StandaloneSteamPC => "Steam",
            Platforms.StandaloneMac => "Mac",
            Platforms.StandaloneWin10 => "Microsoft Store",
            Platforms.StandaloneItch => "Itch.io",
            Platforms.IPhone => "iPhone / iPad",
            Platforms.Android => "Android",
            Platforms.Switch => "Nintendo Switch",
            Platforms.Xbox => "Xbox",
            Platforms.Playstation => "PlayStation",
            _ => "Unknown"
        };
        // Set the text of the capacity field to include the new information
        __instance.capacity.text = $"<size=40%>{separator}\n{trueHostName}\n{__instance.capacity.text}\n" +
                                   $"<#fb0>{GameCode.IntToGameName(__instance.gameListing.GameId)}</color>\n" +
                                   $"<#b0f>{platformId}</color>\n{lobbyTime}\n{separator}</size>";
    }
}