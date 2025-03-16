using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE;

internal class Roaster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Roaster;
    private const int Id = 31100;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public static OptionItem NotifyRoasterAlive;
    private static OptionItem CakeCooldown;
    private static OptionItem CakeDuration;
    private static OptionItem CakeSpeed;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Roaster);
        NotifyRoasterAlive = BooleanOptionItem.Create(Id + 3, "NotifyRoasterAlive", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Roaster]);
        CakeCooldown = FloatOptionItem.Create(Id + 4, "CakeCooldown", new(0f, 60f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Roaster])
            .SetValueFormat(OptionFormat.Seconds);
        CakeDuration = FloatOptionItem.Create(Id + 5, "CakeDuration", new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Roaster])
            .SetValueFormat(OptionFormat.Seconds);
        CakeSpeed = FloatOptionItem.Create(Id + 6, "CakeSpeed", new(0f, 3f, 0.25f), 2.5f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Roaster])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override bool CanUseKillButton(PlayerControl pc)
    {
        return true;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CakeCooldown.GetFloat();

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (pc.IsAlive() && NotifyRoasterAlive.GetBool())
            AddMsg(Translator.GetString("RoasterNoticeAlive"), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Roaster), Translator.GetString("RoasterAliveTitle")));
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Roaster), GetString("SoldCake")));
        target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Roaster), GetString("GetCake")));
        killer.SetKillCooldown();
        var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
        Main.AllPlayerSpeed[target.PlayerId] = CakeSpeed.GetFloat();
        target.MarkDirtySettings();

         _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - CakeSpeed.GetFloat() + tmpSpeed;
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Roaster), GetString("CakeEaten")));
            target.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, CakeDuration.GetFloat());

        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("RoasterCakeButtonText"));
    }
}
