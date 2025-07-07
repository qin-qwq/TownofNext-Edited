using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Ninja : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Ninja;
    private const int Id = 2100;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem MarkCooldown;
    private static OptionItem AssassinateCooldownOpt;
    private static OptionItem ShapeshiftDurationOpt;

    private static readonly Dictionary<byte, byte> MarkedPlayer = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Ninja);
        MarkCooldown = FloatOptionItem.Create(Id + 10, "NinjaMarkCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ninja])
            .SetValueFormat(OptionFormat.Seconds);
        AssassinateCooldownOpt = FloatOptionItem.Create(Id + 11, "NinjaAssassinateCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ninja])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDurationOpt = FloatOptionItem.Create(Id + 13, "SwooperDuration", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ninja])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        MarkedPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    private static void SendRPC(byte playerId)
    {
        var targetId = MarkedPlayer.ContainsKey(playerId) ? MarkedPlayer[playerId] : byte.MaxValue;
        var msg = new RpcSetMarkedPlayer(PlayerControl.LocalPlayer.NetId, playerId, targetId);
        RpcUtils.LateBroadcastReliableMessage(msg);

    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        MarkedPlayer.Remove(playerId);
        if (targetId != byte.MaxValue)
            MarkedPlayer.Add(playerId, targetId);
    }

    private static bool Shapeshifting(byte id) => Main.CheckShapeshift.TryGetValue(id, out bool shapeshifting) && shapeshifting;

    public override void SetKillCooldown(byte id)
        => Main.AllPlayerKillCooldown[id] = Shapeshifting(id) ? DefaultKillCooldown : MarkCooldown.GetFloat();

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AssassinateCooldownOpt.GetFloat();
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("CantMark")));
            return true;
        }

        return killer.CheckDoubleTrigger(target,
            () =>
            {
                MarkedPlayer.Remove(killer.PlayerId);
                MarkedPlayer.Add(killer.PlayerId, target.PlayerId);
                SendRPC(killer.PlayerId);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.SyncSettings();
                killer.RPCPlayCustomSound("Clothe");
            });
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        // Ninja not marked player
        if (!MarkedPlayer.ContainsKey(shapeshifter.PlayerId)) return;

        // Check and kill marked player
        if (MarkedPlayer.TryGetValue(shapeshifter.PlayerId, out var targetId))
        {
            var marketTarget = Utils.GetPlayerById(targetId);

            MarkedPlayer.Remove(shapeshifter.PlayerId);
            SendRPC(shapeshifter.PlayerId);

            if (!(marketTarget == null || !marketTarget.IsAlive()))
            {
                if (shapeshifter.RpcCheckAndMurder(marketTarget, check: true))
                {
                    if (marketTarget.inVent)
                        marketTarget.MyPhysics.RpcBootFromVent(Main.LastEnteredVent[marketTarget.PlayerId].Id);

                    shapeshifter.RpcMakeInvisible();
                    shapeshifter.RpcTeleport(marketTarget.GetCustomPosition());
                    shapeshifter.ResetKillCooldown();
                    shapeshifter.RpcMurderPlayer(marketTarget);

                    _ = new LateTask(() =>
                    {
                        shapeshifter.Notify(GetString("SwooperInvisStateOut"));
                        shapeshifter.RpcMakeVisible();
                    }, ShapeshiftDurationOpt.GetFloat());

                    Logger.Info("Was kill market target", "Ninja");
                }
            }
            else
                shapeshifter.Notify(Utils.ColorString(Utils.GetRoleColor(shapeshifter.GetCustomRole()), GetString("TargetIsAlreadyDead")));
        }
    }
    public override string GetLowerText(PlayerControl witch, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return string.Empty;

        var str = new StringBuilder();
        str.Append(GetString("NinjaModeDouble"));
        return str.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerid)
    {
        if (!Shapeshifting(playerid))
            hud.KillButton.OverrideText(GetString("MarkButtonText"));
        else
            hud.KillButton.OverrideText(GetString("KillButtonText"));

        if (MarkedPlayer.ContainsKey(playerid) && !Shapeshifting(playerid))
            hud.AbilityButton.OverrideText(GetString("KillButtonText"));
    }

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => !shapeshifting ? CustomButton.Get("Mark") : null;
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => !shapeshifting && MarkedPlayer.ContainsKey(player.PlayerId) ? CustomButton.Get("Assassinate") : null;
}
