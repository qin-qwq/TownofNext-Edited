using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class TimeAssassin : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TimeAssassin;
    private const int Id = 32200;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem TimeAssassinSkillCooldown;
    private static OptionItem TimeAssassinSkillDuration;

    public static bool TimeStop;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.TimeAssassin);
        TimeAssassinSkillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeAssassin])
            .SetValueFormat(OptionFormat.Seconds);
        TimeAssassinSkillDuration = FloatOptionItem.Create(Id + 11, GeneralOption.AbilityDuration, new(1f, 60f, 1f), 6f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeAssassin])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        TimeStop = false;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = TimeAssassinSkillCooldown.GetFloat();
    }

    public override bool OnCheckVanish(PlayerControl player)
    {
        if (TimeStop) return false;
        if (AnySabotageIsActive())
        {
            player.Notify(ColorString(GetRoleColor(CustomRoles.TimeAssassin), GetString("TimeStopError")));
            return false;
        }
        foreach (var target in Main.AllAlivePlayerControls.Where(x => !x.Is(CustomRoles.TimeAssassin)))
        {
            player.Notify(GetString("TimeStopStart"));
            TimeStop = true;
            Main.PlayerStates[target.PlayerId].IsBlackOut = true;
            var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            if (target.GetKillTimer() <= TimeAssassinSkillDuration.GetFloat()) target.SetKillCooldown(TimeAssassinSkillDuration.GetFloat());
            MarkEveryoneDirtySettings();
            _ = new LateTask(() =>
            {
                player.Notify(GetString("TimeStopEnd"));
                TimeStop = false;
                player.RpcResetAbilityCooldown();
                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                Main.PlayerStates[target.PlayerId].IsBlackOut = false;
                RPC.PlaySoundRPC(Sounds.TaskComplete, target.PlayerId);
                ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                MarkEveryoneDirtySettings();
            }, TimeAssassinSkillDuration.GetFloat(), "TimeAssassin Stop Time");
        }
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (TimeStop) TimeStop = false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("TimeAssassinShapeShifterButtonText");
    }
}
