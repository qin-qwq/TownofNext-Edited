using Hazel;
using System;
using TONE.Modules.Rpc;
using static TONE.Options;

namespace TONE.Roles.Impostor;

internal class Arrogance : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Arrogance;
    private const int Id = 500;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    public static OptionItem BardChance;

    private static readonly Dictionary<byte, float> NowCooldown = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Arrogance);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.DefaultKillCooldown, new(0f, 180f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ReduceKillCooldown, new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.MinKillCooldown, new(0f, 180f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        BardChance = IntegerOptionItem.Create(Id + 13, "BardChance", new(0, 100, 5), 0, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Init()
    {
        NowCooldown.Clear();
    }
    public override void Add(byte playerId)
    {
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public override void Remove(byte playerId)
    {
        NowCooldown.Remove(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        SendRPC(killer);
        killer.ResetKillCooldown();
        killer.SyncSettings();

        return true;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var reduce = Math.Round(DefaultKillCooldown.GetFloat() - NowCooldown[playerId], 1);
        return NowCooldown[playerId] != DefaultKillCooldown.GetFloat() ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $" -{reduce}s") : string.Empty;
    }
    public void SendRPC(PlayerControl pc)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(NowCooldown[pc.PlayerId]);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        NowCooldown[pc.PlayerId] = reader.ReadSingle();
    }
}
