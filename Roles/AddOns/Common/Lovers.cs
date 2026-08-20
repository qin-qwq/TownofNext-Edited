using Hazel;
using InnerNet;
using TONE.Modules.Rpc;
using TONE.Roles.Neutral;
using static TONE.Options;

namespace TONE.Roles.AddOns.Common;

public class Lovers : IAddon
{
    public CustomRoles Role => CustomRoles.Lovers;
    private const int Id = 23600;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem LoverKnowRoles;
    public static OptionItem LoverSuicide;
    public static OptionItem PrivateChat;
    public static OptionItem ImpCanBeInLove;
    public static OptionItem CrewCanBeInLove;
    public static OptionItem NeutralCanBeInLove;
    public static OptionItem CovenCanBeInLove;

    public static PlayerControl loverless = null;
    public static readonly Dictionary<PlayerControl, PlayerControl> LoversPlayers = [];

    public void SetupCustomOption()
    {
        var spawnOption = StringOptionItem.Create(Id, "Lovers", EnumHelper.GetAllNames<RatesZeroOne>(), 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(CustomRoles.Lovers))
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(Id + 1, "NumberOfLovers", new(2, 2, 2), 2, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetHidden(true)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.Standard);

        var spawnRateOption = IntegerOptionItem.Create(Id + 2, "LoverSpawnChances", new(0, 100, 5), 65, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.Standard) as IntegerOptionItem;

        LoverKnowRoles = BooleanOptionItem.Create(Id + 4, "LoverKnowRoles", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        LoverSuicide = BooleanOptionItem.Create(Id + 3, "LoverSuicide", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        PrivateChat = BooleanOptionItem.Create(Id + 5, "PrivateChat", false, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        ImpCanBeInLove = BooleanOptionItem.Create(Id + 6, "ImpCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        CrewCanBeInLove = BooleanOptionItem.Create(Id + 7, "CrewCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        NeutralCanBeInLove = BooleanOptionItem.Create(Id + 8, "NeutralCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        CovenCanBeInLove = BooleanOptionItem.Create(Id + 9, "CovenCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        CustomAdtRoleSpawnRate.Add(CustomRoles.Lovers, spawnRateOption);
        CustomRoleSpawnChances.Add(CustomRoles.Lovers, spawnOption);
        CustomRoleCounts.Add(CustomRoles.Lovers, countOption);
    }
    public void Init()
    {
        loverless = null;
        LoversPlayers.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        var player = Utils.GetPlayerById(playerId);
        if (!loverless)
        {
            loverless = player;
        }
        else
        {
            LoversPlayers[loverless] = player;
            LoversPlayers[player] = loverless;
            SendRPC(loverless, player);
            loverless = null;
        }
    }
    public void Remove(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (LoversPlayers.TryGetValue(player, out var partner))
        {
            LoversPlayers.Remove(player);
            LoversPlayers.Remove(partner);

            Main.PlayerStates[partner.PlayerId].RemoveSubRole(CustomRoles.Lovers);
        }
    }

    public static byte GetLoverId(PlayerControl player)
    {
        if (!LoversPlayers.ContainsKey(player))
            return byte.MaxValue;

        return LoversPlayers[player].PlayerId;
    }
    public static bool AreLovers(PlayerControl player, PlayerControl target)
    {
        if (!LoversPlayers.ContainsKey(player) || !LoversPlayers.ContainsKey(target)) return false;

        return true;
    }

    public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (LoverSuicide.GetBool())
        {
            var deadPlayer = Utils.GetPlayerById(deathId);
            if (!deadPlayer) return;

            if (!LoversPlayers.TryGetValue(deadPlayer, out var partnerPlayer)) return;

            if (!partnerPlayer || !partnerPlayer.IsAlive()) return;

            if (partnerPlayer.Is(CustomRoles.Lovers))
            {
                partnerPlayer.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);

                if (isExiled)
                {
                    //if (Main.PlayersDiedInMeeting.Contains(deathId))
                    //{
                    partnerPlayer.RpcExileV3();
                    if (MeetingHud.Instance?.state is MeetingHud.MeetingStates.Discussion or MeetingHud.MeetingStates.NotVoted or MeetingHud.MeetingStates.Voted)
                    {
                        MeetingHud.Instance?.CheckForEndVoting();
                    }
                    _ = new LateTask(() => HudManager.Instance?.SetHudActive(false), 0.3f, "SetHudActive in LoversSuicide", shoudLog: false);
                    //}
                    //else
                    //{
                    //CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                    //}
                }
                else
                {
                    partnerPlayer.RpcMurderPlayer(partnerPlayer);
                }
            }
        }
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen)
    {
        string colorCode = Utils.GetRoleColorCode(CustomRoles.Lovers);
        if (AreLovers(seer, seen) || (seer.Is(CustomRoles.Lovers) && seer.PlayerId == seen.PlayerId))
        {
            return $"<color={colorCode}>♡</color>";
        }
        else if ((!seer.IsAlive() || Cupid.IsCupidLover(seer, seen)) && seen.Is(CustomRoles.Lovers))
        {
            byte loverId = GetLoverId(seen);
            return $"<color={colorCode}>♡{loverId}</color>";
        }

        return "";
    }

    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player || !target) return;

        var msg = new RpcSetLoverPairs(PlayerControl.LocalPlayer.NetId, player, target);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        LoversPlayers.Clear();
        var player = reader.ReadNetObject<PlayerControl>();
        var target = reader.ReadNetObject<PlayerControl>();

        if (player && target)
        {
            LoversPlayers[player] = target;
            LoversPlayers[target] = player;
        }
    }

    public static void CheckWin()
    {
        var eligiblePairs = LoversPlayers.Where(x => x.Key.PlayerId < x.Value.PlayerId && !Utils.IsSameTeammate(x.Key, x.Value, neu: false) && (!LoverSuicide.GetBool() || (x.Key.IsAlive() && x.Value.IsAlive()))).ToList();

        if (!eligiblePairs.Any()) return;
        // if not (some lovers dead and lovers suicide)
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican or CustomWinner.Coven)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
            foreach (var pair in eligiblePairs)
            {
                CustomWinnerHolder.WinnerIds.Add(pair.Key.PlayerId);
                CustomWinnerHolder.WinnerIds.Add(pair.Value.PlayerId);
            }
        }
    }
    public static void CheckAdditionalWin()
    {
        var loverWinners = CustomWinnerHolder.WinnerIds.Where(p => p.GetPlayer().Is(CustomRoles.Lovers) && p.GetPlayer().IsPlayerNeutralTeam());

        foreach (var lover in loverWinners)
        {
            var loverId = GetLoverId(lover.GetPlayer());
            if (!CustomWinnerHolder.WinnerIds.Contains(loverId))
            {
                CustomWinnerHolder.WinnerIds.Add(loverId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
            }
        }
    }

    public static void OnPartnerLeft(PlayerControl player)
    {
        if (LoversPlayers.TryGetValue(player, out var partner))
        {
            LoversPlayers.Remove(player);
            LoversPlayers.Remove(partner);

            Main.PlayerStates[player.PlayerId].RemoveSubRole(CustomRoles.Lovers);
            Main.PlayerStates[partner.PlayerId].RemoveSubRole(CustomRoles.Lovers);
        }
    }

    public static bool LoversMsg(PlayerControl pc, string msg, bool check = true)
    {
        //if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || !pc) return false;
        if (!pc.Is(CustomRoles.Lovers)) return false;
        if (!PrivateChat.GetBool()) return false;
        if (!pc.IsAlive()) return false;
        msg = msg.ToLower().Trim();
        if (check)
        {
            if (!GuessManager.CheckCommond(ref msg, "lo|恋人", false)) return false;
        }

        var player = GetLoverId(pc);
        if (player == byte.MaxValue || !player.GetPlayer().IsAlive()) return false;

        if (string.IsNullOrEmpty(msg)) return false;

        if (AmongUsClient.Instance.AmHost || !pc.IsModded())
        {
            SendLoversChannelMsg(pc, msg);
        }
        else
        {
            var message = new RpcSendChannelMsg(PlayerControl.LocalPlayer.NetId, msg, (int)SendTargetPatch.SendTargets.Lovers);
            RpcUtils.LateBroadcastReliableMessage(message);
        }

        return true;
    }

    public static void SendLoversChannelMsg(PlayerControl pc, string msg)
    {
        var player = GetLoverId(pc);
        Main.EnumerateAlivePlayerControls().Where(x => x.PlayerId == player || x == pc)
            .Do(x => Utils.SendMessage(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), msg), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), $"{Translator.GetString("MessageFromLovers")} ~ <size=1.25>{pc.GetRealName(clientData: true)}</size>"), sendTo: x.PlayerId, noReplay: true));
    }
}

public static class LoversUtils
{
    public static bool IsLoverWith(this PlayerControl player, PlayerControl target) => Lovers.AreLovers(player, target);
}
