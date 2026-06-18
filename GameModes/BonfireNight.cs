using System.Text;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Core;
using TONE.Roles.Core.AssignManager;
using UnityEngine;
using static TONE.RoleBase;
using static TONE.Translator;
using static TONE.Utils;
using Tree = TONE.Modules.Tree;

namespace TONE;

public static class BonfireNight
{
    private const int Id = 67_228_001;

    public static OptionItem GameTime;
    public static OptionItem WoodToWin;
    public static OptionItem KillCooldown;
    public static OptionItem ReviveCooldown;
    public static OptionItem InvincibilityCooldownAfterRevive;
    public static OptionItem PickUpWoodCooldown;
    public static OptionItem MaximumWoodHoldingQuantity;
    public static OptionItem FireThiefMaximumWoodHoldingQuantity;
    public static OptionItem FireThiefCanStealWood;
    public static OptionItem WoodGrowthTime;
    public static OptionItem MaximumWoodRefreshQuantity;
    public static OptionItem SakuraWoodGrowthTime;
    public static OptionItem MaximumSakuraWoodRefreshQuantity;

    public static TeamState RedTeamState;
    public static TeamState BlueTeamState;
    public static TeamState FireThiefState;

    public static long StartedAt = 0;
    public static (int, int, int) BonfireState = (0, 0, 0);
    public static bool FireThief = false;
    public static (string, string) Draw = (string.Empty, string.Empty);
    public static int CachedWoodGrowthTime;
    public static int CachedMaxWoodRefresh;
    public static int CachedSakuraGrowthTime;
    public static int CachedMaxSakuraRefresh;

    public static TreeState Tree1;
    public static TreeState Tree2;
    public static TreeState Tree3;
    public static TreeState Tree4;
    public static SakuraTreeState SakuraTree1;
    public static SakuraTreeState SakuraTree2;

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(10000038, "MenuTitle.BonfireNight", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue));

        GameTime = IntegerOptionItem.Create(Id + 2, "BonfireNight_GameTime", (60, 600, 15), 300, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        WoodToWin = IntegerOptionItem.Create(Id + 3, "BonfireNight_WoodToWin", (10, 300, 10), 100, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Pieces);

        KillCooldown = FloatOptionItem.Create(Id + 4, GeneralOption.KillCooldown, (2.5f, 300f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        ReviveCooldown = FloatOptionItem.Create(Id + 5, "BonfireNight_ReviveCooldown", (2.5f, 300f, 2.5f), 10f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        InvincibilityCooldownAfterRevive = IntegerOptionItem.Create(Id + 14, "BonfireNight_InvincibilityCooldownAfterRevive", (0, 300, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        PickUpWoodCooldown = IntegerOptionItem.Create(Id + 6, "BonfireNight_PickUpWoodCooldown", (0, 60, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);

        MaximumWoodHoldingQuantity = IntegerOptionItem.Create(Id + 7, "BonfireNight_MaximumWoodHoldingQuantity", (1, 60, 1), 3, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Pieces)
            .SetHeader(true);
        FireThiefMaximumWoodHoldingQuantity = IntegerOptionItem.Create(Id + 8, "BonfireNight_FireThiefMaximumWoodHoldingQuantity", (1, 60, 1), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Pieces);
        FireThiefCanStealWood = BooleanOptionItem.Create(Id + 9, "BonfireNight_FireThiefCanStealWood", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue));
        WoodGrowthTime = IntegerOptionItem.Create(Id + 10, "BonfireNight_WoodGrowthTime", (1, 60, 1), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        MaximumWoodRefreshQuantity = IntegerOptionItem.Create(Id + 11, "BonfireNight_MaximumWoodRefreshQuantity", (1, 60, 1), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Pieces);
        SakuraWoodGrowthTime = IntegerOptionItem.Create(Id + 12, "BonfireNight_SakuraWoodGrowthTime", (1, 60, 1), 15, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        MaximumSakuraWoodRefreshQuantity = IntegerOptionItem.Create(Id + 13, "BonfireNight_MaximumSakuraWoodRefreshQuantity", (1, 60, 1), 30, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.BonfireNight)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Pieces);
    }

    public static void Init()
    {
        StartedAt = GetTimeStamp();
        BonfireState = (0, 0, 0);
        FireThief = false;
        Draw = (string.Empty, string.Empty);
        Tree1.Wood = MaximumWoodRefreshQuantity.GetInt();
        Tree2.Wood = MaximumWoodRefreshQuantity.GetInt();
        Tree3.Wood = MaximumWoodRefreshQuantity.GetInt();
        Tree4.Wood = MaximumWoodRefreshQuantity.GetInt();
        SakuraTree1.Wood = MaximumSakuraWoodRefreshQuantity.GetInt();
        SakuraTree2.Wood = MaximumSakuraWoodRefreshQuantity.GetInt();
        Tree1.Grow = 0;
        Tree2.Grow = 0;
        Tree3.Grow = 0;
        Tree4.Grow = 0;
        SakuraTree1.Grow = 0;
        SakuraTree2.Grow = 0;
        CachedWoodGrowthTime = WoodGrowthTime.GetInt();
        CachedMaxWoodRefresh = MaximumWoodRefreshQuantity.GetInt();
        CachedSakuraGrowthTime = SakuraWoodGrowthTime.GetInt();
        CachedMaxSakuraRefresh = MaximumSakuraWoodRefreshQuantity.GetInt();

        switch (GetActiveMapName())
        {
            case MapNames.Skeld:
                RedTeamState.Position = new Vector2(-9.0f, -4.0f);
                BlueTeamState.Position = new Vector2(4.0f, -15.5f);
                FireThiefState.Position = new Vector2(4.5f, -7.9f);
                SakuraTree1.Position = new Vector2(9.3f, 1.0f);
                SakuraTree2.Position = new Vector2(-17.0f, -13.5f);
                Tree1.Position = new Vector2(-20.5f, -5.5f);
                Tree2.Position = new Vector2(-1.0f, 3.0f);
                Tree3.Position = new Vector2(-7.5f, -8.8f);
                Tree4.Position = new Vector2(16.5f, -4.8f);
                break;
            case MapNames.MiraHQ:
                RedTeamState.Position = new Vector2(25.5f, 2.0f);
                BlueTeamState.Position = new Vector2(9.5f, 12.0f);
                FireThiefState.Position = new Vector2(-4.5f, 2.0f);
                SakuraTree1.Position = new Vector2(15.3f, 3.8f);
                SakuraTree2.Position = new Vector2(17.8f, 23.0f);
                Tree1.Position = new Vector2(19.5f, 4.0f);
                Tree2.Position = new Vector2(15.0f, 19.0f);
                Tree3.Position = new Vector2(15.5f, -0.5f);
                Tree4.Position = new Vector2(2.5f, 10.5f);
                break;
            case MapNames.Polus:
                RedTeamState.Position = new Vector2(36.5f, -7.5f);
                BlueTeamState.Position = new Vector2(2.3f, -24.0f);
                FireThiefState.Position = new Vector2(21.75f, -25.15f);
                SakuraTree1.Position = new Vector2(26.0f, -17.0f);
                SakuraTree2.Position = new Vector2(9.5f, -12.5f);
                Tree1.Position = new Vector2(36.5f, -22.0f);
                Tree2.Position = new Vector2(2.0f, -17.5f);
                Tree3.Position = new Vector2(20.5f, -12.0f);
                Tree4.Position = new Vector2(24.0f, -22.5f);
                break;
            case MapNames.Airship:
                RedTeamState.Position = new Vector2(-10.3f, -5.9f);
                BlueTeamState.Position = new Vector2(33.5f, -1.5f);
                FireThiefState.Position = new Vector2(6.35f, 2.5f);
                SakuraTree1.Position = new Vector2(-8.9f, 12.2f);
                SakuraTree2.Position = new Vector2(16.3f, -8.8f);
                Tree1.Position = new Vector2(-23.5f, -1.6f);
                Tree2.Position = new Vector2(5.8f, -10.8f);
                Tree3.Position = new Vector2(20f, 10.5f);
                Tree4.Position = new Vector2(15.5f, 0f);
                break;
            case MapNames.Fungle:
                RedTeamState.Position = new Vector2(-16.9f, 5.5f);
                BlueTeamState.Position = new Vector2(12.5f, 9.6f);
                FireThiefState.Position = new Vector2(-4.2f, -7.9f);
                SakuraTree1.Position = new Vector2(2.3f, 4.3f);
                SakuraTree2.Position = new Vector2(20.9f, 13.4f);
                Tree1.Position = new Vector2(-17.8f, -7.3f);
                Tree2.Position = new Vector2(9.2f, -11.8f);
                Tree3.Position = new Vector2(21.8f, -7.2f);
                Tree4.Position = new Vector2(21.9f, 3.2f);
                break;
            case MapNames.Dleks:
                RedTeamState.Position = new Vector2(9.0f, -4.0f);
                BlueTeamState.Position = new Vector2(-4.0f, -15.5f);
                FireThiefState.Position = new Vector2(-4.5f, -7.9f);
                SakuraTree1.Position = new Vector2(-9.3f, 1.0f);
                SakuraTree2.Position = new Vector2(17.0f, -13.5f);
                Tree1.Position = new Vector2(20.5f, -5.5f);
                Tree2.Position = new Vector2(1.0f, 3.0f);
                Tree3.Position = new Vector2(7.5f, -8.8f);
                Tree4.Position = new Vector2(-16.5f, -4.8f);
                break;
        }
    }

    public static void Add()
    {
        StartedAt = GetTimeStamp();
        RpcSyncBonfireNightStates();
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInGame || GameStates.IsEnded) return;
            RedTeamState.Bonfire = new(RedTeamState.Position, 1);
            BlueTeamState.Bonfire = new(BlueTeamState.Position, 2);
            if (FireThief) FireThiefState.Bonfire = new(FireThiefState.Position, 3);
            SakuraTree1.SakuraTree = new(SakuraTree1.Position);
            SakuraTree2.SakuraTree = new(SakuraTree2.Position);
            Tree1.Tree = new(Tree1.Position);
            Tree2.Tree = new(Tree2.Position);
            Tree3.Tree = new(Tree3.Position);
            Tree4.Tree = new(Tree4.Position);

            foreach (var pc in Main.EnumerateAlivePlayerControls())
            {
                if (pc.Is(CustomRoles.RWoodCollector))
                {
                    pc.SetColor(0);

                    var message = new RpcSetColorMessage(pc.NetId, pc.Data.NetId, 0);
                    RpcUtils.LateBroadcastReliableMessage(message);
                    if (GetActiveMapName() is not MapNames.Airship) pc.RpcTeleport(RedTeamState.Position);
                }
                else if (pc.Is(CustomRoles.BWoodCollector))
                {
                    pc.SetColor(1);

                    var message = new RpcSetColorMessage(pc.NetId, pc.Data.NetId, 1);
                    RpcUtils.LateBroadcastReliableMessage(message);
                    if (GetActiveMapName() is not MapNames.Airship) pc.RpcTeleport(BlueTeamState.Position);
                }
                else
                {
                    pc.SetColor(15);

                    var message = new RpcSetColorMessage(pc.NetId, pc.Data.NetId, 15);
                    RpcUtils.LateBroadcastReliableMessage(message);
                    if (GetActiveMapName() is not MapNames.Airship) pc.RpcTeleport(FireThiefState.Position);
                }
                pc.SetKillCooldown(KillCooldown.GetFloat());
            }
        }, 3f, "BonfireNight Add");
    }

    public static void RpcSyncBonfireNightStates()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(StartedAt.ToString());
        writer.Write(BonfireState.Item1);
        writer.Write(BonfireState.Item2);
        writer.Write(BonfireState.Item3);
        writer.Write(FireThief);
        var sender = new RpcSyncBonfireNightStates(PlayerControl.LocalPlayer.NetId, writer);
        RpcUtils.LateBroadcastReliableMessage(sender);
    }

    public static void HandleSyncBonfireNightStates(MessageReader reader)
    {
        var start = reader.ReadString();
        if (!long.TryParse(start, out StartedAt))
        {
            Logger.Error("Failed to parse StartedAt timestamp from " + start, "HandleSyncBonfireNightStates");
        }
        BonfireState.Item1 = reader.ReadInt32();
        BonfireState.Item2 = reader.ReadInt32();
        BonfireState.Item3 = reader.ReadInt32();
        FireThief = reader.ReadBoolean();
    }

    public static void SelectRoles()
    {
        var random = IRandom.Instance;
        var AllPlayers = Main.EnumeratePlayerControls().Shuffle(random).ToList();
        foreach (var player in Main.EnumeratePlayerControls())
        {
            if (Main.EnableGM.Value && player.IsHost())
            {
                RoleAssign.RoleResult[player.PlayerId] = CustomRoles.GM;
                AllPlayers.Remove(player);
                continue;
            }
            else if (TagManager.AssignGameMaster(player.FriendCode))
            {
                RoleAssign.RoleResult[player.PlayerId] = CustomRoles.GM;
                AllPlayers.Remove(player);
                Logger.Info($"Assign Game Master due to tag for [{player.PlayerId}]{player.GetRealName()}", "TagManager");
                continue;
            }
            else if (RoleAssign.SetRoles.TryGetValue(player.PlayerId, out var role) && role == CustomRoles.GM)
            {
                RoleAssign.RoleResult[player.PlayerId] = CustomRoles.GM;
                AllPlayers.Remove(player);
                Logger.Info($"Assign Game Master due to tag for [{player.PlayerId}]{player.GetRealName()}", "SetRoles");
                continue;
            }
        }
        var fireThief = AllPlayers.Count % 2 != 0 ? 1 : 0;
        var redTeam = (int)(AllPlayers.Count * 0.5);
        foreach (var pc in AllPlayers)
        {
            if (fireThief > 0)
            {
                RoleAssign.RoleResult[pc.PlayerId] = CustomRoles.FireThief;
                FireThief = true;
                fireThief--;
                continue;
            }
            else if (redTeam > 0)
            {
                RoleAssign.RoleResult[pc.PlayerId] = CustomRoles.RWoodCollector;
                redTeam--;
                continue;
            }
            RoleAssign.RoleResult[pc.PlayerId] = CustomRoles.BWoodCollector;
        }
    }

    public static string GetGameState()
    {
        StringBuilder builder = new();
        builder.Append(ColorString(Color.red, GetString("RedTeamWoodNum")) + $": {BonfireState.Item1}");
        builder.AppendLine();
        builder.Append(ColorString(Color.blue, GetString("BlueTeamWoodNum")) + $": {BonfireState.Item2}");
        if (FireThief)
        {
            builder.AppendLine();
            builder.Append(ColorString(Color.gray, GetString("FireThiefWoodNum")) + $": {BonfireState.Item3}");
        }
        return builder.ToString();
    }

    public static void AppendBonfireNightKcount(StringBuilder builder)
    {
        int RedCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.RWoodCollector));
        int BlueCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.BWoodCollector));
        int fireThiefCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.FireThief));

        builder.Append(string.Format(GetString("Remaining.BonfireNightRed"), RedCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.BonfireNightBlue"), BlueCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.BonfireNightFireThief"), fireThiefCount));
    }

    public static void SetWinner(string a)
    {
        switch (a)
        {
            case "r":
                Draw.Item1 = GetString("RedTeam") + GetString("Wins");
                Draw.Item2 = "ff0000";
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RedTeam);
                Main.EnumeratePlayerControls().Where(x => x.Is(CustomRoles.RWoodCollector)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
                break;
            case "b":
                Draw.Item1 = GetString("BlueTeam") + GetString("Wins");
                Draw.Item2 = "0000ff";
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BlueTeam);
                Main.EnumeratePlayerControls().Where(x => x.Is(CustomRoles.BWoodCollector)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
                break;
            case "f":
                Draw.Item1 = GetString("FireThief") + GetString("Wins");
                Draw.Item2 = "a9a9a9";
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.FireThief);
                Main.EnumeratePlayerControls().Where(x => x.Is(CustomRoles.FireThief)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
                break;
            case "d":
                Draw.Item1 = GetString("Draw");
                Draw.Item2 = "808080";
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                Main.EnumeratePlayerControls().Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
                break;   
        }

        Main.DoBlockNameChange = true;
    }

    public struct TeamState
    {
        public Vector2 Position;
        internal Bonfire Bonfire;
    }

    public struct TreeState
    {
        public int Wood;
        public Vector2 Position;
        internal Tree Tree;
        public long Grow;
    }

    public struct SakuraTreeState
    {
        public int Wood;
        public Vector2 Position;
        internal SakuraTree SakuraTree;
        public long Grow;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdateInGameModeBonfireNightPatch
    {
        private static long LastFixedUpdate;
        public static void Postfix()
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.BonfireNight) return;

            var now = GetTimeStamp();

            if (LastFixedUpdate == now) return;
            LastFixedUpdate = now;

            if (!AmongUsClient.Instance.AmHost) return;

            if ((StartedAt + GameTime.GetInt() - TimeStamp) <= 60)
            {
                NotifyRoles();
            }

            var maxWood = CachedMaxWoodRefresh;
            var growTime = CachedWoodGrowthTime;

            Tree1 = UpdateTree(Tree1, maxWood, growTime);
            Tree2 = UpdateTree(Tree2, maxWood, growTime);
            Tree3 = UpdateTree(Tree3, maxWood, growTime);
            Tree4 = UpdateTree(Tree4, maxWood, growTime);

            var maxSakura = CachedMaxSakuraRefresh;
            var sakuraGrow = CachedSakuraGrowthTime;

            SakuraTree1 = UpdateSakuraTree(SakuraTree1, maxSakura, sakuraGrow);
            SakuraTree2 = UpdateSakuraTree(SakuraTree2, maxSakura, sakuraGrow);
        }

        private static TreeState UpdateTree(TreeState tree, int maxWood, int growTime)
        {
            if (tree.Wood >= maxWood) return tree;

            if (tree.Grow + growTime > TimeStamp) return tree;

            tree.Wood++;
            tree.Grow = GetTimeStamp();
            if (tree.Wood == 1)
                tree.Tree = new Tree(tree.Position);
            return tree;
        }

        private static SakuraTreeState UpdateSakuraTree(SakuraTreeState tree, int maxWood, int growTime)
        {
            if (tree.Wood >= maxWood) return tree;

            if (tree.Grow + growTime > TimeStamp) return tree;

            tree.Wood++;
            tree.Grow = GetTimeStamp();
            if (tree.Wood == 1)
                tree.SakuraTree = new SakuraTree(tree.Position);
            return tree;
        }
    }
}

class BonfireNightGameEndPredicate : GameEndPredicate
{
    public override bool CheckForEndGame(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;

        if (!Main.AllPlayerControls.Any(x => x.Is(CustomRoles.BWoodCollector)) && !Main.AllPlayerControls.Any(x => x.Is(CustomRoles.FireThief)))
        {
            reason = GameOverReason.ImpostorDisconnect;
            BonfireNight.SetWinner("r");
            return true;
        }

        if (!Main.AllPlayerControls.Any(x => x.Is(CustomRoles.RWoodCollector)) && !Main.AllPlayerControls.Any(x => x.Is(CustomRoles.FireThief)))
        {
            reason = GameOverReason.ImpostorDisconnect;
            BonfireNight.SetWinner("b");
            return true;
        }

        if (!Main.AllPlayerControls.Any(x => x.Is(CustomRoles.BWoodCollector)) && !Main.AllPlayerControls.Any(x => x.Is(CustomRoles.RWoodCollector)))
        {
            reason = GameOverReason.ImpostorDisconnect;
            BonfireNight.SetWinner("f");
            return true;
        }

        if (BonfireNight.BonfireState.Item1 >= BonfireNight.WoodToWin.GetInt())
        {
            BonfireNight.SetWinner("r");
            return true;
        }

        if (BonfireNight.BonfireState.Item2 >= BonfireNight.WoodToWin.GetInt())
        {
            BonfireNight.SetWinner("b");
            return true;
        }

        if (BonfireNight.BonfireState.Item3 >= BonfireNight.WoodToWin.GetInt())
        {
            BonfireNight.SetWinner("f");
            return true;
        }

        if (BonfireNight.StartedAt != 0 && GetTimeStamp() - BonfireNight.StartedAt >= BonfireNight.GameTime.GetInt())
        {
            reason = GameOverReason.CrewmatesByTask;
            var (a, b, c) = BonfireNight.BonfireState;

            var maxIndex = (a, b, c) switch
            {
                var (x, y, z) when x > y && x > z => 0,
                var (x, y, z) when y > x && y > z => 1,
                var (x, y, z) when z > x && z > y => 2,
                _ => -1
            };

            switch (maxIndex)
            {
                case 0:
                    BonfireNight.SetWinner("r");
                    break;
                case 1:
                    BonfireNight.SetWinner("b");
                    break;
                case 2:
                    BonfireNight.SetWinner("f");
                    break;
                case -1:
                    BonfireNight.SetWinner("d");
                    break;
            }
            return true;
        }

        return false;
    }
}

public class FireThief : RoleBase
{
    public override CustomRoles Role => CustomRoles.FireThief;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    public override bool IsDesyncRole => true;

    public static readonly Dictionary<byte, int> WoodNum = [];
    public static readonly Dictionary<byte, int> MaxNum = [];
    public long ReviveTime = 0;

    public override bool CanUseSabotage(PlayerControl pc)
    {
        return false;
    }

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
    {
        return false;
    }

    public override bool CanUseKillButton(PlayerControl pc) => WoodNum[pc.PlayerId] == 0;

    public override bool CanUseImpostorVentButton(PlayerControl pc) => WoodNum[pc.PlayerId] == 0;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = BonfireNight.KillCooldown.GetFloat();

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        return false;
    }

    public override void Init()
    {
        WoodNum.Clear();
        MaxNum.Clear();
    }

    public override void Add(byte playerId)
    {
        var player = GetPlayerById(playerId);
        WoodNum.TryAdd(playerId, 0);
        MaxNum.TryAdd(playerId, player.Is(CustomRoles.FireThief) ? BonfireNight.FireThiefMaximumWoodHoldingQuantity.GetInt() : BonfireNight.MaximumWoodHoldingQuantity.GetInt());
        ReviveTime = 0;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = BonfireNight.PickUpWoodCooldown.GetFloat();
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;

        if (!player.IsAlive())
        {
            if (ReviveTime + BonfireNight.ReviveCooldown.GetInt() < nowTime)
            {
                player.RpcRevive();
                switch (player.GetCustomRole())
                {
                    case CustomRoles.RWoodCollector:
                        player.RpcTeleport(BonfireNight.RedTeamState.Position);
                        break;
                    case CustomRoles.BWoodCollector:
                        player.RpcTeleport(BonfireNight.BlueTeamState.Position);
                        break;
                    case CustomRoles.FireThief:
                        player.RpcTeleport(BonfireNight.FireThiefState.Position);
                        break;
                }
            }
        }

        var changed = false;
        if (player.Is(CustomRoles.RWoodCollector) && GetDistance(player.GetCustomPosition(), BonfireNight.RedTeamState.Position) <= 1f && WoodNum[player.PlayerId] > 0)
        {
            BonfireNight.BonfireState.Item1 += WoodNum[player.PlayerId];
            WoodNum[player.PlayerId] = 0;
            changed = true;
        }

        if (player.Is(CustomRoles.BWoodCollector) && GetDistance(player.GetCustomPosition(), BonfireNight.BlueTeamState.Position) <= 1f && WoodNum[player.PlayerId] > 0)
        {
            BonfireNight.BonfireState.Item2 += WoodNum[player.PlayerId];
            WoodNum[player.PlayerId] = 0;
            changed = true;
        }

        if (player.Is(CustomRoles.FireThief) && GetDistance(player.GetCustomPosition(), BonfireNight.FireThiefState.Position) <= 1f && WoodNum[player.PlayerId] > 0)
        {
            BonfireNight.BonfireState.Item3 += WoodNum[player.PlayerId];
            WoodNum[player.PlayerId] = 0;
            changed = true;
        }

        if (changed)
        {
            SendRPC(player);
            BonfireNight.RpcSyncBonfireNightStates();
            NotifyRoles();
        }
    }

    public void SendRPC(PlayerControl player)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(WoodNum[player.PlayerId]);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        WoodNum[pc.PlayerId] = reader.ReadInt32();
    }

    /*public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId)
        {
            var color = new Color32(244, 164, 96, byte.MaxValue);
            return ColorString(color, $"({WoodNum[seer.PlayerId]}/{MaxNum[seer.PlayerId]})");
        }

        return "";
    }*/

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //if (seer.PlayerId == seen.PlayerId) return "";

        var color = new Color32(244, 164, 96, byte.MaxValue);
        return ColorString(color, $"({WoodNum[seen.PlayerId]}/{MaxNum[seen.PlayerId]})");
    }

    public override bool OnCheckVanish(PlayerControl phantom)
    {
        if (BonfireNight.StartedAt + 3 > TimeStamp) return false;

        var position = phantom.GetCustomPosition();

        BonfireNight.Tree1 = TryCollect(phantom, BonfireNight.Tree1, position, MaxNum[phantom.PlayerId]);
        BonfireNight.Tree2 = TryCollect(phantom, BonfireNight.Tree2, position, MaxNum[phantom.PlayerId]);
        BonfireNight.Tree3 = TryCollect(phantom, BonfireNight.Tree3, position, MaxNum[phantom.PlayerId]);
        BonfireNight.Tree4 = TryCollect(phantom, BonfireNight.Tree4, position, MaxNum[phantom.PlayerId]);
        BonfireNight.SakuraTree1 = TryCollect(phantom, BonfireNight.SakuraTree1, position, MaxNum[phantom.PlayerId]);
        BonfireNight.SakuraTree2 = TryCollect(phantom, BonfireNight.SakuraTree2, position, MaxNum[phantom.PlayerId]);

        if (phantom.Is(CustomRoles.FireThief) && BonfireNight.FireThiefCanStealWood.GetBool() && WoodNum[phantom.PlayerId] < MaxNum[phantom.PlayerId])
        {
            if (GetDistance(BonfireNight.RedTeamState.Position, position) <= 1f && BonfireNight.BonfireState.Item1 > 0)
            {
                WoodNum[phantom.PlayerId]++;
                BonfireNight.BonfireState.Item1--;
                SendRPC(phantom);
                BonfireNight.RpcSyncBonfireNightStates();
                NotifyRoles();
            }

            if (GetDistance(BonfireNight.BlueTeamState.Position, position) <= 1f && BonfireNight.BonfireState.Item2 > 0)
            {
                WoodNum[phantom.PlayerId]++;
                BonfireNight.BonfireState.Item2--;
                SendRPC(phantom);
                BonfireNight.RpcSyncBonfireNightStates();
                NotifyRoles();
            }
        }

        return false;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetCustomRole() == target.GetCustomRole()) return false;

        if (target.GetRoleClass() is FireThief ft)
        {
            if (ft.ReviveTime + BonfireNight.ReviveCooldown.GetInt() + BonfireNight.InvincibilityCooldownAfterRevive.GetInt() > TimeStamp) return false;
        }

        return true;
    }

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (WoodNum.TryGetValue(target.PlayerId, out var num) && num > 0)
        {
            WoodNum[killer.PlayerId] += num;
            WoodNum[target.PlayerId] = 0;
            if (WoodNum[killer.PlayerId] > MaxNum[killer.PlayerId])
            {
                WoodNum[killer.PlayerId] = MaxNum[killer.PlayerId];
            }
            SendRPC(killer);
            NotifyRoles();
        }
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        ReviveTime = GetTimeStamp();
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var time = BonfireNight.StartedAt + BonfireNight.GameTime.GetInt() - GetTimeStamp();
        var timeText = $"{GetString("BonfireNight_GameTime")}: {time}s";
    
        return seer.IsModded() ? timeText : $"{BonfireNight.GetGameState()}\n{timeText}";
    }

    private BonfireNight.TreeState TryCollect(PlayerControl player, BonfireNight.TreeState tree, Vector2 pos, int max)
    {
        if (WoodNum[player.PlayerId] >= max) return tree;
        if (GetDistance(tree.Position, pos) <= 1f && tree.Wood > 0)
        {
            WoodNum[player.PlayerId]++;
            tree.Wood--;
            if (tree.Wood <= 0)
            {
                tree.Tree.Despawn();
            }
        }
        SendRPC(player);
        NotifyRoles();
        return tree;
    }
    private BonfireNight.SakuraTreeState TryCollect(PlayerControl player, BonfireNight.SakuraTreeState tree, Vector2 pos, int max)
    {
        if (WoodNum[player.PlayerId] >= max) return tree;
        if (GetDistance(tree.Position, pos) <= 1f && tree.Wood > 0)
        {
            WoodNum[player.PlayerId]++;
            tree.Wood--;
            if (tree.Wood <= 0)
            {
                tree.SakuraTree.Despawn();
            }
        }
        SendRPC(player);
        NotifyRoles();
        return tree;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(GetString("PickUp"));
        hud.AbilityButton.SetUsesRemaining(MaxNum[playerId] - WoodNum[playerId]);
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => seer.IsAlive();
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Main.roleColors[target.GetCustomRole()];
}
