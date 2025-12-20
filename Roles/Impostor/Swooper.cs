using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Swooper : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Swooper;
    private const int Id = 4700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Swooper);
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SwooperCooldown;
    private static OptionItem SwooperDuration;

    private static readonly Dictionary<byte, long> InvisCooldown = [];
    private static readonly Dictionary<byte, long> InvisDuration = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Swooper);
        SwooperCooldown = FloatOptionItem.Create(Id + 2, "SwooperCooldown", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Swooper])
            .SetValueFormat(OptionFormat.Seconds);
        SwooperDuration = FloatOptionItem.Create(Id + 4, "SwooperDuration", new(1f, 60f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Swooper])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        InvisCooldown.Clear();
        InvisDuration.Clear();
    }
    public override void Add(byte playerId)
    {
        InvisCooldown[playerId] = Utils.GetTimeStamp();
    }
    private void SendRPC(PlayerControl pc)
    {
        if (!pc.IsNonHostModdedClient()) return;
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(InvisCooldown.GetValueOrDefault(pc.PlayerId, -1).ToString());
        writer.Write(InvisDuration.GetValueOrDefault(pc.PlayerId, -1).ToString());
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        InvisCooldown.Clear();
        InvisDuration.Clear();
        long cooldown = long.Parse(reader.ReadString());
        long invis = long.Parse(reader.ReadString());
        if (cooldown > 0) InvisCooldown.Add(PlayerControl.LocalPlayer.PlayerId, cooldown);
        if (invis > 0) InvisDuration.Add(PlayerControl.LocalPlayer.PlayerId, invis);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = SwooperCooldown.GetFloat() + 1;        
    }

    private static bool CanGoInvis(byte id)
        => GameStates.IsInTask && !InvisCooldown.ContainsKey(id);

    private static bool IsInvis(byte id)
        => InvisDuration.ContainsKey(id);

    public override bool OnCheckVanish(PlayerControl player)
    {
        if (IsInvis(player.PlayerId)) return false;

        if (CanGoInvis(player.PlayerId))
        {
            player.RpcMakeInvisible();
            
            InvisDuration.Remove(player.PlayerId);
            InvisDuration.Add(player.PlayerId, Utils.GetTimeStamp());

            SendRPC(player);
            player.Notify(GetString("SwooperInvisState"), SwooperDuration.GetFloat(), hasPriority: true);
        }

        return false;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        var playerId = player.PlayerId;
        var needSync = false;

        if (InvisCooldown.TryGetValue(playerId, out var oldTime) && (oldTime + (long)SwooperCooldown.GetFloat() - nowTime) < 0)
        {
            InvisCooldown.Remove(playerId);
            if (!player.IsModded()) player.Notify(GetString("SwooperCanVent"), hasPriority: true);
            needSync = true;
        }

        foreach (var swoopInfo in InvisDuration)
        {
            var swooperId = swoopInfo.Key;
            var swooper = Utils.GetPlayerById(swooperId);
            if (swooper == null) continue;

            var remainTime = swoopInfo.Value + (long)SwooperDuration.GetFloat() - nowTime;

            if (remainTime < 0 || !swooper.IsAlive())
            {
                InvisCooldown.Remove(swooperId);
                InvisCooldown.Add(swooperId, nowTime);

                swooper.Notify(GetString("SwooperInvisStateOut"), hasPriority: true);
                swooper.RpcMakeVisible();
                swooper.RpcResetAbilityCooldown();

                needSync = true;
                InvisDuration.Remove(swooperId);
            }
            else if (remainTime <= 10)
            {
                if (!swooper.IsModded())
                    swooper.Notify(string.Format(GetString("SwooperInvisStateCountdown"), remainTime), hasPriority: true, sendInLog: false);
            }
        }

        if (needSync)
        {
            SendRPC(player);
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!IsInvis(killer.PlayerId)) return true;

        if (!killer.RpcCheckAndMurder(target, true)) return false;

        RPC.PlaySoundRPC(Sounds.KillSound, killer.PlayerId);
        killer.RpcGuardAndKill(target);
        killer.SetKillCooldown();

        target.RpcMurderPlayer(target);
        target.SetRealKiller(killer);
        return false;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var swooperId in _playerIdList)
        {
            if (!IsInvis(swooperId)) continue;
            var swooper = Utils.GetPlayerById(swooperId);
            if (swooper == null) continue;

            InvisDuration.Remove(swooperId);
            swooper.RpcMakeVisible();
            SendRPC(swooper);
        }

        InvisCooldown.Clear();
        InvisDuration.Clear();
    }
    public override void AfterMeetingTasks()
    {
        InvisCooldown.Clear();
        InvisDuration.Clear();

        foreach (var swooperId in _playerIdList)
        {
            var swooper = Utils.GetPlayerById(swooperId);
            if (!swooper.IsAlive()) continue;

            InvisCooldown.Add(swooperId, Utils.GetTimeStamp());
            SendRPC(swooper);
        }
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        // Only for modded
        if (seer == null || !isForHud || isForMeeting || !seer.IsAlive()) return string.Empty;

        var str = new StringBuilder();
        var seerId = seer.PlayerId;

        if (IsInvis(seerId))
        {
            var remainTime = InvisDuration[seerId] + (long)SwooperDuration.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("SwooperInvisStateCountdown"), remainTime + 1));
        }
        else if (InvisCooldown.TryGetValue(seerId, out var time))
        {
            var cooldown = time + (long)SwooperCooldown.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("SwooperInvisCooldownRemain"), cooldown + 1));
        }
        else
        {
            str.Append(GetString("SwooperCanVent"));
        }
        return str.ToString();
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(GetString("SwooperVentButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("invisible");
}