using Hazel;
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

    public static bool isLoversDead = true;
    public static readonly HashSet<PlayerControl> LoversPlayers = [];

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
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    public static byte GetLoverId(PlayerControl player)
    {
        if (!LoversPlayers.Any())
            return byte.MaxValue;

        return LoversPlayers.FirstOrDefault(lp => lp.PlayerId != player.PlayerId).PlayerId;
    }
    public static byte GetLoverId(byte playerId) => GetLoverId(playerId.GetPlayer());
    public static bool AreLovers(PlayerControl player, PlayerControl target) => player.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers);
    public static bool AreLovers(byte player, byte target) => AreLovers(player.GetPlayer(), target.GetPlayer());

    public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (LoverSuicide.GetBool() && isLoversDead == false)
        {
            foreach (var loversPlayer in LoversPlayers.ToArray())
            {
                if (loversPlayer.IsAlive() && loversPlayer.PlayerId != deathId) continue;

                isLoversDead = true;
                foreach (var partnerPlayer in LoversPlayers.ToArray())
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    if (partnerPlayer.PlayerId != deathId && partnerPlayer.IsAlive())
                    {
                        if (partnerPlayer.Is(CustomRoles.Lovers))
                        {
                            partnerPlayer.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);

                            if (isExiled)
                            {
                                //if (Main.PlayersDiedInMeeting.Contains(deathId))
                                //{
                                    partnerPlayer.RpcExileV3();
                                    if (MeetingHud.Instance?.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
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

    public static void ReceiveRPC(MessageReader reader)
    {
        LoversPlayers.Clear();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
    }

    public static void CheckWin()
    {
        var alivePairs = !(!LoversPlayers.ToArray().All(p => p.IsAlive()) && LoverSuicide.GetBool());

        if (!alivePairs) return;
        if (SameTeammate(neu: false)) return;
        // if not (some lovers dead and lovers suicide)
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican or CustomWinner.Coven)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
            Main.AllPlayerControls
                .Where(p => p.Is(CustomRoles.Lovers))
                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
        }
    }
    public static void CheckAdditionalWin()
    {
        var loverArray = Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lovers)).ToArray();

        foreach (var lover in loverArray)
        {
            if (CustomWinnerHolder.WinnerIds.Any(x => Utils.GetPlayerById(x).Is(CustomRoles.Lovers)) && !CustomWinnerHolder.WinnerIds.Contains(lover.PlayerId))
            {
                CustomWinnerHolder.WinnerIds.Add(lover.PlayerId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
            }
        }
    }

    public static void OnPartnerLeft()
    {
        foreach (var lovers in LoversPlayers.ToArray())
        {
            isLoversDead = true;
            LoversPlayers.Remove(lovers);
            Main.PlayerStates[lovers.PlayerId].RemoveSubRole(CustomRoles.Lovers);
        }
    }

    public static bool SameTeammate(bool crew = true, bool imp = true, bool neu = true, bool coven = true)
    {
        if (!LoversPlayers.Any())
            return false;

        var lovers = LoversPlayers.ToArray();

        var first = lovers[0];
        return lovers.All(p => Utils.IsSameTeammate(first, p, crew, imp, neu, coven));
    }

    public static bool LoversMsg(PlayerControl pc, string msg, bool check = true)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null) return false;
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

        Main.EnumerateAlivePlayerControls().Where(x => x.PlayerId == player || x == pc)
            .Do(x => Utils.SendMessage(msg, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), $"{Translator.GetString("MessageFromLovers")} ~ <size=1.25>{pc.GetRealName(clientData: true)}</size>"), sendTo: x.PlayerId, noReplay: true));

        return true;
    }
}

public static class LoversUtils
{
    public static bool IsLoverWith(this PlayerControl player, PlayerControl target) => Lovers.AreLovers(player, target);
}
