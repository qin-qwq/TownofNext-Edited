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
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static OptionItem KillCooldown;
    private static OptionItem AngryCooldown;
    private static OptionItem AngryDuration;
    private static OptionItem AngryKillCooldown;
    private static OptionItem AngrySpeed;

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
        AngryKillCooldown = FloatOptionItem.Create(Id + 13, "AngryKillCooldown", new(0f, 120f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngrySpeed = FloatOptionItem.Create(Id + 14, "AngrySpeed", new(0f, 3f, 0.25f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Init()
    {
        PlayerToAngry.Clear();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = AngryCooldown.GetFloat();
    }

    public override bool OnCheckVanish(PlayerControl player)
    {
        if (PlayerToAngry.Contains(player.PlayerId)) return false;
        PlayerToAngry.Add(player.PlayerId);
        player.SetKillCooldown(AngryKillCooldown.GetFloat());
        foreach (var target in Main.AllPlayerControls)
        {
            if (!target.IsModded()) target.KillFlash();
            RPC.PlaySoundRPC(Sounds.ImpTransform, target.PlayerId);
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fury), GetString("SeerFuryInRage")));
        }
        player.MarkDirtySettings();
        var tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
        Main.AllPlayerSpeed[player.PlayerId] = AngrySpeed.GetFloat();
        var tmpKillCooldown = Main.AllPlayerKillCooldown[player.PlayerId];
        Main.AllPlayerKillCooldown[player.PlayerId] = AngryKillCooldown.GetFloat();

        _ = new LateTask(() =>
        {
            PlayerToAngry.Remove(player.PlayerId);
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - AngrySpeed.GetFloat() + tmpSpeed;
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] - AngryKillCooldown.GetFloat() + tmpKillCooldown;
            player.RpcResetAbilityCooldown();
            player.Notify(GetString("FuryInCalm"), 5f);
            player.MarkDirtySettings();
        }, AngryDuration.GetFloat());
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("FuryVanishText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Rage");
}
