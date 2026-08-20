using System;
using System.IO;
using System.Text;
using System.Text.Json;
using static TONE.AchievementBase;
using static TONE.Translator;

namespace TONE;

public static class AchievementManager
{
    private static readonly List<AchievementBase> AllAchievements = [];
    private static readonly Dictionary<int, AchievementBase> achievementsById = [];
    private static readonly Dictionary<CustomRoles, List<AchievementBase>> achievementsByRole = [];

    private static readonly HashSet<int> CompletedThisGame = [];

    private static Dictionary<int, AchievementSaveData> saveData = [];

    private static string SavePath => $"{Main.Path}/TONE-DATA/Achievements.json";

    // 加载
    public static void Load()
    {
        AllAchievements.Clear();
        achievementsById.Clear();
        achievementsByRole.Clear();
        CompletedThisGame.Clear();
        saveData = [];

        foreach (var type in GetAchievementTypes())
        {
            try
            {
                if (Activator.CreateInstance(type) is not AchievementBase achievement) continue;
                AllAchievements.Add(achievement);
                achievementsById[achievement.Id] = achievement;

                if (!achievementsByRole.TryGetValue(achievement.Role, out var list))
                    achievementsByRole[achievement.Role] = list = [];
                list.Add(achievement);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create achievement {type.Name}: {ex.Message}", "AchievementManager");
            }
        }

        // 读取本地存档
        try
        {
            if (File.Exists(SavePath))
                saveData = JsonSerializer.Deserialize<Dictionary<int, AchievementSaveData>>(File.ReadAllText(SavePath)) ?? [];
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load achievement save: {ex}", "AchievementManager");
        }

        foreach (var achievement in AllAchievements)
        {
            if (saveData.TryGetValue(achievement.Id, out var data))
                achievement.SetProgress(data.Progress, data.Completed);
        }
        Logger.Info($"Loaded {AllAchievements.Count} achievements", "AchievementManager");
    }

    /// <summary>
    /// 游戏结束时保存成就
    /// </summary>
    public static void Save()
    {
        try
        {
            foreach (var achievement in AllAchievements)
                saveData[achievement.Id] = new AchievementSaveData { Progress = achievement.Progress, Completed = achievement.IsCompleted };
            File.WriteAllText(SavePath, JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save achievements: {ex}", "AchievementManager");
        }
    }

    /// <summary>
    /// 游戏开始
    /// </summary>
    public static void OnGameStart()
    {
        CompletedThisGame.Clear();
        foreach (var achievement in AllAchievements)
        {
            achievement.CompletedThisGame = false;
            if (!achievement.IsCompleted) achievement.OnGameStart();
        }
    }

    /// <summary>
    /// 成功击杀
    /// </summary>
    public static void OnPlayerKilled(PlayerControl killer, PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost || !killer || !target) return;

        CustomRoles role = killer.GetCustomRole();
        DispatchKillEvent(role, killer, target);
        if (role != CustomRoles.NotAssigned)
            DispatchKillEvent(CustomRoles.NotAssigned, killer, target);
    }

    private static void DispatchKillEvent(CustomRoles role, PlayerControl killer, PlayerControl target)
    {
        if (!achievementsByRole.TryGetValue(role, out var list)) return;
        foreach (var achievement in list)
        {
            if (achievement.IsCompleted) continue;
            achievement.OnPlayerKilled(killer, target);
        }
    }

    /// <summary>
    /// 职业技能事件
    /// </summary>
    public static void OnRoleAbility(CustomRoles role, AchievementEventType type, PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !player) return;
        DispatchRoleAbilityEvent(role, type, player);
        if (role != CustomRoles.NotAssigned)
            DispatchRoleAbilityEvent(CustomRoles.NotAssigned, type, player);
    }

    private static void DispatchRoleAbilityEvent(CustomRoles role, AchievementEventType type, PlayerControl player)
    {
        if (!achievementsByRole.TryGetValue(role, out var list)) return;
        foreach (var achievement in list)
        {
            if (achievement.IsCompleted) continue;
            achievement.OnRoleAbility(type, player);
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public static void OnGameEnd()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var local = PlayerControl.LocalPlayer;
        var isWinner = local && (CustomWinnerHolder.WinnerIds.Contains(local.PlayerId) || CustomWinnerHolder.WinnerRoles.Contains(local.GetCustomRole()));

        foreach (var achievement in AllAchievements)
        {
            if (!achievement.SaveProgress && !achievement.IsCompleted) achievement.ResetProgress();
            if (achievement.IsCompleted) continue;
            achievement.OnGameEnd(isWinner);
        }

        Save();
    }

    /// <summary>
    /// 成就完成
    /// </summary>
    public static void OnAchievementCompleted(AchievementBase achievement)
    {
        if (achievement == null || achievement.CompletedThisGame) return;
        achievement.CompletedThisGame = true;
        CompletedThisGame.Add(achievement.Id);
    }

    /// <summary>
    /// 展示成就列表
    /// </summary>
    public static void ShowAchievements(byte playerId, string roleName = null)
    {
        List<AchievementBase> list = AllAchievements;

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            if (!ChatCommands.GetRoleByName(roleName, out var role))
            {
                Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), playerId);
                return;
            }
            list = achievementsByRole.TryGetValue(role, out List<AchievementBase> roleList) ? roleList : [];
        }

        List<AchievementBase> completed = [.. list.Where(a => a.IsCompleted)];
        List<AchievementBase> incomplete = [.. list.Where(a => !a.IsCompleted)];

        var sc = new StringBuilder();
        for (var i = 0; i < incomplete.Count; i++)
        {
            var a = incomplete[i];
            sc.Append($"<size=70%><b>{a.Title}</b> - {a.Description}</size> {a.GetProgressText()} ({GetRoleLabel(a)})");
            if (i < incomplete.Count - 1)
                sc.AppendLine();
        }

        Utils.SendMessage(sc.ToString(), playerId, GetString("Achievement.Incomplete"));

        var sb = new StringBuilder();
        for (var i = 0; i < completed.Count; i++)
        {
            var a = completed[i];
            sb.Append($"<size=70%><b>{a.Title}</b> - {a.Description}</size> ({GetRoleLabel(a)})");
            if (i < completed.Count - 1)
                sb.AppendLine();
        }

        Utils.SendMessage(sb.ToString(), playerId, GetString("Achievement.Completed") + $" <#ffc0cb>(<#00ffa5>{completed.Count}</color>/{list.Count})</color>");
    }

    /// <summary>
    /// 结算显示本局完成的成就
    /// </summary>
    public static void ShowCompletedThisGame()
    {
        if (CompletedThisGame.Count == 0)
        {
            return;
        }

        foreach (var id in CompletedThisGame)
        {
            if (!achievementsById.TryGetValue(id, out var a)) continue;
            var sb = new StringBuilder();
            sb.Append($"<b>{a.Title}</b>\n{a.Description}");
            Utils.SendMessage(sb.ToString(), 0, GetString("Achievement.Completed"));
        }
    }

    private static string GetRoleLabel(AchievementBase a)
        => a.Role == CustomRoles.NotAssigned ? GetString("Achievement.All") : Utils.ColorString(Utils.GetRoleColor(a.Role), Utils.GetRoleName(a.Role));

    // 反射
    private static List<Type> GetAchievementTypes()
    {
        var result = new List<Type>();
        try
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsAbstract && typeof(AchievementBase).IsAssignableFrom(type))
                    result.Add(type);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // 加载失败，跳过能加载的部分
            foreach (var type in ex.Types)
            {
                if (type != null && !type.IsAbstract && typeof(AchievementBase).IsAssignableFrom(type))
                    result.Add(type);
            }
            Logger.Error($"Partial failure scanning achievement types: {ex.Message}", "AchievementManager");
        }
        return result;
    }
}
public class AchievementSaveData
{
    public int Progress { get; set; }
    public bool Completed { get; set; }
}