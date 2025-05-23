using AmongUs.Data;
using Discord;
using InnerNet;
using System;

namespace TOHE.Patches
{
    // Originally from Town of Us Rewritten, by Det
    [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
    public class DiscordRPC
    {
        private static string lobbycode = "";
        private static string region = "";
        public static void Prefix([HarmonyArgument(0)] Activity activity)
        {
            if (activity == null) return;

            var details = $"TONE v{Main.PluginDisplayVersion}";
            activity.Details = details;

            try
            {
                if (activity.State != "In Menus")
                {
                    if (!DataManager.Settings.Gameplay.StreamerMode)
                    {
                        int maxSize = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers;
                        if (GameStates.IsLobby)
                        {
                            lobbycode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                            region = Utils.GetRegionName();
                        }

                        if (lobbycode != "" && region != "")
                        {
                            if (GameStates.IsNormalGame)
                                details = $"TONE - {lobbycode} ({region})";

                            else if (GameStates.IsHideNSeek)
                                details = $"TONE Hide & Seek - {lobbycode} ({region})";
                        }

                        activity.Details = details;
                    }
                    else
                    {
                        if (GameStates.IsNormalGame)
                            details = $"TONE v{Main.PluginDisplayVersion}";

                        else if (GameStates.IsHideNSeek)
                            details = $"TONE v{Main.PluginDisplayVersion} - Hide & Seek";

                        else details = $"TONE v{Main.PluginDisplayVersion}";

                        activity.Details = details;
                    }
                }
            }

            catch (ArgumentException ex)
            {
                Logger.Error("Error in updating discord rpc", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                details = $"TONE v{Main.PluginDisplayVersion}";
                activity.Details = details;
            }
        }
    }
}