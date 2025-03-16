using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;


// 部分代码参考：https://github.com/TOHOptimized/TownofHost-Optimized
// 贴图来源 : https://github.com/Dolly1016/Nebula-Public
internal class Fury : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Fury;
    private const int Id = 32000;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static OptionItem KillCooldown;
    private static OptionItem AngryCooldown;
    private static OptionItem AngryDuration;
    private static OptionItem AngryKillCooldown;
    private static OptionItem AngrySpeed;
    private static OptionItem AngryVision;
    private static OptionItem CanStartMeetingWhenAngry;

    private bool FuryAngry;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fury);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngryCooldown = FloatOptionItem.Create(Id + 11, "AngryCooldown", new(2.5f, 120f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngryDuration = FloatOptionItem.Create(Id + 12, "AngryDuration", new(2.5f, 60f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
                .SetValueFormat(OptionFormat.Seconds);
        AngryKillCooldown = FloatOptionItem.Create(Id + 13, "AngryKillCooldown", new(0f, 120f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngrySpeed = FloatOptionItem.Create(Id + 14, "AngrySpeed", new(0f, 3f, 0.25f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
        AngryVision = FloatOptionItem.Create(Id + 15, "AngryVision", new(0f, 5f, 0.05f), 0.25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
        CanStartMeetingWhenAngry = BooleanOptionItem.Create(Id + 16, "CanStartMeetingWhenAngry", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury]);
    }

    public override void Init()
    {
        FuryAngry = false;
    }

    public override bool OnCheckStartMeeting(PlayerControl reporter)
    {
        foreach (PlayerControl playerControl in Main.AllPlayerControls)
        {
            if (!CanStartMeetingWhenAngry.GetBool() && FuryAngry == true)
            {
                return false;
            }
            if (CanStartMeetingWhenAngry.GetBool() && FuryAngry == true)
            {
                return true;
            }
        }
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AngryCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
        if (FuryAngry)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, AngryVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, AngryVision.GetFloat());
        }
        else
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
    }
    public override void UnShapeShiftButton(PlayerControl player)
    {
        FuryAngry = true;
        player.SetKillCooldown(AngryKillCooldown.GetFloat());
        player.Notify(GetString("FuryInRage"), AngryDuration.GetFloat());
        foreach (var target in Main.AllPlayerControls)
        {
            target.KillFlash();
            RPC.PlaySoundRPC(player.PlayerId, Sounds.ImpTransform);
            target.Notify(GetString("SeerFuryInRage"), 5f);
        }
        player.MarkDirtySettings();
        var tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
        Main.AllPlayerSpeed[player.PlayerId] = AngrySpeed.GetFloat();
        var tmpKillCooldown = Main.AllPlayerKillCooldown[player.PlayerId];
        Main.AllPlayerKillCooldown[player.PlayerId] = AngryKillCooldown.GetFloat();

        _ = new LateTask(() =>
        {
            FuryAngry = false;
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - AngrySpeed.GetFloat() + tmpSpeed;
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] - AngryKillCooldown.GetFloat() + tmpKillCooldown;
            player.Notify(GetString("FuryInCalm"), 5f);
            player.MarkDirtySettings();
        }, AngryDuration.GetFloat());
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("FuryShapeshiftText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Rage");
}
