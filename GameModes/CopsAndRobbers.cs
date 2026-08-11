using System.Text;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using TMPro;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Core;
using TONE.Roles.Core.AssignManager;
using UnityEngine;
using static TONE.RoleBase;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE;

internal class CopsAndRobbers : GameModeBase
{
    public override CustomGameMode GameMode => CustomGameMode.CopsAndRobbers;
    private const int Id = 67_230_001;
    public override bool OpeningHours => Main.IsSummer;

    public static OptionItem GameTime;
    public static OptionItem ShowChatInGame;

    public static OptionItem NumCops;
    public static OptionItem CaptureCooldown;
    public static OptionItem CaptureDuration;
    public static OptionItem CopVision;
    public static OptionItem CopCanKillRobber;
    public static OptionItem CopReviveCooldown;
    public static OptionItem CopInvincibilityCooldownAfterRevive;

    public static OptionItem RobberJewels;
    public static OptionItem InitialJewels;
    public static OptionItem RobberVision;
    public static OptionItem RobberCanKillCop;
    public static OptionItem RobberKillCooldown;
    public static OptionItem RobberReviveCooldown;
    public static OptionItem RobberInvincibilityCooldownAfterRevive;
    public static OptionItem JewelSpawnCooldown;
    public static OptionItem RobberJailbreakCooldown;

    public const int MaxJewels = 6;
    public static long StartedAt = 0;
    public static int NumJewels;
    public static int RefreshTime;
    public static readonly List<byte> RobberList = [];
    public static readonly List<byte> CaptureList = [];
    public static readonly List<byte> JewelList = [];
    public static readonly List<JewelState> Jewel = [];
    public static readonly List<Vector2> AllLocation = [];
    public static long LastSpawnTime = 0;
    public static PrisonState Prison;
    public static BagState Bag;

    public override void SetupCustomOption()
    {
        TextOptionItem.Create(10000040, "MenuTitle.C&R", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(135, 206, 250, byte.MaxValue));

        GameTime = IntegerOptionItem.Create(Id + 1, "C&R_GameTime", (60, 600, 15), 300, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(135, 206, 250, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        ShowChatInGame = BooleanOptionItem.Create(Id + 2, "C&R_ShowChatInGame", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(135, 206, 250, byte.MaxValue));

        /*********** Cops ***********/
        TextOptionItem.Create(Id + 10, "MenuTitle.Cop", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));
        
        NumCops = IntegerOptionItem.Create(Id + 11, "C&R_NumCops", new(1, 10, 1), 6, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Players);
        CaptureCooldown = FloatOptionItem.Create(Id + 12, "C&R_CaptureCooldown", new(5f, 60f, 2.5f), 10f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CaptureDuration = FloatOptionItem.Create(Id + 13, "C&R_CaptureDuration", new(0f, 60f, 1f), 3f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CopVision = FloatOptionItem.Create(Id + 14, "C&R_CopVision", new(0.25f, 5f, 0.25f), 0.75f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier);
        CopCanKillRobber = BooleanOptionItem.Create(Id + 15, "C&R_CopCanKillRobber", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue));
        CopReviveCooldown = IntegerOptionItem.Create(Id + 16, "C&R_CopReviveCooldown", new(0, 60, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        CopInvincibilityCooldownAfterRevive = IntegerOptionItem.Create(Id + 17, "C&R_CopInvincibilityCooldownAfterRevive", new(0, 60, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(0, 123, 255, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);

        /*********** Robbers ***********/
        TextOptionItem.Create(Id + 20, "MenuTitle.Robber", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue));

        RobberJewels = IntegerOptionItem.Create(Id + 21, "C&R_RobberJewels", new(1, 50, 1), 18, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Pieces);
        InitialJewels = IntegerOptionItem.Create(Id + 22, "C&R_InitialJewels", new(1, 6, 1), 6, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Pieces);
        RobberVision = FloatOptionItem.Create(Id + 23, "C&R_RobberVision", new(0.25f, 5f, 0.25f), 0.75f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier);
        RobberCanKillCop = BooleanOptionItem.Create(Id + 24, "C&R_RobberCanKillCop", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue));
        RobberKillCooldown = FloatOptionItem.Create(Id + 25, GeneralOption.KillCooldown, new(5f, 60f, 2.5f), 25f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetParent(RobberCanKillCop);
        RobberReviveCooldown = IntegerOptionItem.Create(Id + 26, "C&R_RobberReviveCooldown", new(0, 60, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        RobberInvincibilityCooldownAfterRevive = IntegerOptionItem.Create(Id + 27, "C&R_RobberInvincibilityCooldownAfterRevive", new(0, 60, 1), 5, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        JewelSpawnCooldown = IntegerOptionItem.Create(Id + 28, "C&R_JewelSpawnCooldown", new(5, 100, 5), 10, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        RobberJailbreakCooldown = IntegerOptionItem.Create(Id + 29, "C&R_RobberJailbreakCooldown", new(5, 100, 5), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CopsAndRobbers)
            .SetColor(new Color32(255, 140, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        StartedAt = GetTimeStamp();
        NumJewels = 0;
        RefreshTime = JewelSpawnCooldown.GetInt();
        RobberList.Clear();
        CaptureList.Clear();
        JewelList.Clear();
        Jewel.Clear();
        AllLocation.Clear();
        LastSpawnTime = 0;

        var map = GetActiveMapName();
        (Prison.Position, Bag.Position) = map switch
        {
            MapNames.Skeld => (new Vector2(-10.2f, 1.18f), new Vector2(-1.31f, -16.25f)),
            MapNames.MiraHQ => (new Vector2(1.8f, -1f), new Vector2(17.75f, 11.5f)),
            MapNames.Polus => (new Vector2(8.18f, -7.4f), new Vector2(30f, -15.75f)),
            MapNames.Airship => (new Vector2(-18.5f, 0.75f), new Vector2(7.15f, -14.5f)),
            MapNames.Fungle => (new Vector2(-22.5f, -0.5f), new Vector2(20f, 11f)),
            MapNames.Dleks => (new Vector2(10.2f, 1.18f), new Vector2(1.31f, -16.25f)),
            _ => (Vector2.zero, Vector2.zero)
        };
    }

    public override void Add()
    {
        StartedAt = GetTimeStamp();
        NumJewels = 0;
        Jewel.Clear();
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInGame || GameStates.IsEnded) return;
            LastSpawnTime = GetTimeStamp();
            Prison.Prison = new(Prison.Position);
            Bag.Bag = new(Bag.Position);
            AllLocation.AddRange(GetAllRandomSpawnLocation());

            for (var i = 0; i < InitialJewels.GetInt(); i++)
            {
                var location = AllLocation.RandomElement();
                Jewel.Add(new JewelState { Position = location, Jewel = new(location)});
                AllLocation.Remove(location);
            }

            foreach (var pc in Main.EnumerateAlivePlayerControls())
            {
                if (pc.Is(CustomRoles.Cop))
                {
                    pc.SetColor(1);

                    var message = new RpcSetColorMessage(pc.NetId, pc.Data.NetId, 1);
                    RpcUtils.LateBroadcastReliableMessage(message);
                    if (GetActiveMapName() is not MapNames.Airship) pc.RpcTeleport(Prison.Position);
                }
                else if (pc.Is(CustomRoles.Robber))
                {
                    pc.SetColor(6);

                    var message = new RpcSetColorMessage(pc.NetId, pc.Data.NetId, 6);
                    RpcUtils.LateBroadcastReliableMessage(message);
                    if (GetActiveMapName() is not MapNames.Airship) pc.RpcTeleport(Bag.Position);
                }
                pc.SetKillCooldown(CaptureCooldown.GetFloat());
            }
        }, 3f, "C&R Add");
    }

    public override void SelectRoles()
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
        var cops = NumCops.GetInt();
        foreach (var pc in AllPlayers)
        {
            if (cops > 0)
            {
                RoleAssign.RoleResult[pc.PlayerId] = CustomRoles.Cop;
                cops--;
                continue;
            }
            RoleAssign.RoleResult[pc.PlayerId] = CustomRoles.Robber;
            RobberList.Add(pc.PlayerId);
        }
    }

    public override string GetGameState(string taskText = null, bool forGameEnd = false)
    {
        StringBuilder builder = new();

        int RobberCount = RobberList.Count;
        int JewelCount = NumJewels;
        int CapturedCount = CaptureList.Count;
        var time = StartedAt + GameTime.GetInt() - GetTimeStamp();

        if (!forGameEnd)
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("<size=80%>");
        }
        else
        {
            builder.AppendLine();
        }

        builder.Append(string.Format(GetString("Remaining.C&R.Jewel"), JewelCount, RobberJewels.GetInt()));
        builder.AppendLine();
        builder.Append(string.Format(GetString("Remaining.C&R.Capture"), CapturedCount, RobberCount));
        if (!forGameEnd)
        {
            builder.AppendLine();
            builder.Append($"{GetString("C&R_GameTime")}: {time}s");
        }

        if (!forGameEnd) builder.Append("</size>");

        return builder.ToString();
    }

    public override void SummaryText(StringBuilder sb, List<byte> cloneRoles, bool sendMessage = false)
    {
        base.SummaryText(sb, cloneRoles, sendMessage);
        sb.Append(GetGameState(null, true));
    }

    public override void SetPredicate() => GameEndCheckerForNormal.predicate = new CopsAndRobbersGameEndPredicate();

    public override void AppendKcount(StringBuilder builder)
    {
        int CopCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Cop));
        int RobberCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Robber));

        builder.Append(string.Format(GetString("Remaining.C&R.Cop"), CopCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.C&R.Robber"), RobberCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.C&R.Jewel"), NumJewels, RobberJewels.GetInt()));
        builder.Append(string.Format("\n\r" + GetString("Remaining.C&R.Capture"), CaptureList.Count, RobberCount));
    }

    public static void OnPlayerLeft(byte playerId)
    {
        if (CaptureList.Contains(playerId)) CaptureList.Remove(playerId);
        if (RobberList.Contains(playerId)) RobberList.Remove(playerId);
    }

    public struct JewelState
    {
        public Vector2 Position;
        internal Jewel Jewel;
    }

    public struct PrisonState
    {
        public Vector2 Position;
        internal Prison Prison;
    }

    public struct BagState
    {
        public Vector2 Position;
        internal Bag Bag;
    }

    private static long LastFixedUpdate;

    public static void FixedUpdate()
    {
        var now = GetTimeStamp();

        if (LastFixedUpdate == now) return;
        LastFixedUpdate = now;

        if ((StartedAt + GameTime.GetInt() - TimeStamp) <= 60)
        {
            NotifyRoles();
        }

        if (LastSpawnTime == 0) return;
        if (Jewel.Count >= MaxJewels) return;

        if (LastSpawnTime + RefreshTime < now)
        {
            LastSpawnTime = now;
            if (!AllLocation.Any())
            {
                var allPossible = GetAllRandomSpawnLocation();
                var occupiedPositions = Jewel.Select(j => j.Position).ToHashSet();
                var available = allPossible.Where(p => !occupiedPositions.Contains(p)).ToList();
                AllLocation.AddRange(available);
            }
            var location = AllLocation.RandomElement();
            Jewel.Add( new JewelState { Position = location, Jewel = new (location) });
            AllLocation.Remove(location);
        }
    }
}

class CopsAndRobbersGameEndPredicate : GameEndPredicate
{
    public override bool CheckForEndGame(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;

        if (Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Robber)) == CopsAndRobbers.CaptureList.Count ||
            !Main.AllAlivePlayerControls.Any(x => x.Is(CustomRoles.Robber)) ||
            CopsAndRobbers.StartedAt != 0 && GetTimeStamp() - CopsAndRobbers.StartedAt >= CopsAndRobbers.GameTime.GetInt())
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Cop);
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Cop)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
            Main.DoBlockNameChange = true;
            return true;
        }

        if (CopsAndRobbers.NumJewels >= CopsAndRobbers.RobberJewels.GetInt() ||
            !Main.AllAlivePlayerControls.Any(x => x.Is(CustomRoles.Cop)))
        {
            reason = GameOverReason.CrewmatesByTask;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Robber);
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Robber)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
            Main.DoBlockNameChange = true;
            return true;
        }

        return false;
    }
}

public class Cop : RoleBase
{
    public override CustomRoles Role => CustomRoles.Cop;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    public override bool IsDesyncRole => false;

    public (bool, long, PlayerControl) CaptureState = (false, 0, null);
    public bool CaptureMode = true;
    public long ReviveTime = 0;

    public override void Add(byte playerId)
    {
        CaptureState = (false, 0, null);
        CaptureMode = true;
        ReviveTime = 0;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, CopsAndRobbers.CopVision.GetFloat());

        AURoleOptions.ShapeshifterCooldown = 1f;
    }

    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => false;

    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CopsAndRobbers.CaptureCooldown.GetFloat();

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        return false;
    }

    public override void UnShapeShiftButton(PlayerControl player)
    {
        if (!CopsAndRobbers.CopCanKillRobber.GetBool()) return;
        CaptureMode = !CaptureMode;
        if (player.IsNonHostModdedClient()) SendRPC(player, 0);
        NotifyRoles(SpecifyTarget: player);
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (CopsAndRobbers.CaptureList.Contains(target.PlayerId) || target.Is(CustomRoles.Cop)) return false;

        if (target.GetRoleClass() is Robber r)
        {
            if (r.ReviveTime + CopsAndRobbers.RobberReviveCooldown.GetInt() + CopsAndRobbers.RobberInvincibilityCooldownAfterRevive.GetInt() > TimeStamp) return false;
        }

        if (!CaptureState.Item1 && CaptureMode)
        {
            killer.SetKillCooldown(CopsAndRobbers.CaptureDuration.GetInt());
            CaptureState = (true, GetTimeStamp(), target);
            NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            return false;
        }

        if (CopsAndRobbers.JewelList.Contains(target.PlayerId))
        {
            CopsAndRobbers.JewelList.Remove(target.PlayerId);
        }

        SendRPC(target, 2);
        NotifyRoles(SpecifyTarget: target);

        return true;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!player.IsAlive())
        {
            if (ReviveTime + CopsAndRobbers.CopReviveCooldown.GetInt() < nowTime)
            {
                player.RpcRevive();
                player.RpcTeleport(CopsAndRobbers.Prison.Position);
            }
        }

        if (CaptureState.Item1 && player.IsAlive())
        {
            var target = CaptureState.Item3;
            if (!target.IsAlive())
            {
                CaptureState = (false, 0, null);
                NotifyRoles(SpecifySeer: player, SpecifyTarget: target, ForceLoop: true);
            }
            else
            {
                float range = ExtendedPlayerControl.GetKillDistances() + 0.5f;
                float distance = GetDistance(player.GetCustomPosition(), CaptureState.Item3.GetCustomPosition());

                if (distance <= range)
                {
                    if (CaptureState.Item2 + CopsAndRobbers.CaptureDuration.GetInt() < nowTime)
                    {
                        CapturePlayer(player, target);
                        CaptureState = (false, 0, null);
                        NotifyRoles(SpecifySeer: player, SpecifyTarget: target, ForceLoop: true);
                    }
                }
                else
                {
                    CaptureState = (false, 0, null);
                    NotifyRoles(SpecifySeer: player, SpecifyTarget: target, ForceLoop: true);
                    Logger.Info($"Canceled: {player.GetNameWithRole()}", "Cop");
                }
            }
        }
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        ReviveTime = GetTimeStamp();
    }

    public void CapturePlayer(PlayerControl killer, PlayerControl target)
    {
        if (CopsAndRobbers.JewelList.Contains(target.PlayerId))
        {
            CopsAndRobbers.JewelList.Remove(target.PlayerId);
        }

        CopsAndRobbers.CaptureList.Add(target.PlayerId);
        target.MarkDirtySettings();
        target.RpcTeleport(CopsAndRobbers.Prison.Position);
        var CapturedSkin = new NetworkedPlayerInfo.PlayerOutfit();
        CapturedSkin.Set(target.GetRealName(isMeeting: true),
                    5, //yellow
                    "hat_tombstone", //hat
                    "skin_prisoner", //skin 
                    "visor_pk01_DumStickerVisor", //visor
                    target.CurrentOutfit.PetId,
                    target.CurrentOutfit.NamePlateId);
        killer.SetKillCooldown(CopsAndRobbers.CaptureCooldown.GetFloat(), target, true);
        killer.ResetKillCooldown();
        killer.Notify(GetString("C&R.CaptureTarget"));

        var popup = GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab;

        var newPopUp = Object.Instantiate(popup, HudManager.Instance.transform.parent);

        newPopUp.gameObject.transform.GetChild(0).GetComponent<TextTranslatorTMP>().enabled = false;
        newPopUp.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("C&R.PrefabCaptureTarget");
        newPopUp.Show(target, 0);

        SendRPC(target, 1);

        target.SetNewOutfit(CapturedSkin);

        NotifyRoles(SpecifyTarget: target);
    }

    public void SendRPC(PlayerControl target, int id = 0)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(target.PlayerId);
        writer.Write(id);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var targetId = reader.ReadByte();
        var id = reader.ReadInt32();
        var target = targetId.GetPlayer();

        if (id != 0 && CopsAndRobbers.JewelList.Contains(target.PlayerId)) CopsAndRobbers.JewelList.Remove(target.PlayerId);

        if (id == 0)
        {
            CaptureMode = !CaptureMode;
        }
        else
        {
            var popup = GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab;

            var newPopUp = Object.Instantiate(popup, HudManager.Instance.transform.parent);

            newPopUp.gameObject.transform.GetChild(0).GetComponent<TextTranslatorTMP>().enabled = false;
            newPopUp.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("C&R.PrefabCaptureTarget");
            newPopUp.Show(target, 0);
        }
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var sb = new StringBuilder();
        if (!seer.IsModded())
        {
            sb.Append(new CopsAndRobbers().GetGameState(null, false).Trim());
            sb.AppendLine();
            sb.Append("<size=80%>");
        }
        sb.Append(CaptureMode ? GetString("C&R.CopCaptureMode") : GetString("C&R.CopKillMode"));
        if (!seer.IsModded()) sb.Append("</size>");
        return sb.ToString();
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!seen) return string.Empty;

        if (!isForMeeting && CaptureState.Item1 && CaptureState.Item3 == seen)
            return ColorString(GetRoleColor(CustomRoles.Cop), "△");

        return string.Empty;
    }

    public override string GetProgressText(byte playerId, bool comms) => string.Empty;

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(GetString("ChangeButtonText"));
        if (CaptureMode) hud.KillButton?.OverrideText($"{GetString("CopButtonText")}");
        else hud.KillButton?.OverrideText($"{GetString("KillButtonText")}");
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Main.roleColors[target.GetCustomRole()];
}

public class Robber : RoleBase
{
    public override CustomRoles Role => CustomRoles.Robber;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    public override bool IsDesyncRole => false;

    public long ReviveTime = 0;

    public override void Add(byte playerId)
    {
        ReviveTime = 0;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, CopsAndRobbers.RobberVision.GetFloat());

        Main.AllPlayerSpeed[playerId] = CopsAndRobbers.CaptureList.Contains(playerId) ? Main.MinSpeed : Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        AURoleOptions.PlayerSpeedMod = CopsAndRobbers.CaptureList.Contains(playerId) ? Main.MinSpeed : Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);

        AURoleOptions.ShapeshifterCooldown = 1f;
    }

    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => false;

    public override bool CanUseKillButton(PlayerControl pc) => !CopsAndRobbers.JewelList.Contains(pc.PlayerId) && !CopsAndRobbers.CaptureList.Contains(pc.PlayerId);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CopsAndRobbers.RobberKillCooldown.GetFloat();

    public override bool CanUseImpostorVentButton(PlayerControl pc) => !CopsAndRobbers.JewelList.Contains(pc.PlayerId) && !CopsAndRobbers.CaptureList.Contains(pc.PlayerId);

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        return false;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;

        if (!player.IsAlive())
        {
            if (ReviveTime + CopsAndRobbers.RobberReviveCooldown.GetInt() < nowTime)
            {
                player.RpcRemoveAbilityCD();
                player.RpcAddAbilityCD();
                player.RpcRevive();
                player.RpcTeleport(CopsAndRobbers.Bag.Position);
            }
        }
    }

    public override void UnShapeShiftButton(PlayerControl player)
    {
        if (CopsAndRobbers.CaptureList.Contains(player.PlayerId)) return;

        var index = CopsAndRobbers.Jewel.FindIndex(location => !CopsAndRobbers.JewelList.Contains(player.PlayerId) && GetDistance(player.GetCustomPosition(), location.Position) <= 1f);

        if (index >= 0)
        {
            var location = CopsAndRobbers.Jewel[index];
            CopsAndRobbers.JewelList.Add(player.PlayerId);
            location.Jewel.Despawn();
            player.Notify(GetString("C&R.FindJewel"));
            SendRPC(player, 1);
            NotifyRoles(SpecifyTarget: player);
            CopsAndRobbers.Jewel.RemoveAt(index);
            return;
        }

        if (CopsAndRobbers.JewelList.Contains(player.PlayerId) && GetDistance(player.GetCustomPosition(), CopsAndRobbers.Bag.Position) <= 1f)
        {
            CopsAndRobbers.JewelList.Remove(player.PlayerId);
            CopsAndRobbers.NumJewels++;
            player.Notify(GetString("C&R.StealJewel"));

            var popup = GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab;

            var newPopUp = Object.Instantiate(popup, HudManager.Instance.transform.parent);

            newPopUp.gameObject.transform.GetChild(0).GetComponent<TextTranslatorTMP>().enabled = false;
            newPopUp.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("C&R.PrefabStealJewel");
            newPopUp.Show(player, 0);

            SendRPC(player, 2);
            NotifyRoles(SpecifyTarget: player);
            return;
        }

        if (CopsAndRobbers.CaptureList.Any() && !CopsAndRobbers.CaptureList.Contains(player.PlayerId) && !player.HasAbilityCD() && GetDistance(player.GetCustomPosition(), CopsAndRobbers.Prison.Position) <= 1f)
        {
            var targetId = CopsAndRobbers.CaptureList.RandomElement();
            var target = targetId.GetPlayer();
            CopsAndRobbers.CaptureList.Remove(targetId);
            player.RpcAddAbilityCD();
            player.Notify(GetString("HelpJailbreak"));
            target.Notify(GetString("YouJailbreak"));
            target.MarkDirtySettings();
            Camouflage.PlayerSkins[targetId].ColorId = 6;
            target.SetNewOutfit(Camouflage.PlayerSkins[targetId]);
            target.RpcTeleport(CopsAndRobbers.Bag.Position);

            var popup = GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab;

            var newPopUp = Object.Instantiate(popup, HudManager.Instance.transform.parent);

            newPopUp.gameObject.transform.GetChild(0).GetComponent<TextTranslatorTMP>().enabled = false;
            newPopUp.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("C&R.PrefabJailbreak");
            newPopUp.Show(target, 0);

            SendRPC(player, 3, targetId);
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.GetRoleClass() is Cop c)
        {
            if (c.ReviveTime + CopsAndRobbers.CopReviveCooldown.GetInt() + CopsAndRobbers.CopInvincibilityCooldownAfterRevive.GetInt() > TimeStamp) return false;
        }
        if (target.Is(CustomRoles.Robber)) return false;
        if (CopsAndRobbers.RobberCanKillCop.GetBool()) return true;
        else return false;
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        ReviveTime = GetTimeStamp();
    }

    public void SendRPC(PlayerControl player, int id = 0, byte targetId = 255)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(player.PlayerId);
        writer.Write(id);
        writer.Write(targetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var playerId = reader.ReadByte();
        var id = reader.ReadInt32();
        var targetId = reader.ReadByte();

        var player = playerId.GetPlayer();
        var target = targetId.GetPlayer();

        var popup = GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab;
        var newPopUp = Object.Instantiate(popup, HudManager.Instance.transform.parent);

        switch (id)
        {
            case 1:
                CopsAndRobbers.JewelList.Add(player.PlayerId);
                break;
            case 2:
                CopsAndRobbers.JewelList.Remove(player.PlayerId);
                CopsAndRobbers.NumJewels++;
                newPopUp.gameObject.transform.GetChild(0).GetComponent<TextTranslatorTMP>().enabled = false;
                newPopUp.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("C&R.PrefabStealJewel");
                newPopUp.Show(player, 0);
                break;
            case 3:
                CopsAndRobbers.CaptureList.Remove(targetId);
                if (targetId != 255)
                {
                    newPopUp.gameObject.transform.GetChild(0).GetComponent<TextTranslatorTMP>().enabled = false;
                    newPopUp.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("C&R.PrefabJailbreak");
                    newPopUp.Show(target, 0);
                }
                break;
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId && CopsAndRobbers.JewelList.Contains(seer.PlayerId))
        {
            return ColorString(new Color32(80, 200, 120, byte.MaxValue), "◆");
        }

        return "";
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId) return "";

        if (CopsAndRobbers.JewelList.Contains(seen.PlayerId))
        {
            return ColorString(new Color32(80, 200, 120, byte.MaxValue), "◆");
        }

        return "";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var sb = new StringBuilder();
        if (!seer.IsModded())
        {
            sb.Append(new CopsAndRobbers().GetGameState(null, false).Trim());
            sb.AppendLine();
        }
        sb.Append(GetAbilityTimeDisplay(seer, seen));
        return sb.ToString();
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(GetString("ArchaeologistVentButtonText"));
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Main.roleColors[target.GetCustomRole()];
}
