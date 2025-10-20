using AmongUs.GameOptions;
using Hazel;
using System;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Modules.HazelExtensions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

//EHR - https://github.com/Gurge44/EndlessHostRoles/blob/main/Roles/Impostor/Abyssbringer.cs
internal class AbyssBringer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Abyssbringer;
    const int Id = 31300;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    public static bool ShouldDespawnCNOOnMeeting => (DespawnMode)BlackHoleDespawnMode.GetValue() == DespawnMode.AfterMeeting;

    private static OptionItem BlackHoleCountLimit;
    private static OptionItem BlackHolePlaceCooldown;
    private static OptionItem BlackHoleDespawnMode;
    private static OptionItem BlackHoleDespawnTime;
    private static OptionItem BlackHoleMovesTowardsNearestPlayer;
    private static OptionItem BlackHoleMoveSpeed;
    private static OptionItem BlackHoleRadius;
    public static OptionItem BlackHoleSkin;
    private static OptionItem CanKillImpostors;
    private static OptionItem CanKillTNA;

    private readonly Dictionary<byte, BlackHoleData> BlackHoles = [];

    public override void SetupCustomOption()
    {
        const TabGroup tab = TabGroup.ImpostorRoles;
        const CustomRoles role = CustomRoles.Abyssbringer;
        SetupRoleOptions(Id, tab, role);
        BlackHoleCountLimit = IntegerOptionItem.Create(Id + 16, "BlackHoleCountLimit", new(1, 15, 1), 1, tab, false)
            .SetParent(CustomRoleSpawnChances[role]);
        BlackHolePlaceCooldown = IntegerOptionItem.Create(Id + 10, "BlackHolePlaceCooldown", new(1, 180, 1), 30, tab, false)
            .SetParent(CustomRoleSpawnChances[role])
            .SetValueFormat(OptionFormat.Seconds);
        BlackHoleDespawnMode = StringOptionItem.Create(Id + 11, "BlackHoleDespawnMode", Enum.GetNames<DespawnMode>(), 0, tab, false)
            .SetParent(CustomRoleSpawnChances[role]);
        BlackHoleDespawnTime = IntegerOptionItem.Create(Id + 12, "BlackHoleDespawnTime", new(1, 60, 1), 15, tab, false)
            .SetParent(BlackHoleDespawnMode)
            .SetValueFormat(OptionFormat.Seconds);
        BlackHoleMovesTowardsNearestPlayer = BooleanOptionItem.Create(Id + 13, "BlackHoleMovesTowardsNearestPlayer", true, tab, false)
            .SetParent(CustomRoleSpawnChances[role]);
        BlackHoleMoveSpeed = FloatOptionItem.Create(Id + 14, "BlackHoleMoveSpeed", new(0.25f, 10f, 0.25f), 1f, tab, false)
            .SetParent(BlackHoleMovesTowardsNearestPlayer);
        BlackHoleRadius = FloatOptionItem.Create(Id + 15, "BlackHoleRadius", new(0.1f, 5f, 0.1f), 1.2f, tab, false)
            .SetParent(CustomRoleSpawnChances[role])
            .SetValueFormat(OptionFormat.Multiplier);
        BlackHoleSkin = BooleanOptionItem.Create(Id + 17, "BlackHoleSkin", false, tab, false)
            .SetParent(CustomRoleSpawnChances[role]);
        CanKillImpostors = BooleanOptionItem.Create(Id + 19, "CanKillImpostors", false, tab, false).SetParent(CustomRoleSpawnChances[role]);
        CanKillTNA = BooleanOptionItem.Create(Id + 20, "CanKillTNA", false, tab, false).SetParent(CustomRoleSpawnChances[role]);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = BlackHolePlaceCooldown.GetInt();
    }

    public override void Init()
    {
        if (BlackHoles.Count > 0)
        {
            foreach (var blackHole in BlackHoles)
            {
                if (blackHole.Value.NetObject != null && AmongUsClient.Instance.AmHost)
                    blackHole.Value.NetObject.Despawn();
            }
            BlackHoles.Clear();
        }
    }

    public override bool OnCheckVanish(PlayerControl shapeshifter)
    {
        if (!Main.AllAlivePlayerControls.Where(x => x.PlayerId != shapeshifter.PlayerId).Any())
        {
            return false;
        }
        // When no player exists, Instantly spawm and despawn networked object will cause error spam

        if (BlackHoles.Count >= BlackHoleCountLimit.GetInt())
        {
            return false;
        }

        CreateBlackHole(shapeshifter);
        return false;
    }
    private void CreateBlackHole(PlayerControl shapeshifter)
    {
        Vector2 pos = shapeshifter.GetCustomPosition();
        PlainShipRoom room = shapeshifter.GetPlainShipRoom();
        string roomName = room == null ? string.Empty : Translator.GetString($"{room.RoomId}");
        BlackHoles.Add(shapeshifter.PlayerId, new(new(pos), Utils.TimeStamp, pos, roomName, 0));
        SendCreateBlackholeRPC();
        // Utils.SendRPC(CustomRPC.SyncRoleSkill, shapeshifter.PlayerId, 1, pos, roomName);
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (ShouldDespawnCNOOnMeeting)
        {
            foreach (var abyss in BlackHoles.Keys)
            {
                SendRemoveBlackholeRPC(abyss);
            }

            BlackHoles.ForEach(x => x.Value.NetObject.Despawn());
            BlackHoles.Clear();
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.OverrideText(Translator.GetString("AbyssbringerButtonText"));
        hud.AbilityButton.SetUsesRemaining(BlackHoleCountLimit.GetInt() - BlackHoles.Count);
    }
    private int Count;
    public override void OnFixedUpdate(PlayerControl pc, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (Count++ < 3) return;

        Count = 0;

        var abyss = _Player;
        var id = abyss.PlayerId;

        if (!BlackHoles.TryGetValue(_Player.PlayerId, out var blackHole))
        {
            return;
        }
        var despawnMode = (DespawnMode)BlackHoleDespawnMode.GetValue();

        switch (despawnMode)
        {
            case DespawnMode.AfterTime when Utils.TimeStamp - blackHole.PlaceTimeStamp > BlackHoleDespawnTime.GetInt():
                RemoveBlackHole();
                return;
            case DespawnMode.AfterMeeting when Main.MeetingIsStarted:
                RemoveBlackHole();
                return;
        }

        if (MeetingHud.Instance || Main.LastMeetingEnded + 2 > nowTime) return;

        var nearestPlayer = Main.AllAlivePlayerControls.Where(x => x != abyss).MinBy(x => Vector2.Distance(x.GetCustomPosition(), blackHole.Position));
        if (nearestPlayer != null)
        {
            var pos = nearestPlayer.GetCustomPosition();
            if (BlackHoleMovesTowardsNearestPlayer.GetBool() && GameStates.IsInTask && !ExileController.Instance)
            {
                var direction = (pos - blackHole.Position).normalized;
                var newPosition = blackHole.Position + direction * BlackHoleMoveSpeed.GetFloat() * Time.fixedDeltaTime;
                blackHole.Position = newPosition;
                blackHole.NetObject.Position = newPosition;
            }

            if (Vector2.Distance(pos, blackHole.Position) <= BlackHoleRadius.GetFloat())
            {
                if ((nearestPlayer.Is(Custom_Team.Impostor) && !pc.Is(CustomRoles.Narc) && !CanKillImpostors.GetBool()) || (nearestPlayer.IsTransformedNeutralApocalypse() && !CanKillTNA.GetBool())) return;
                if (nearestPlayer.IsPolice() && pc.Is(CustomRoles.Narc) && !CanKillImpostors.GetBool()) return;

                RPC.PlaySoundRPC(Sounds.KillSound, pc.PlayerId);
                blackHole.PlayersConsumed++;
                SendConsumedRPC(id, blackHole);
                Notify();

                nearestPlayer.RpcExileV2();
                nearestPlayer.SetRealKiller(_Player);
                nearestPlayer.SetDeathReason(PlayerState.DeathReason.Consumed);
                Main.PlayerStates[nearestPlayer.PlayerId].SetDead();
                MurderPlayerPatch.AfterPlayerDeathTasks(_Player, nearestPlayer, inMeeting: false, fromRole: true);

                if (despawnMode == DespawnMode.After1PlayerEaten)
                {
                    RemoveBlackHole();
                }
            }
        }
        else
        {
            // No players to follow, despawn
            RemoveBlackHole();
            Notify();
        }

        void RemoveBlackHole()
        {
            BlackHoles.Remove(id);
            blackHole.NetObject.Despawn();
            SendRemoveBlackholeRPC(id);
            Notify();
        }

        void Notify() => Utils.NotifyRoles(SpecifySeer: abyss, SpecifyTarget: abyss);
    }

    private void SendCreateBlackholeRPC()
    {
        Vector2 pos = _Player.GetCustomPosition();
        PlainShipRoom room = _Player.GetPlainShipRoom();
        string roomName = room == null ? string.Empty : Translator.GetString($"{room.RoomId}");
        Utils.SendRPC(CustomRPC.SyncRoleSkill, _Player.PlayerId, 1, pos, roomName);
    }
    private void SendRemoveBlackholeRPC(byte id)
    {
        Utils.SendRPC(CustomRPC.SyncRoleSkill, _Player, 3, id);
    }

    private void SendConsumedRPC(byte id, BlackHoleData blackHole)
    {
        Utils.SendRPC(CustomRPC.SyncRoleSkill, _Player, 2, id, (byte)blackHole.PlayersConsumed);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        switch (reader.ReadPackedInt32())
        {
            case 1:
                var id = reader.ReadByte();
                var pos = reader.ReadVector2();
                var roomName = reader.ReadString();
                if (BlackHoles.ContainsKey(id))
                {
                    BlackHoles.Remove(id);
                }
                BlackHoles.Add(id, new(new(pos), Utils.TimeStamp, pos, roomName, 0));
                break;
            case 2:
                var key = reader.ReadByte();
                if (!BlackHoles.ContainsKey(key)) return;

                BlackHoles[key].PlayersConsumed = reader.ReadByte();
                break;
            case 3:
                BlackHoles.Remove(reader.ReadByte());
                break;
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl target = null, bool isMeeting = false, bool isForHud = false)
    {
        if (seer.PlayerId != target.PlayerId || seer.PlayerId != _state.PlayerId || (seer.IsModded() && !isForHud) || isMeeting || BlackHoles.Count == 0) return string.Empty;
        return string.Format(Translator.GetString("Abyssbringer.Suffix"), BlackHoles.Count, string.Join('\n', BlackHoles.Select(x => GetBlackHoleFormatText(x.Value.RoomName, x.Value.PlayersConsumed))));

        static string GetBlackHoleFormatText(string roomName, int playersConsumed)
        {
            var rn = roomName == string.Empty ? Translator.GetString("Outside") : roomName;
            return string.Format(Translator.GetString("Abyssbringer.Suffix.BlackHole"), rn, playersConsumed);
        }
    }

    [Obfuscation(Exclude = true)]
    enum DespawnMode
    {
        None,
        AfterTime,
        After1PlayerEaten,
        AfterMeeting
    }

    class BlackHoleData(BlackHole NetObject, long PlaceTimeStamp, Vector2 Position, string RoomName, int PlayersConsumed)
    {
        public BlackHole NetObject { get; } = NetObject;
        public long PlaceTimeStamp { get; } = PlaceTimeStamp;
        public Vector2 Position { get; set; } = Position;
        public string RoomName { get; } = RoomName;
        public int PlayersConsumed { get; set; } = PlayersConsumed;
    }

    internal sealed class BlackHole : CustomNetObject
    {
        internal BlackHole(Vector2 position)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!BlackHoleSkin.GetBool()) CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#000000>█<#19131c>█<#000000>█<#000000>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#412847>█<#000000>█<#19131c>█<#000000>█<#412847>█<#260f26>█<alpha=#00>█<br><#000000>█<#412847>█<#412847>█<#000000>█<#260f26>█<#1c0d1c>█<#19131c>█<#000000>█<br><#19131c>█<#000000>█<#412847>█<#1c0d1c>█<#1c0d1c>█<#000000>█<#19131c>█<#000000>█<br><#000000>█<#000000>█<#260f26>█<#1c0d1c>█<#1c0d1c>█<#000000>█<#000000>█<#260f26>█<br><#000000>█<#260f26>█<#1c0d1c>█<#1c0d1c>█<#19131c>█<#412847>█<#412847>█<#19131c>█<br><alpha=#00>█<#260f26>█<#412847>█<#412847>█<#19131c>█<#260f26>█<#19131c>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#412847>█<#260f26>█<#260f26>█<#000000>█<alpha=#00>█<alpha=#00>█<br></line-height></size>", position);
            else CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#ca07e4>█<#ca07e4>█<#ca07e4>█<#ca07e4>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#ca07e4>█<#b407e4>█<#b407e4>█<#b407e4>█<#b407e4>█<#ca07e4>█<alpha=#00>█<br><#ca07e4>█<#b407e4>█<#a907e4>█<#a907e4>█<#a907e4>█<#a907e4>█<#b407e4>█<#ca07e4>█<br><#ca07e4>█<#b407e4>█<#a907e4>█<#8b07e4>█<#8b07e4>█<#a907e4>█<#b407e4>█<#ca07e4>█<br><#ca07e4>█<#b407e4>█<#a907e4>█<#8b07e4>█<#8b07e4>█<#a907e4>█<#b407e4>█<#ca07e4>█<br><#ca07e4>█<#b407e4>█<#a907e4>█<#a907e4>█<#a907e4>█<#a907e4>█<#b407e4>█<#ca07e4>█<br><alpha=#00>█<#ca07e4>█<#b407e4>█<#b407e4>█<#b407e4>█<#b407e4>█<#ca07e4>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#ca07e4>█<#ca07e4>█<#ca07e4>█<#ca07e4>█<alpha=#00>█<alpha=#00>█<br></line-height></size>", position);
        }
    }
}