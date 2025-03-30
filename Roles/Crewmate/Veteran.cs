using AmongUs.GameOptions;
using System;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Veteran : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Veteran;
    private const int Id = 11350;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Veteran);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem VeteranSkillCooldown;
    private static OptionItem VeteranSkillDuration;
    private static OptionItem VeteranSkillMaxOfUseage;
    private static OptionItem EnableAwakening;
    private static OptionItem ProgressPerTask;
    private static OptionItem ProgressPerSkill;
    private static OptionItem ProgressPerSecond;

    private static float AwakeningProgress;
    private static bool IsAwakened;
    private static bool AutoAlert;
    private static readonly Dictionary<byte, long> VeteranInProtect = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Veteran);
        VeteranSkillCooldown = FloatOptionItem.Create(Id + 10, "VeteranSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillDuration = FloatOptionItem.Create(Id + 11, "VeteranSkillDuration", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillMaxOfUseage = IntegerOptionItem.Create(Id + 12, "VeteranSkillMaxOfUseage", new(0, 20, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Times);
        VeteranAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Times);
        EnableAwakening = BooleanOptionItem.Create(Id + 14, "EnableAwakening", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran]);
        ProgressPerTask = FloatOptionItem.Create(Id + 15, "ProgressPerTask", new(0f, 100f, 10f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSkill = FloatOptionItem.Create(Id + 16, "ProgressPerSkill", new(0f, 100f, 10f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 17, "ProgressPerSecond", new(0.1f, 3f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Init()
    {
        VeteranInProtect.Clear();
        AwakeningProgress = 0;
        IsAwakened = false;
        AutoAlert = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(VeteranSkillMaxOfUseage.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = VeteranSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!EnableAwakening.GetBool() || AwakeningProgress >= 100) return string.Empty;
        return string.Format(GetString("AwakeningProgress") + ": {0:F0}% / {1:F0}%", AwakeningProgress, 100);
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!IsAwakened)
        {
            AwakeningProgress += ProgressPerTask.GetFloat();
        }
        return true;
    }

    private static void CheckAwakening(PlayerControl player)
    {
        if (AwakeningProgress >= 100 && !IsAwakened && EnableAwakening.GetBool() && player.IsAlive())
        {
            IsAwakened = true;
            AutoAlert = true;
            player.Notify(GetString("SuccessfulAwakening"), 5f);
        }
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        var killerRole = killer.GetCustomRole();
        // Not should kill
        if (killerRole is CustomRoles.Taskinator
            or CustomRoles.Crusader
            or CustomRoles.Bodyguard
            or CustomRoles.Deputy)
            return true;

        if (AutoAlert && IsAwakened)
        {
            target.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
            target.RpcRemoveAbilityUse();
            AutoAlert = false;
            return false;
        }
        if (killer.PlayerId != target.PlayerId && VeteranInProtect.TryGetValue(target.PlayerId, out var time))
            if (time + VeteranSkillDuration.GetInt() >= GetTimeStamp())
            {
                if (killer.Is(CustomRoles.Pestilence) || killer.Is(CustomRoles.War))
                {
                    killer.RpcMurderPlayer(target);
                    target.SetRealKiller(killer);
                    Logger.Info($"{killer.GetRealName()} kill {target.GetRealName()} because killer Pestilence or War", "Veteran");
                    return false;
                }
                else if (killer.Is(CustomRoles.Jinx))
                {
                    target.RpcCheckAndMurder(killer);
                    Logger.Info($"{killer.GetRealName()} is Jinx try kill {target.GetRealName()} but it is canceled", "Veteran");
                    return false;
                }
                else
                {
                    target.RpcMurderPlayer(killer);
                    killer.SetRealKiller(target);
                    Logger.Info($"{target.GetRealName()} kill {killer.GetRealName()}", "Veteran");
                    return false;
                }
            }
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (AwakeningProgress < 100)
        {
            AwakeningProgress += ProgressPerSecond.GetFloat() * Time.fixedDeltaTime;
        }
        else CheckAwakening(player);

        if (!lowLoad && VeteranInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + VeteranSkillDuration.GetInt() < nowTime)
        {
            VeteranInProtect.Remove(player.PlayerId);

            if (!DisableShieldAnimations.GetBool())
            {
                player.RpcGuardAndKill();
            }
            else
            {
                player.RpcResetAbilityCooldown();
            }

            player.Notify(string.Format(GetString("AbilityExpired"), player.GetAbilityUseLimit()));
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        // Ability use limit reached
        if (pc.GetAbilityUseLimit() <= 0)
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
            return;
        }

        // Use ability
        if (!VeteranInProtect.ContainsKey(pc.PlayerId))
        {
            if (!IsAwakened)
            {
                AwakeningProgress += ProgressPerSkill.GetFloat();
            }
            VeteranInProtect.Remove(pc.PlayerId);
            VeteranInProtect.Add(pc.PlayerId, GetTimeStamp(DateTime.Now));
            pc.RpcRemoveAbilityUse();

            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("Gunload");
            pc.Notify(GetString("AbilityInUse"), VeteranSkillDuration.GetFloat());
        }
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId) => physics.myPlayer.GetAbilityUseLimit() < 1;

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => VeteranInProtect.Clear();

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("VeteranVentButtonText");
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Veteran");
}
