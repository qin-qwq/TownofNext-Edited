namespace TONE;

// 沉默是金：作为勒索者勒索 1 名玩家
public sealed class BlackmailPerGame : AchievementBase
{
    public override int Id => 50;
    public override string Name => "BlackmailPerGame";
    public override CustomRoles Role => CustomRoles.Blackmailer;
    public override int TargetProgress => 1;

    public override void OnRoleAbility(AchievementEventType type, PlayerControl player)
    {
        if (type == AchievementEventType.BlackmailerBlackmailed && IsLocalPlayer(player))
            AddProgress();
    }
}

// 悬赏猎手：作为赏金猎人一局内连续击杀 3 名悬赏目标
public sealed class BountyHunterStreak : AchievementBase
{
    public override int Id => 31;
    public override string Name => "BountyHunterStreak";
    public override CustomRoles Role => CustomRoles.BountyHunter;
    public override int TargetProgress => 3;
    public override bool SaveProgress => false;

    public override void OnRoleAbility(AchievementEventType type, PlayerControl player)
    {
        if (!IsLocalPlayer(player)) return;

        if (type == AchievementEventType.BountyHunterKilledTarget)
            AddProgress();
        else if (type == AchievementEventType.BountyHunterKilledNonTarget)
            SetProgress(0, false);
    }
}

// 扫大街：作为清理工清理 3 个尸体
public sealed class CleanerCleanBodies : AchievementBase
{
    public override int Id => 21;
    public override string Name => "CleanerCleanBodies";
    public override CustomRoles Role => CustomRoles.Cleaner;
    public override int TargetProgress => 3;

    public override void OnRoleAbility(AchievementEventType type, PlayerControl player)
    {
        if (type == AchievementEventType.CleanerCleanedBody && IsLocalPlayer(player))
            AddProgress();
    }
}

// 初试身手：作为警长走火 1 次
public sealed class SheriffMisfire : AchievementBase
{
    public override int Id => 0;
    public override string Name => "SheriffMisfire";
    public override CustomRoles Role => CustomRoles.Sheriff;
    public override int TargetProgress => 1;

    public override void OnRoleAbility(AchievementEventType type, PlayerControl player)
    {
        if (type == AchievementEventType.SheriffMisfire && IsLocalPlayer(player))
            AddProgress();
    }
}

// 正义裁决：作为警长执法 3 名敌人
public sealed class SheriffJustice : AchievementBase
{
    public override int Id => 1;
    public override string Name => "SheriffJustice";
    public override CustomRoles Role => CustomRoles.Sheriff;
    public override int TargetProgress => 3;

    public override void OnPlayerKilled(PlayerControl killer, PlayerControl target)
    {
        if (IsMyRole(killer) && !target.IsPlayerCrewmateTeam() && IsLocalPlayer(killer))
            AddProgress();
    }
}

// 熊熊燃烧：作为纵火犯获胜 1 次
public sealed class ArsonistWin : AchievementBase
{
    public override int Id => 10;
    public override string Name => "ArsonistWin";
    public override CustomRoles Role => CustomRoles.Arsonist;
    public override int TargetProgress => 1;

    public override void OnGameEnd(bool isWinner)
    {
        if (isWinner && Main.PlayerStates.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var state)
            && state.MainRole == Role)
            AddProgress();
    }
}

// 完美的恶作剧：作为小丑获胜 1 次
public sealed class JesterWin : AchievementBase
{
    public override int Id => 40;
    public override string Name => "JesterWin";
    public override CustomRoles Role => CustomRoles.Jester;
    public override int TargetProgress => 1;

    public override void OnGameEnd(bool isWinner)
    {
        if (isWinner && Main.PlayerStates.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var state)
            && state.MainRole == Role)
            AddProgress();
    }
}

// 贪吃蛇：作为鹈鹕一局内吞下 3 名玩家
public sealed class PelicanSnake : AchievementBase
{
    public override int Id => 60;
    public override string Name => "PelicanSnake";
    public override CustomRoles Role => CustomRoles.Pelican;
    public override int TargetProgress => 3;
    public override bool SaveProgress => false;

    public override void OnRoleAbility(AchievementEventType type, PlayerControl player)
    {
        if (type == AchievementEventType.PelicanEat && IsLocalPlayer(player))
            AddProgress();
    }
}

// 大胃王：作为鹈鹕吞下 6 名玩家
public sealed class PelicanBigEater : AchievementBase
{
    public override int Id => 61;
    public override string Name => "PelicanBigEater";
    public override CustomRoles Role => CustomRoles.Pelican;
    public override int TargetProgress => 6;

    public override void OnRoleAbility(AchievementEventType type, PlayerControl player)
    {
        if (type == AchievementEventType.PelicanEat && IsLocalPlayer(player))
            AddProgress();
    }
}

//#######################################
// 61 last id for achievements (Next use 70)
// Limit id for achievements --- Endless
//#######################################