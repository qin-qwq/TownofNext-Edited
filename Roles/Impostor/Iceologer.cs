using AmongUs.GameOptions;
using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Impostor;

internal class Iceologer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Iceologer;
    private const int Id = 33500;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem FreezeCooldown;
    private static OptionItem FreezeDuration;
    private static OptionItem FreezeIgnoreTorch;
    private static OptionItem FreezeFatal;

    private static readonly HashSet<byte> FreezePlayer = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Iceologer);
        FreezeCooldown = FloatOptionItem.Create(Id + 10, "FreezeCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Iceologer])
            .SetValueFormat(OptionFormat.Seconds);
        FreezeDuration = FloatOptionItem.Create(Id + 11, "FreezeDuration", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Iceologer])
            .SetValueFormat(OptionFormat.Seconds);
        FreezeIgnoreTorch = BooleanOptionItem.Create(Id + 12, "FreezeIgnoreTorch", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Iceologer]);
        FreezeFatal = BooleanOptionItem.Create(Id + 13, "FreezeFatal", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Iceologer]);
    }

    public override void Init()
    {
        FreezePlayer.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = FreezeCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    private void SendRPC(byte targetId = 255)
    {
        if (!_Player.IsNonHostModdedClient()) return;
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(targetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte targetId = reader.ReadByte();

        if (targetId != 255) FreezePlayer.Add(targetId);
    }

    public override bool OnCheckShapeshift(PlayerControl player, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (player.PlayerId == target.PlayerId) return false;
        if (!target.IsAlive())
        {
            resetCooldown = false;
            return false;
        }
        if (target.Is(CustomRoles.Torch) && FreezeIgnoreTorch.GetBool())
        {
            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Torch), GetString("TorchIgnoreFreeze")));
            return false;
        }
        if (FreezePlayer.Contains(target.PlayerId) && player.RpcCheckAndMurder(target, true) && FreezeFatal.GetBool())
        {
            player.KillFlash();
            target.RpcMurderPlayer(target);
            target.SetRealKiller(player);
            target.SetDeathReason(PlayerState.DeathReason.Ice);
            return false;
        }
        FreezePlayer.Add(target.PlayerId);
        SendRPC(target.PlayerId);
        Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: target);
        var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
        Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
        target.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
            target.MarkDirtySettings();
            RPC.PlaySoundRPC(Sounds.TaskComplete, target.PlayerId);
        }, FreezeDuration.GetFloat(), "Iceologer BlockMove");
        return false;
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        string color = string.Empty;
        if (seer.Is(CustomRoles.Iceologer) && FreezePlayer.Contains(target.PlayerId) && FreezeFatal.GetBool()) color = "#ADD8E6";
        return color;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.OverrideText(GetString("IceologerButtonText"));
    }
}
