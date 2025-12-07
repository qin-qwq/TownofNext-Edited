using Hazel;
using TOHE.Modules.Rpc;
using UnityEngine;

namespace TOHE;

public static class NameNotifyManager
{
    public static readonly Dictionary<byte, List<(string Text, long TimeStamp)>> Notice = [];
    
    public static void Reset() => Notice.Clear();
    
    public static bool Notifying(this PlayerControl pc) => Notice.ContainsKey(pc.PlayerId) && Notice[pc.PlayerId].Any();
    
    public static void Notify(this PlayerControl pc, string text, float time = 5f, bool sendInLog = true, bool hasPriority = false)
    {
        if (!AmongUsClient.Instance.AmHost || pc == null) return;
        if (!GameStates.IsInTask) return;
        
        text = text.Trim();
        if (!text.Contains("<color=") && !text.Contains("</color>")) text = Utils.ColorString(Color.white, text);
        if (!text.Contains("<size=")) text = $"<size=1.9>{text}</size>";

        if (!Notice.ContainsKey(pc.PlayerId))
            Notice[pc.PlayerId] = new List<(string Text, long TimeStamp)>();
  
        if (hasPriority)
        {
            Notice[pc.PlayerId].Clear();
        }

        var existingIndex = Notice[pc.PlayerId].FindIndex(n => n.Text == text);
        if (existingIndex != -1)
        {
            var updatedNotification = (text, Utils.TimeStamp + (long)time);
            Notice[pc.PlayerId][existingIndex] = updatedNotification;
        }
        else
        {
            var newNotification = (text, Utils.TimeStamp + (long)time);
            Notice[pc.PlayerId].Add(newNotification);
        }
        
        //var newNotification = (text, Utils.TimeStamp + (long)time);
        //Notice[pc.PlayerId].Add(newNotification);

        SendRPC(pc.PlayerId);
        Utils.NotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);

        if (sendInLog) Logger.Info($"New name notify for {pc.GetNameWithRole().RemoveHtmlTags()}: {text} ({time}s)", "Name Notify");
    }
    
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask)
        {
            if (Notice.Any()) Notice.Clear();
            return;
        }

        if (Notice.ContainsKey(player.PlayerId))
        {
            var expiredNotifies = Notice[player.PlayerId].Where(n => n.TimeStamp < Utils.GetTimeStamp()).ToList();
            foreach (var notify in expiredNotifies)
            {
                Notice[player.PlayerId].Remove(notify);
            }
            
            if (!Notice[player.PlayerId].Any())
            {
                Notice.Remove(player.PlayerId);
                Utils.NotifyRoles(SpecifySeer: player, ForceLoop: false);
            }
            else
            {
                Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: player);
            }
        }
    }
    
    public static bool GetNameNotify(PlayerControl player, out string name)
    {
        name = string.Empty;
        if (!Notice.TryGetValue(player.PlayerId, out List<(string Text, long TimeStamp)> value) || !value.Any()) 
            return false;
        
        name = string.Join("\n", value.Select(n => n.Text));
        return true;
    }
    
    private static void SendRPC(byte playerId)
    {
        var player = playerId.GetPlayer();
        if (player == null || !AmongUsClient.Instance.AmHost || !player.IsNonHostModdedClient()) return;

        var playerNotifies = Notice.ContainsKey(playerId) ? Notice[playerId] : new List<(string Text, long TimeStamp)>();
        var message = new RpcSyncNameNotify(
            PlayerControl.LocalPlayer.NetId,
            playerId,
            playerNotifies.Any(),
            string.Join("\n", playerNotifies.Select(n => n.Text)),
            playerNotifies.Any() ? playerNotifies.Max(n => n.TimeStamp) - Utils.GetTimeStamp() : 0f);
        RpcUtils.LateSpecificSendMessage(message, player.OwnerId);
    }
    
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        Notice.Remove(PlayerId);
        long now = Utils.GetTimeStamp();
        
        if (reader.ReadBoolean())
        {
            var text = reader.ReadString();
            var timeLeft = reader.ReadSingle();
            
            var texts = text.Split('\n');
            var notifications = new List<(string Text, long TimeStamp)>();
            
            foreach (var t in texts)
            {
                if (!string.IsNullOrEmpty(t.Trim()))
                {
                    var existingIndex = notifications.FindIndex(n => n.Text == t.Trim());
                    if (existingIndex != -1)
                    {
                        var updatedNotification = (t.Trim(), Utils.GetTimeStamp() + (long)timeLeft);
                        notifications[existingIndex] = updatedNotification;
                    }
                    else
                    {
                        notifications.Add((t.Trim(), Utils.GetTimeStamp() + (long)timeLeft));
                    }
                }
            }
            
            if (notifications.Any())
            {
                Notice[PlayerId] = notifications;
                Logger.Info($"New name notify for {Main.AllPlayerNames[PlayerId]}: {text} ({timeLeft}s)", "Name Notify");
            }
        }
    }
}