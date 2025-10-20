using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE;

public static class TagMode
{
    private const int Id = 67_226_001;

    public static OptionItem PlaySound;

    public static OptionItem ZombieMaximun;
    public static OptionItem ZombieVision;
    public static OptionItem ZombieSpeed;
    public static OptionItem ZombieKcd;

    public static OptionItem CrewmateTasks;
    public static OptionItem CrewmateVision;
    public static OptionItem CrewmateSpeed;
    public static OptionItem CrewmateVentCD;
    public static OptionItem CrewmateVentMaxTime;
    public static OptionItem CrewmateInvisibleTime;
    public static OptionItem CrewmateDetectTime;
    public static OptionItem CrewmateZapTime;

    [Obfuscation(Exclude = true)]
    private enum TCrewmateTaskList
    {
        TagMode_VeryLess,
        TagMode_Less,
        TagMode_Much,
        TagMode_VeryMuch,
    }

    public static bool Dark;
    public static bool Zap;
    public static (int, int) TaskCount = (0, 0);

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(10000035, "MenuTitle.TagMode", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.TagMode)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));

        PlaySound = BooleanOptionItem.Create(Id + 1, "PlaySound", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue))
            .SetHeader(true);

        ZombieMaximun = IntegerOptionItem.Create(Id + 2, "TagMode_ZombieMaximum", new(1, 4, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Players)
            .SetHeader(true);
        ZombieVision = FloatOptionItem.Create(Id + 3, "TagMode_ZombieVision", new(0.25f, 5f, 0.25f), 1f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Multiplier)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        ZombieSpeed = FloatOptionItem.Create(Id + 4, "TagMode_ZombieSpeed", new(0.25f, 5f, 0.25f), 1.25f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Multiplier)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        ZombieKcd = FloatOptionItem.Create(Id + 5, "TagMode_ZombieKcd", new(0f, 60f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));

        CrewmateTasks = StringOptionItem.Create(Id + 6, "TagMode_CrewmateTasks", EnumHelper.GetAllNames<TCrewmateTaskList>(), 0 , TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue))
            .SetHeader(true);
        CrewmateVision = FloatOptionItem.Create(Id + 7, "TagMode_CrewmateVision", new(0.25f, 5f, 0.25f), 1.25f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Multiplier)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        CrewmateSpeed = FloatOptionItem.Create(Id + 8, "TagMode_CrewmateSpeed", new(0.25f, 5f, 0.25f), 1.25f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Multiplier)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        CrewmateVentCD = FloatOptionItem.Create(Id + 9, "TagMode_CrewmateVentCD", new(0f, 60f, 1f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        CrewmateVentMaxTime = FloatOptionItem.Create(Id + 10, "TagMode_CrewmateVentMaxTime", new(0f, 60f, 1f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        CrewmateInvisibleTime = FloatOptionItem.Create(Id + 11, "TagMode_CrewmateInvisibleTime", new(0f, 60f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        CrewmateDetectTime = FloatOptionItem.Create(Id + 12, "TagMode_CrewmateDetectTime", new(0f, 60f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
        CrewmateZapTime = FloatOptionItem.Create(Id + 13, "TagMode_CrewmateZapTime", new(0f, 60f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TagMode)
            .SetValueFormat(OptionFormat.Seconds)
            .SetColor(new Color32(44, 204, 0, byte.MaxValue));
    }

    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.TagMode) return;

        Dark = true;
        Zap = false;
        _ = new LateTask(() =>
        {
            Dark = false;
            foreach (var tac in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TZombie)))
                tac.MarkDirtySettings();
        }, 25f, "Zombie Can Move");
        int TaskNum = 0;
        switch (CrewmateTasks.GetInt())
        {
            case 0:
                TaskNum = Main.AllAlivePlayerControls.Count() - ZombieMaximun.GetInt();
                break;
            case 1:
                TaskNum = (Main.AllAlivePlayerControls.Count() - ZombieMaximun.GetInt()) * 2;
                break;
            case 2:
                TaskNum = (Main.AllAlivePlayerControls.Count() - ZombieMaximun.GetInt()) * 3;
                break;
            case 3:
                TaskNum = (Main.AllAlivePlayerControls.Count() - ZombieMaximun.GetInt()) * 4;
                break;
        }
        TaskCount = (0, TaskNum);
        if (PlaySound.GetBool())
        {
            Main.AllPlayerControls.Where(x => x.IsHost()).Do(x => RPC.PlaySound(x.PlayerId, Sounds.HnSLong));
        }
    }

    public static void SendTaskRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(TaskCount.Item1);
        writer.Write(TaskCount.Item2);
        var sender = new RpcSyncTagModeTaskStates(PlayerControl.LocalPlayer.NetId, TaskCount.Item1, TaskCount.Item2);
        RpcUtils.LateBroadcastReliableMessage(sender);
    }

    public static void HandleSyncTagModeTaskStates(MessageReader reader)
    {
        TaskCount = (reader.ReadInt32(), reader.ReadInt32());
    }

    public static void AppendTagModeKcount(StringBuilder builder)
    {
        int ZombieCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.TZombie));
        int CrewmateCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.TCrewmate));

        builder.Append(string.Format(GetString("Remaining.TagModeZombie"), ZombieCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.TagModeCrewmate"), CrewmateCount));
    }
}

class TagModeGameEndPredicate : GameEndPredicate
{
    public override bool CheckForEndGame(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;

        if (Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.TZombie)) <= 0)
        {
            reason = GameOverReason.ImpostorDisconnect;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TCrewmate);
            CustomWinnerHolder.WinnerIds.Clear();
            Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TCrewmate)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
            Main.DoBlockNameChange = true;
            return true;
        }

        if (Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.TCrewmate)) < 1)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TZombie);
            CustomWinnerHolder.WinnerIds.Clear();
            Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TZombie)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
            Main.DoBlockNameChange = true;
            return true;
        }

        if (TagMode.TaskCount.Item1 >= TagMode.TaskCount.Item2)
        {
            reason = GameOverReason.CrewmatesByTask;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TCrewmate);
            CustomWinnerHolder.WinnerIds.Clear();
            Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TCrewmate)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
            Main.DoBlockNameChange = true;
            return true;
        }

        return false;
    }
}

public class TZombie : RoleBase
{
    public override CustomRoles Role => CustomRoles.TZombie;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    public override bool IsDesyncRole => true;

    public override void Add(byte playerId)
    {
        var player = GetPlayerById(playerId);
        player.RpcSetColor(2);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, TagMode.Dark ? 0f : TagMode.ZombieVision.GetFloat());

        float speed;

        if (TagMode.Dark)
        {
            speed = Main.MinSpeed;
        }
        else
        {
            speed = TagMode.ZombieSpeed.GetFloat();
        }

        Main.AllPlayerSpeed[playerId] = speed;
        AURoleOptions.PlayerSpeedMod = speed;
    }

    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => false;

    public override bool CanUseKillButton(PlayerControl pc) => !TagMode.Zap;

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = TagMode.ZombieKcd.GetFloat();

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        return false;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var targetRoleClass = target.GetRoleClass();

        if (targetRoleClass.Role != CustomRoles.TCrewmate)
        {
            return false;
        }

        TCrewmate targetRole = targetRoleClass as TCrewmate;

        if (targetRole.ProtectState.Item1)
        {
            targetRole.ProtectState = (false, 0f);
            targetRole.SendRPC();

            killer.SetKillCooldown(TagMode.ZombieKcd.GetFloat(), target, true);
            killer.ResetKillCooldown();

            target.RpcGuardAndKill(target);

            return false;
        }

        target.GetRoleClass()?.OnRemove(target.PlayerId);
        target.RpcSetCustomRole(CustomRoles.TZombie);
        target.RpcSetRoleDesync(RoleTypes.Impostor, target.GetClientId());
        target.GetRoleClass()?.OnAdd(target.PlayerId);
        killer.SetKillCooldown(TagMode.ZombieKcd.GetFloat(), target, true);
        killer.ResetKillCooldown();
        target.ResetKillCooldown();
        target.SetKillCooldown(forceAnime: true);
        NotifyRoles(killer, target, true);
        NotifyRoles(target, killer, true);
        target.Notify(ColorString(GetRoleColor(CustomRoles.TZombie), GetString("YouBecomeZombie")));

        foreach (var tac in Main.AllPlayerControls)
            RPC.PlaySoundRPC(Sounds.ImpTransform, tac.PlayerId);

        return false;
    }

    public override string GetProgressText(byte playerId, bool comms) => string.Empty;

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText($"{GetString("TZombieButtonText")}");
    }
}

public class TCrewmate : RoleBase
{
    public override CustomRoles Role => CustomRoles.TCrewmate;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    public override bool IsDesyncRole => false;
    public override bool BlockMoveInVent(PlayerControl pc) => pc.GetAbilityUseLimit() < 1;

    public (bool, float) ProtectState = (false, 0f);
    public (bool, float) InvisibleState = (false, 0f);
    public (bool, float) DetectState = (false, 0f);
    public int TaskInt = 0;

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
        ProtectState = (false, 0f);
        InvisibleState = (false, 0f);
        DetectState = (false, 0f);
        TaskInt = 0;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, TagMode.CrewmateVision.GetFloat());

        float speed;

        speed = TagMode.CrewmateSpeed.GetFloat();

        Main.AllPlayerSpeed[playerId] = speed;
        AURoleOptions.PlayerSpeedMod = speed;

        var pc = GetPlayerById(playerId);
        AURoleOptions.EngineerCooldown = TagMode.CrewmateVentCD.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = TagMode.CrewmateVentMaxTime.GetFloat();
    }

    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => true;

    public override bool CanUseKillButton(PlayerControl pc) => false;

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        return false;
    }

    public void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(TaskInt);
        writer.Write(ProtectState.Item1);
        writer.Write(ProtectState.Item2);
        writer.Write(InvisibleState.Item1);
        writer.Write(InvisibleState.Item2);
        writer.Write(DetectState.Item1);
        writer.Write(DetectState.Item2);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        TaskInt = reader.ReadInt32();
        ProtectState = (reader.ReadBoolean(), reader.ReadSingle());
        InvisibleState = (reader.ReadBoolean(), reader.ReadSingle());
        DetectState = (reader.ReadBoolean(), reader.ReadByte());
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;
        if (TaskInt >= 2)
        {
            player.RpcResetTasks();
            TaskInt = 0;
        }
        player.SetAbilityUseLimit(0);

        var Fg = IRandom.Instance;
        int Power = Fg.Next(1, 6);

        if (Power == 1)
        {
            player.Notify(GetString("CanVent"), 5f);
            player.RpcIncreaseAbilityUseLimitBy(1);
        }
        else if (Power == 2)
        {
            player.Notify(GetString("YouInvisible"), 5f);
            player.RpcMakeInvisible();
            InvisibleState = (true, TagMode.CrewmateInvisibleTime.GetFloat());
        }
        else if (Power == 3)
        {
            player.Notify(GetString("YouProtect"), 5f);
            ProtectState = (true, 300f);
        }
        else if (Power == 4)
        {
            player.Notify(GetString("YouDetect"), 5f);
            foreach (var target in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TZombie)))
                TargetArrow.Add(player.PlayerId, target.PlayerId);
            DetectState = (true, TagMode.CrewmateDetectTime.GetFloat());
        }
        else if (Power == 5)
        {
            player.Notify(GetString("ZapZombie"), 5f);
            foreach (var target in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TZombie)))
                target.Notify(GetString("YouZap"), 5f);
            TagMode.Zap = true;
            _ = new LateTask(() =>
            {
                TagMode.Zap = false;
                foreach (var target in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TZombie)))
                    target.Notify(GetString("ZapFinished"), 5f);
            }, TagMode.CrewmateZapTime.GetFloat(), "Zap Finished");
        }
        TagMode.TaskCount.Item1++;
        TaskInt++;

        SendRPC();
        TagMode.SendTaskRPC();
        return true;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        var changed = false;
        if (ProtectState.Item1)
        {
            ProtectState.Item2 -= Time.fixedDeltaTime;

            if (ProtectState.Item2 <= 0)
            {
                ProtectState = (false, 0f);
                changed = true;
            }
        }

        if (InvisibleState.Item1)
        {
            InvisibleState.Item2 -= Time.fixedDeltaTime;
            if (InvisibleState.Item2 <= 0)
            {
                InvisibleState = (false, 0f);
                player.RpcMakeVisible();
                player.MarkDirtySettings();
                changed = true;
            }
        }

        if (DetectState.Item1)
        {
            DetectState.Item2 -= Time.fixedDeltaTime;
            if (DetectState.Item2 <= 0)
            {
                DetectState = (false, 0f);
                foreach (var target in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TZombie)))
                    TargetArrow.Remove(player.PlayerId, target.PlayerId);
                changed = true;
            }
        }

        if (changed)
        {
            SendRPC();
            NotifyRoles();
        }
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (pc.GetAbilityUseLimit() > 0)
            pc.RpcRemoveAbilityUse();
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId)
        {
            if (ProtectState.Item1)
            {
                return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
            }
        }

        return "";
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId) return "";

        if (ProtectState.Item1)
        {
            return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
        }

        return "";
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        var arrows = TargetArrow.GetAllArrows(seer);

        return arrows;
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        return ColorString(Color.yellow, $"({TagMode.TaskCount.Item1}/{TagMode.TaskCount.Item2}) - ({playerId.GetAbilityUseLimit()})");
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.SetUsesRemaining((int)id.GetAbilityUseLimit());
    }
}
