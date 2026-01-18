using AmongUs.GameOptions;
using System;
using System.Collections;
using TONE.Modules;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;
using Object = UnityEngine.Object;

namespace TONE.Roles.Crewmate;

// 部分代码参考：https://github.com/Gurge44/EndlessHostRoles
internal class TimeMaster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TimeMaster;
    private const int Id = 9900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.TimeMaster);
    public override CustomRoles ThisRoleBase => UsePets.GetBool() ? CustomRoles.Crewmate : CustomRoles.Engineer;
    public override bool IsExperimental => true;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    public static OptionItem TimeMasterSkillCooldown;
    public static OptionItem TimeMasterSkillDuration;
    private static OptionItem TimeMasterSkillDuration2;
    public static OptionItem TimeMasterUsePetRewind;
    private static OptionItem TimeMasterMaxUses;

    private static readonly Dictionary<byte, long> TimeMasterInProtect = [];
    private static Dictionary<long, Dictionary<byte, Vector2>> BackTrack = [];
    private static readonly Dictionary<byte, float> originalSpeed = [];
    public static bool Rewinding;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeMaster);
        TimeMasterSkillCooldown = FloatOptionItem.Create(Id + 10, "TimeMasterSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        TimeMasterSkillDuration = FloatOptionItem.Create(Id + 11, "TimeMasterSkillDuration", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        TimeMasterSkillDuration2 = FloatOptionItem.Create(Id + 12, "TimeMasterSkillDuration2", new(1f, 180f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        TimeMasterUsePetRewind = BooleanOptionItem.Create(Id + 13, "TimeMasterUsePetRewind", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster]);
        TimeMasterMaxUses = IntegerOptionItem.Create(Id + 14, "AbilityUseLimit", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Times);
        TimeMasterAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeMaster])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        TimeMasterInProtect.Clear();
        originalSpeed.Clear();
        Rewinding = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(TimeMasterMaxUses.GetInt());
        BackTrack = [];
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = TimeMasterSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        if (!UsePets.GetBool())
        {
            hud.AbilityButton.buttonLabelText.text = GetString("TimeMasterVentButtonText");
            hud.AbilityButton.SetUsesRemaining((int)id.GetAbilityUseLimit());
        }
        else
        {
            if (!TimeMasterUsePetRewind.GetBool()) hud.PetButton.OverrideText(GetString("TimeMasterVentButtonText"));
            else hud.PetButton.OverrideText(GetString("TimeMasterPetButtonText"));
        }
    }
    private static IEnumerator Rewind()
    {
        try
        {
            Rewinding = true;

            const float delay = 0.3f;
            long now = TimeStamp;
            int length = TimeMasterSkillDuration2.GetInt();

            foreach (var pc in Main.AllPlayerControls)
            {
                Main.AllPlayerSpeed[pc.PlayerId] = Main.MinSpeed;
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = false;
                originalSpeed.Remove(pc.PlayerId);
                originalSpeed.Add(pc.PlayerId, Main.AllPlayerSpeed[pc.PlayerId]);
            }

            string notify = ColorString(Color.yellow, string.Format(GetString("TimeMasterRewindStart"), CustomRoles.TimeMaster.ToColoredString()));

            foreach (PlayerControl player in Main.AllPlayerControls)
            {
                if (player.inVent || player.MyPhysics?.Animations?.IsPlayingEnterVentAnimation() == true) player.MyPhysics?.RpcExitVent(player.GetClosestVent().Id);
                player.ReactorFlash(flashDuration: length * delay + 0.55f);
                player.Notify(notify, Math.Max((length * delay) + 0.55f, 4f));
                player.MarkDirtySettings();
            }

            yield return new WaitForSecondsRealtime(0.55f);

            for (long i = now - 1; i >= now - length; i--)
            {
                if (!BackTrack.TryGetValue(i, out Dictionary<byte, Vector2> track)) continue;

                foreach ((byte playerId, Vector2 pos) in track)
                {
                    PlayerControl player = playerId.GetPlayer();
                    if (player == null || !player.IsAlive()) continue;

                    player.RpcTeleport(pos);
                }

                yield return new WaitForSecondsRealtime(delay);
            }

            foreach (DeadBody deadBody in Object.FindObjectsOfType<DeadBody>())
            {
                if (!Main.PlayerStates.TryGetValue(deadBody.ParentId, out PlayerState ps)) continue;

                if (ps.RealKiller.TimeStamp.AddSeconds(length) >= DateTime.Now)
                {
                    ps.Player.RpcRevive();
                    ps.Player.RpcTeleport(deadBody.TruePosition);
                    ps.Player.Notify(ColorString(Color.yellow, GetString("RevivedByTimeMaster")));
                }
            }

            foreach (var pc in Main.AllPlayerControls)
            {
                Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + originalSpeed[pc.PlayerId];
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
            }
            MarkEveryoneDirtySettings();
        }
        finally { Rewinding = false; }
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (ExileController.Instance || !player.IsAlive()) return;

        long now = TimeStamp;
        if (BackTrack.ContainsKey(now)) return;

        BackTrack[now] = Main.AllAlivePlayerControls.Where(x => !x.inVent && !x.onLadder && !x.inMovingPlat).ToDictionary(x => x.PlayerId, x => x.GetCustomPosition());

        if (!lowLoad && TimeMasterInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + TimeMasterSkillDuration.GetInt() < nowTime)
        {
            TimeMasterInProtect.Remove(player.PlayerId);
            if (!DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
            else player.RpcResetAbilityCooldown();
            player.Notify(GetString("TimeMasterSkillStop"));
        }
    }
    public override void OnPet(PlayerControl pc)
    {
        if (!TimeMasterUsePetRewind.GetBool())
        {
            OnEnterVent(pc, null);
        }
        else
        {
            if (pc.GetAbilityUseLimit() >= 1)
            {
                pc.RpcRemoveAbilityUse();

                Main.Instance.StartCoroutine(Rewind());
            }
            else
            {
                pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
            }
        }
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (TimeMasterInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
            if (TimeMasterInProtect[target.PlayerId] + TimeMasterSkillDuration.GetInt() >= GetTimeStamp(DateTime.UtcNow) && !killer.Is(CustomRoles.Pestilence))
            {
                Main.Instance.StartCoroutine(Rewind());
                killer.SetKillCooldown(target: target, forceAnime: true);
                return false;
            }
        return true;
    }
    public override void OnEnterVent(PlayerControl pc, Vent currentVent)
    {
        if (pc.GetAbilityUseLimit() >= 1)
        {
            pc.RpcRemoveAbilityUse();

            TimeMasterInProtect.Remove(pc.PlayerId);
            TimeMasterInProtect.Add(pc.PlayerId, GetTimeStamp());

            if (!pc.IsModded())
            {
                pc.RpcGuardAndKill(pc);
            }
            pc.Notify(GetString("TimeMasterOnGuard"), TimeMasterSkillDuration.GetFloat());
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting)
    {
        if (!UsePets.GetBool()) return CustomButton.Get("Time Master");
        return null;
    }
    public override Sprite GetPetButtonSprite(PlayerControl player)
    {
        if (UsePets.GetBool()) return CustomButton.Get("Time Master");
        return null;
    }
}
