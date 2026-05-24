using AmongUs.GameOptions;
using Hazel;
using TONE.Modules.Rpc;
using TONE.Roles.Crewmate;
using TONE.Roles.Impostor;
using TONE.Roles.Neutral;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.AddOns.Common;

public class Mini : IAddon
{
    //===========================SETUP================================\\
    public CustomRoles Role => CustomRoles.Mini;
    private const int Id = 7000;
    public AddonTypes Type => AddonTypes.Experimental;
    //==================================================================\\

    private static OptionItem GrowUpDuration;
    public static OptionItem EveryoneCanKnowMini;
    private static OptionItem CountMeetingTime;
    private static OptionItem UpDateAge;
    private static OptionItem CanWin;
    private static OptionItem MiniSpeed;

    public static int Age = new();
    private static int GrowUpTime = new();
    //private static int GrowUp = new();
    private static long LastFixedUpdate = new();
    private static bool misguessed = false;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Mini, canSetNum: true, teamSpawnOptions: true);
        GrowUpDuration = IntegerOptionItem.Create(Id + 100, "GrowUpDuration", new(200, 800, 25), 400, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini])
            .SetValueFormat(OptionFormat.Seconds);
        EveryoneCanKnowMini = BooleanOptionItem.Create(Id + 102, "EveryoneCanKnowMini", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        UpDateAge = BooleanOptionItem.Create(Id + 114, "UpDateAge", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        CountMeetingTime = BooleanOptionItem.Create(Id + 116, "CountMeetingTime", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        CanWin = BooleanOptionItem.Create(Id + 117, "NiceMiniCanWin", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        MiniSpeed = FloatOptionItem.Create(Id + 10, "MiniSpeed", new(0.25f, 5f, 0.25f), 2.0f, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public void Init()
    {
        GrowUpTime = 0;
        Age = 0;
        misguessed = false;
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            SendRPC();
        }
        Main.AllPlayerSpeed[playerId] = MiniSpeed.GetFloat();
    }
    public void Remove(byte playerId)
    {
        Main.AllPlayerSpeed[playerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        playerId.GetPlayer()?.MarkDirtySettings();
    }
    private static void SendRPC()
    {
        var msg = new RpcSyncMiniAge(PlayerControl.LocalPlayer.NetId, Age);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        Age = reader.ReadInt32();
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (Age < 18)
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Mini), GetString("Cantkillkid")));
            return false;
        }
        return true;
    }
    public static void OnFixedUpdates(PlayerControl player, long nowTime)
    {
        if (Age >= 18) return;

        //Check if nice mini is dead
        if (player.Is(CustomRoles.Mini) && !player.IsAlive() && player.IsPlayerCrewmateTeam() && CanWin.GetBool())
        {
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default && !CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NiceMini);
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
            }
            // â†‘ This code will show the mini winning player on the checkout screen, Tommy you shouldn't comment it out!
        } //Is there any need to check this 30 times a second?

        if (GameStates.IsMeeting && !CountMeetingTime.GetBool()) return;

        if (LastFixedUpdate == nowTime) return;
        LastFixedUpdate = nowTime;
        GrowUpTime++;

        if (GrowUpTime >= GrowUpDuration.GetInt() / 18)
        {
            Age += 1;
            GrowUpTime = 0;
            Logger.Info("Mini grow up by 1", "Mini");

            /*Dont show guard animation for evil mini,
            this would simply stop them from murdering.
            Imagine reseting kill cool down every 20 seconds
            and evil mini can never kill before age 18*/

            if (UpDateAge.GetBool())
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    SendRPC();
                }
                player.Notify(GetString("MiniUp"));
                NotifyRoles(SpecifyTarget: player);
            }
            if (Age >= 18)
            {
                var penguins = GetRoleBasesByType<Penguin>()?.ToList();
                if (TimeMaster.Rewinding)
                {
                    TimeMaster.originalSpeed[player.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
                    return;
                }
                if (Pelican.IsEaten(player.PlayerId))
                {
                    Pelican.originalSpeed[player.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
                    return;
                }
                if (TimeAssassin.TimeStop)
                {
                    return;
                }
                if (penguins != null)
                {
                    if (penguins.Any(pg => player.PlayerId == pg.AbductVictim?.PlayerId))
                    {
                        return;
                    }
                }
                Main.AllPlayerSpeed[player.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
                player?.MarkDirtySettings();
            }
        }
    }
    public static void RecoverySpeed(PlayerControl player)
    {
        if (Age < 18) return;
        if (!player.Is(CustomRoles.Mini)) return;

        Main.AllPlayerSpeed[player.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        player?.MarkDirtySettings();
    }
    public static bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (guesser.Is(CustomRoles.Mini) && Age < 18 && misguessed)
        {
            guesser.ShowInfoMessage(isUI, GetString("MiniGuessMax"));
            return true;
        }
        return false;
    }
    public static bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role is not CustomRoles.Mini) return false;
        if (target.Is(CustomRoles.Mini) && Age < 18)
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessMini"));
            return true;
        }
        return false;
    }
    public static bool CheckMisGuessed(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (Age < 18 && guesser.PlayerId == target.PlayerId)
        {
            misguessed = true;
            _ = new LateTask(() => { SendMessage(GetString("MiniMisGuessed"), target.PlayerId, ColorString(GetRoleColor(CustomRoles.Mini), GetString("GuessKillTitle")), true); }, 0.6f, "Mini MisGuess Msg");
            return true;
        }

        return false;
    }

    public static void CheckExile(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        var mini = GetPlayerById(exiled.PlayerId);
        if (mini != null && mini.Is(CustomRoles.Mini) && Age < 18 && mini.IsPlayerCrewmateTeam() && CanWin.GetBool())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(exiled.PlayerId))
            {
                if (isMeetingHud)
                {
                    name = string.Format(GetString("ExiledNiceMini"), Main.LastVotedPlayer, GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, false, true));
                }
                else
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NiceMini);
                    CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                }
                DecidedWinner = true;
            }
        }
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        => (EveryoneCanKnowMini.GetBool() || seer.Is(CustomRoles.Mini)) && target.Is(CustomRoles.Mini)
            ? CustomRoles.Mini.GetColoredTextByRole(Age != 18 && UpDateAge.GetBool() ? $"({Age})" : string.Empty)
            : string.Empty;
}
