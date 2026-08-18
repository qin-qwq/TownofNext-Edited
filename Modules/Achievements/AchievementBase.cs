using System;
using static TONE.Translator;

namespace TONE;

public abstract class AchievementBase
{
    public abstract int Id { get; }

    public abstract string Name { get; }

    /// <summary>
    /// 成就名称
    /// </summary>
    public string Title => GetString($"Achievement.Title.{Name}");

    /// <summary>
    /// 成就描述
    /// </summary>
    public string Description => GetString($"Achievement.Desc.{Name}");

    /// <summary>
    /// 关联职业
    /// </summary>
    public abstract CustomRoles Role { get; }

    /// <summary>
    /// 达成所需进度
    /// </summary>
    public abstract int TargetProgress { get; }

    /// <summary>
    /// 当前进度
    /// </summary>
    public int Progress { get; private set; }

    /// <summary>
    /// 游戏结束时保存进度
    /// </summary>
    public virtual bool SaveProgress => true;

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted => Progress >= TargetProgress;

    /// <summary>
    /// 是否本局完成
    /// </summary>
    public bool CompletedThisGame { get; internal set; }

    /// <summary>
    /// 增加成就进度
    /// </summary>
    public void AddProgress(int amount = 1)
    {
        if (IsCompleted || amount <= 0 || GameStates.IsLocalGame || Options.NoGameEnd.GetBool()) return;
        Progress = Math.Min(Progress + amount, TargetProgress);
        if (IsCompleted) AchievementManager.OnAchievementCompleted(this);
    }

    /// <summary>
    /// 设置成就进度
    /// </summary>
    public void SetProgress(int progress, bool completed)
    {
        Progress = Math.Clamp(progress, 0, TargetProgress);
        if (completed) Progress = TargetProgress;
    }

    /// <summary>
    /// 重置成就进度
    /// </summary>
    public void ResetProgress() => Progress = 0;

    /// <summary>
    /// 判断是否为成就对应的关联职业
    /// </summary>
    protected bool IsMyRole(PlayerControl player) => player && player.GetCustomRole() == Role;

    /// <summary>
    /// 游戏开始时调用
    /// </summary>
    public virtual void OnGameStart()
    { }

    /// <summary>
    /// 成功击杀时调用
    /// </summary>
    public virtual void OnPlayerKilled(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// 游戏结束时调用
    /// </summary>
    public virtual void OnGameEnd(bool isWinner)
    { }

    /// <summary>
    /// 进度显示文本
    /// </summary>
    public virtual string GetProgressText() => $"({Progress}/{TargetProgress})";

    /// <summary>
    /// 职业技能事件
    /// </summary>
    public virtual void OnRoleAbility(AchievementEventType type, PlayerControl player)
    { }

    protected static bool IsLocalPlayer(PlayerControl player) => player && player.IsHost();

    /// <summary>
    /// 职业技能事件类型
    /// </summary>
    public enum AchievementEventType
    {
        BlackmailerBlackmailed, // 勒索者勒索目标
        BountyHunterKilledTarget, // 赏金猎人击杀悬赏目标
        BountyHunterKilledNonTarget, // 赏金猎人击杀非悬赏目标
        CleanerCleanedBody, // 清理工清理尸体
        SheriffMisfire, // 警长走火
        VeteranKilledAttacker, // 老兵警戒反杀
        PelicanEat, // 鹈鹕吞下
    }
}