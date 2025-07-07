using AmongUs.GameOptions;
using TOHE.Roles.Core;
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
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Fury);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static OptionItem KillCooldown;
    private static OptionItem AngryCooldown;
    private static OptionItem AngryDuration;
    private static OptionItem AngryKillCooldown;
    private static OptionItem AngrySpeed;
    private static OptionItem ShowRedNameWhenAngry;

    private static bool Angry;
    public static readonly List<byte> PlayerToAngry = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fury);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngryCooldown = FloatOptionItem.Create(Id + 11, "AngryCooldown", new(2.5f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngryDuration = FloatOptionItem.Create(Id + 12, "AngryDuration", new(2.5f, 60f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
                .SetValueFormat(OptionFormat.Seconds);
        AngryKillCooldown = FloatOptionItem.Create(Id + 13, "AngryKillCooldown", new(0f, 120f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngrySpeed = FloatOptionItem.Create(Id + 14, "AngrySpeed", new(0f, 3f, 0.25f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
        ShowRedNameWhenAngry = BooleanOptionItem.Create(Id + 15, "ShowRedNameWhenAngry", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury]);
    }

    public override void Init()
    {
        Angry = false;
        PlayerToAngry.Clear();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AngryCooldown.GetFloat();
    }

    public override void UnShapeShiftButton(PlayerControl player)
    {
        if (Angry) return;
        Angry = true;
        if (ShowRedNameWhenAngry.GetBool())
        {
            PlayerToAngry.Add(player.PlayerId);
            Utils.NotifyRoles(SpecifyTarget: player);
        }
        AURoleOptions.ShapeshifterCooldown = AngryDuration.GetFloat();
        player.SetKillCooldown(AngryKillCooldown.GetFloat());
        foreach (var target in Main.AllPlayerControls)
        {
            target.KillFlash();
            RPC.PlaySound(target.PlayerId, Sounds.ImpTransform);
            target.Notify(GetString("SeerFuryInRage"), 5f);
        }
        player.MarkDirtySettings();
        var tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
        Main.AllPlayerSpeed[player.PlayerId] = AngrySpeed.GetFloat();
        var tmpKillCooldown = Main.AllPlayerKillCooldown[player.PlayerId];
        Main.AllPlayerKillCooldown[player.PlayerId] = AngryKillCooldown.GetFloat();

        _ = new LateTask(() =>
        {
            Angry = false;
            if (ShowRedNameWhenAngry.GetBool())
            {
                PlayerToAngry.Remove(player.PlayerId);
                if (!GameStates.IsMeeting) Utils.NotifyRoles(SpecifyTarget: player);
            }
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - AngrySpeed.GetFloat() + tmpSpeed;
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] - AngryKillCooldown.GetFloat() + tmpKillCooldown;
            player.RpcResetAbilityCooldown();
            player.Notify(GetString("FuryInCalm"), 5f);
            player.MarkDirtySettings();
        }, AngryDuration.GetFloat());
    }
    public override void OnReportDeadBody(PlayerControl player, NetworkedPlayerInfo deadBody)
    {
        Angry = false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("FuryVanishText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Rage");
}
