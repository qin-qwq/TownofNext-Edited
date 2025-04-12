using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using MS.Internal.Xml.XPath;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Yandere : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Yandere;
    private const int Id = 33000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Yandere);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem KnowTargetRole;
    private static OptionItem TargetKnowYandere;
    public static OptionItem YandereWinWithTarget;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanUsesSabotage;

    public static Dictionary<byte, byte> BetPlayer = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Yandere, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 11, "YandereKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
        TargetKnowYandere = BooleanOptionItem.Create(Id + 12, "TargetKnowYandere", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
        YandereWinWithTarget = BooleanOptionItem.Create(Id + 13, "YandereWinWithTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
        CanVent = BooleanOptionItem.Create(Id + 14, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 15, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 16, GeneralOption.CanUseSabotage, false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
    }

    public override void Init()
    {
        BetPlayer.Clear();
    }

    public override void Add(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);
        if (AmongUsClient.Instance.AmHost)
        {
            ResetTarget(Utils.GetPlayerById(playerId));
        }
    }

    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Remove(OthersAfterPlayerDeathTask);
    }

    private static void SendRPC(byte bountyId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetYandereTarget, SendOption.Reliable, -1);
        writer.Write(bountyId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte bountyId = reader.ReadByte();
        byte targetId = reader.ReadByte();
        BetPlayer[bountyId] = targetId;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte babuyaga) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Yandere) && BetPlayer.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer == seen) return string.Empty;

        return BetPlayer.ContainsValue(seen.PlayerId)
            ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Yandere), "♡") : string.Empty;
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Yandere) && TargetKnowYandere.GetBool())
        {
            if (seer == target && seer.IsAlive() && BetPlayer.ContainsValue(seer.PlayerId))
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Yandere), "♡");
            }
            else if (seer != target && seer.IsAlive() && BetPlayer.ContainsKey(target.PlayerId) && BetPlayer.ContainsValue(seer.PlayerId))
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Yandere), "♡");
            }
            else if (seer != target && !seer.IsAlive() && BetPlayer.ContainsValue(target.PlayerId))
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Yandere), "♡");
            }
        }
        return string.Empty;
    }

    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        if (exiled == null) return;

        var exiledId = exiled.PlayerId;
        if (BetPlayer.ContainsValue(exiledId))
        {
            player = Utils.GetPlayerById(exiledId);
            if (player == null) return;

            TryKill(player);
        }
    }

    private void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl player, bool inMeeting)
    {
        TryKill(player);
    }

    private void TryKill(PlayerControl player)
    {
        var playerId = player.PlayerId;
        if (!BetPlayer.ContainsValue(playerId) || player == null) return;

        byte yandere = 0x73;
        BetPlayer.Do(x =>
        {
            if (x.Value == playerId)
                yandere = x.Key;
        });
        if (yandere == 0x73) return;
        var pc = Utils.GetPlayerById(yandere);
        if (pc == null) return;

        if (player.GetRealKiller() == pc)
        {
            pc.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);
            pc.RpcMurderPlayer(pc);
            return;
        }
        else
        {
            pc.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);
            pc.RpcExileV2();
            pc.Data.IsDead = true;
            pc.Data.MarkDirty();
            Main.PlayerStates[pc.PlayerId].SetDead();
        }
    }

    public static byte GetTarget(PlayerControl player)
    {
        if (player == null) return 0xff;
        BetPlayer ??= [];

        if (!BetPlayer.TryGetValue(player.PlayerId, out var targetId))
            targetId = ResetTarget(player);
        return targetId;
    }

    public static PlayerControl GetTargetPC(PlayerControl player)
    {
        var targetId = GetTarget(player);
        return targetId == 0xff ? null : Utils.GetPlayerById(targetId);
    }

    private static bool PotentialTarget(PlayerControl player, PlayerControl target)
    {
        if (target == null || player == null) return false;

        if (target == player) return false;

        if (target.Is(CustomRoles.Lovers)) return false;

        return true;
    }

    private static byte ResetTarget(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return 0xff;

        var playerId = player.PlayerId;

        var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => PotentialTarget(player, pc) && pc.GetCustomRole() is not CustomRoles.Solsticer));

        if (cTargets.Count >= 2 && BetPlayer.TryGetValue(player.PlayerId, out var nowTarget))
            cTargets.RemoveAll(x => x.PlayerId == nowTarget);

        if (cTargets.Count == 0)
        {
            return 0xff;
        }

        var rand = IRandom.Instance;
        var target = cTargets.RandomElement();
        var targetId = target.PlayerId;
        BetPlayer[playerId] = targetId;

        SendRPC(player.PlayerId, targetId);
        return targetId;
    }
}
