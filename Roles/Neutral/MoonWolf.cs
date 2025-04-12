using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class MoonWolf : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.MoonWolf;
    private const int Id = 31700;

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MoonCooldown;
    private static OptionItem MoonDuration;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanUsesSabotage;

    private bool Moon;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.MoonWolf, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MoonWolf])
            .SetValueFormat(OptionFormat.Seconds);
        MoonCooldown = FloatOptionItem.Create(Id + 11, "MoonCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MoonWolf])
            .SetValueFormat(OptionFormat.Seconds);
        MoonDuration = FloatOptionItem.Create(Id + 12, "MoonDuration", new(0f, 180f, 1f), 12f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MoonWolf])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 13, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MoonWolf]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 14, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MoonWolf]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 15, GeneralOption.CanUseSabotage, false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MoonWolf]);
    }

    public override void Init()
    {
        Moon = false;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Moon ? KillCooldown.GetFloat() : 300f;
    public override void ApplyGameOptions(IGameOptions opt, byte id) 
    {
        AURoleOptions.ShapeshifterCooldown = MoonCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
        opt.SetVision(HasImpostorVision.GetBool());
    }
    public override bool CanUseKillButton(PlayerControl pc) 
    {
        return Moon && pc.IsAlive();
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Moon = false;
    }
    
    public override void UnShapeShiftButton(PlayerControl player)
    {
        Moon = true;
        player.ResetKillCooldown();
        player.SetKillCooldown();
        player.Notify(GetString("InMoon"), MoonDuration.GetFloat());
        RPC.PlaySoundRPC(player.PlayerId, Sounds.ImpTransform);
        player.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            Moon = false;
            player.ResetKillCooldown();
            player.SetKillCooldown();
            player.RpcResetAbilityCooldown();
            player.MarkDirtySettings();
        }, MoonDuration.GetFloat());
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("MoonWolfShapeshiftText"));
    }
}
