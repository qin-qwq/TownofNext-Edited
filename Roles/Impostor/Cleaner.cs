using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Cleaner : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Cleaner;
    private const int Id = 3000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    public override Sprite ReportButtonSprite => CustomButton.Get("Clean");

    private static OptionItem KillCooldown;
    private static OptionItem KillCooldownAfterCleaning;
    private static OptionItem EnableAwakening;
    private static OptionItem ProgressPerKill;
    private static OptionItem ProgressPerSkill;
    private static OptionItem ProgressPerSecond;

    private static float AwakeningProgress;
    private static bool IsAwakened;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Cleaner);
        KillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownAfterCleaning = FloatOptionItem.Create(Id + 3, "KillCooldownAfterCleaning", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
        EnableAwakening = BooleanOptionItem.Create(Id + 12, "EnableAwakening", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Cleaner]);
        ProgressPerKill = FloatOptionItem.Create(Id + 13, "ProgressPerKill", new(0f, 100f, 10f), 40f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSkill = FloatOptionItem.Create(Id + 14, "ProgressPerSkill", new(0f, 100f, 10f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 15, "ProgressPerSecond", new(0.1f, 3f, 0.1f), 0.5f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void Init()
    {
        AwakeningProgress = 0;
        IsAwakened = false;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!EnableAwakening.GetBool() || AwakeningProgress >= 100 || GameStates.IsMeeting || isForMeeting) return string.Empty;
        return string.Format(GetString("AwakeningProgress") + ": {0:F0}% / {1:F0}%", AwakeningProgress, 100);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (AwakeningProgress < 100)
        {
            AwakeningProgress += ProgressPerSecond.GetFloat() * Time.fixedDeltaTime;
        }
        else CheckAwakening(player);
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!IsAwakened)
        {
            AwakeningProgress += ProgressPerKill.GetFloat();
        }
        return true;
    }

    private static void CheckAwakening(PlayerControl player)
    {
        if (AwakeningProgress >= 100 && !IsAwakened && EnableAwakening.GetBool() && player.IsAlive())
        {
            IsAwakened = true;
            player.Notify(GetString("SuccessfulAwakening"), 5f);
        }
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;

        if (reporter.Is(CustomRoles.Cleaner))
        {
            Main.UnreportableBodies.Add(deadBody.PlayerId);

            reporter.Notify(Translator.GetString("CleanerCleanBody"));
            if (!IsAwakened)
            {
                AwakeningProgress += ProgressPerSkill.GetFloat(); 
                reporter.SetKillCooldownV3(KillCooldownAfterCleaning.GetFloat(), forceAnime: true);
            }
            Logger.Info($"Cleaner: {reporter.GetRealName()} clear body: {deadBody.PlayerName}", "Cleaner");
            return false;
        }

        return true;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(Translator.GetString("CleanerReportButtonText"));
    }
}
