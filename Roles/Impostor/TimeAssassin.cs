using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class TimeAssassin : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TimeAssassin;
    private const int Id = 32200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.TimeAssassin);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem TimeAssassinSkillCooldown;
    private static OptionItem TimeAssassinSkillDuration;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.TimeAssassin);
        TimeAssassinSkillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeAssassin])
            .SetValueFormat(OptionFormat.Seconds);
        TimeAssassinSkillDuration = FloatOptionItem.Create(Id + 11, GeneralOption.AbilityDuration, new(1f, 60f, 1f), 6f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeAssassin])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = TimeAssassinSkillCooldown.GetFloat();
    }

    public override void UnShapeShiftButton(PlayerControl player)
    {
        foreach (var target in Main.AllAlivePlayerControls.Where(x => !x.Is(CustomRoles.TimeAssassin)))
        {
            player.Notify(GetString("TimeStopStart"));
            Main.PlayerStates[target.PlayerId].IsBlackOut = true;
            var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            target.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                player.Notify(GetString("TimeStopEnd"));
                player.RpcResetAbilityCooldown();
                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                Main.PlayerStates[target.PlayerId].IsBlackOut = false;
                RPC.PlaySoundRPC(Sounds.TaskComplete, target.PlayerId);
                ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                target.MarkDirtySettings();
            }, TimeAssassinSkillDuration.GetFloat(), "TimeAssassin Stop Time");
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("TimeAssassinShapeShifterButtonText");
    }
}
