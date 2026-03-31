using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Neutral;

internal class Logos : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Logos;
    private const int Id = 34300;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem HasImpostorVision;
    public static OptionItem CanUnlockStage1;
    public static OptionItem Stage1Tasks;
    public static OptionItem CanUnlockStage2;
    public static OptionItem Stage2Tasks;
    public static OptionItem Stage2KillCooldown;
    public static OptionItem CanUnlockStage3;
    public static OptionItem Stage3Tasks;
    public static OptionItem CanUnlockStage4;
    public static OptionItem Stage4Tasks;

    public static List<bool> Stage = [];
    public static bool PreventKill;
    public static float NowCooldown;
    public static bool AssignNew;
    public static int AbilityStage;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Logos, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos])
            .SetValueFormat(OptionFormat.Seconds);
        HasImpostorVision = BooleanOptionItem.Create(Id + 11, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos]);
        CanUnlockStage1 = BooleanOptionItem.Create(Id + 12, "Logos.CanUnlockStage1", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos]);
        Stage1Tasks = IntegerOptionItem.Create(Id + 13, "Logos.Stage1Tasks", new(0, 100, 5), 25, TabGroup.NeutralRoles, false)
            .SetParent(CanUnlockStage1)
            .SetValueFormat(OptionFormat.Percent);
        CanUnlockStage2 = BooleanOptionItem.Create(Id + 14, "Logos.CanUnlockStage2", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos]);
        Stage2Tasks = IntegerOptionItem.Create(Id + 15, "Logos.Stage2Tasks", new(0, 100, 5), 50, TabGroup.NeutralRoles, false)
            .SetParent(CanUnlockStage2)
            .SetValueFormat(OptionFormat.Percent);
        Stage2KillCooldown = FloatOptionItem.Create(Id + 16, GeneralOption.MinKillCooldown, new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CanUnlockStage2)
            .SetValueFormat(OptionFormat.Seconds);
        CanUnlockStage3 = BooleanOptionItem.Create(Id + 17, "Logos.CanUnlockStage3", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos]);
        Stage3Tasks = IntegerOptionItem.Create(Id + 18, "Logos.Stage3Tasks", new(0, 100, 5), 75, TabGroup.NeutralRoles, false)
            .SetParent(CanUnlockStage3)
            .SetValueFormat(OptionFormat.Percent);
        CanUnlockStage4 = BooleanOptionItem.Create(Id + 19, "Logos.CanUnlockStage4", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Logos]);
        Stage4Tasks = IntegerOptionItem.Create(Id + 20, "Logos.Stage4Tasks", new(0, 100, 5), 100, TabGroup.NeutralRoles, false)
            .SetParent(CanUnlockStage4)
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void Init()
    {
        Stage ??= new List<bool>(new bool[4]);
        Stage.Clear();
        Stage.AddRange(new bool[4]);
        PreventKill = false;
        NowCooldown = KillCooldown.GetFloat();
        AssignNew = false;
        AbilityStage = 0;
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown;
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Stage[0];
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (_Player.GetAbilityUseLimit() > 0)
            hud.KillButton?.OverrideText($"{GetString("GangsterButtonText")}");
        else
            hud.KillButton?.OverrideText($"{GetString("KillButtonText")}");
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var addon = killer.GetBetrayalAddon(true);
        var role = addon switch
        {
            CustomRoles.Admired => CustomRoles.Sheriff,
            CustomRoles.Madmate => CustomRoles.Refugee,
            CustomRoles.Recruit => CustomRoles.Sidekick,
            _ => CustomRoles.Philosopher
        };

        if (target.Is(CustomRoles.Logos) || target.Is(CustomRoles.Philosopher)) return false;

        bool TargetCanBePhilosopher = !target.Is(CustomRoles.Loyal) && !target.Is(CustomRoles.Paranoia);

        if (killer.GetAbilityUseLimit() > 0)
        {
            if (!TargetCanBePhilosopher)
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Logos), GetString("Jackal_RecruitFailed")));
                return true;
            }
            killer.RpcRemoveAbilityUse();

            Logger.Info($"Logos {killer.GetNameWithRole()} assigned {role} to {target.GetNameWithRole()}", "Logos");

            foreach (var x in target.GetCustomSubRoles().ToList())
            {
                if (x.IsBetrayalAddonV2())
                {
                    Main.PlayerStates[target.PlayerId].RemoveSubRole(x);
                    Main.PlayerStates[target.PlayerId].SubRoles.Remove(CustomRoles.Rascal);
                }
            }

            target.GetRoleClass()?.OnRemove(target.PlayerId);
            target.RpcChangeRoleBasis(role);
            target.RpcSetCustomRole(role);
            target.GetRoleClass()?.OnAdd(target.PlayerId);
            target.RpcResetTasks();

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(role), GetString("GangsterSuccessfullyRecruited")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(role), GetString("BeRecruitedByLogos")));

            if (role is CustomRoles.Philosopher && killer.GetBetrayalAddon() != CustomRoles.NotAssigned)
                target.RpcSetCustomRole(addon);

            Utils.NotifyRoles(killer, target, true);
            Utils.NotifyRoles(target, killer, true);

            killer.ResetKillCooldown();
            killer.SetKillCooldown(5f, forceAnime: !DisableShieldAnimations.GetBool());

            return false;
        }

        return true;
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (Stage[2] && !PreventKill)
        {
            killer.RpcGuardAndKill(target);
            killer.SetKillCooldown();
            PreventKill = true;
            return false;
        }
        return true;
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (target.IsDisconnected())
        {
            if (inMeeting) AssignNew = true;
            else AssignNewLogos();
            return;
        }
        foreach (var player in Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Philosopher)))
        {
            if (inMeeting) player.RpcExileV3();
            else player.RpcMurderPlayer(player);

            player.SetRealKiller(killer);
            player.SetDeathReason(PlayerState.DeathReason.Philosophy);
        }
    }

    public static void AssignNewLogos()
    {
        var candidates = Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Philosopher)).ToList();
        if (candidates.Count > 0)
        {
            var player = candidates.RandomElement();
            player.GetRoleClass()?.OnRemove(player.PlayerId);
            player.RpcChangeRoleBasis(CustomRoles.Logos);
            player.RpcSetCustomRole(CustomRoles.Logos);
            player.GetRoleClass()?.OnAdd(player.PlayerId);
            player.SetAbilityUseLimit(0);
            Utils.NotifyRoles(SpecifyTarget: player);
        }
    }

    public override void AfterMeetingTasks()
    {
        if (AssignNew)
        {
            AssignNewLogos();
            AssignNew = false;
        }
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Logos)) return string.Empty;
        if (target.Is(CustomRoles.Philosopher)) return Main.roleColors[CustomRoles.Logos];
        if (!seer.IsAlive() || !Stage[3]) return string.Empty;

        var customRole = target.GetCustomRole();

        if (Lich.IsCursed(target)) return "7f8c8d";

        foreach (var SubRole in target.GetCustomSubRoles())
        {
            if (SubRole is CustomRoles.Charmed
                or CustomRoles.Infected
                or CustomRoles.Contagious
                or CustomRoles.Egoist
                or CustomRoles.Recruit
                or CustomRoles.Soulless)
                return "7f8c8d";
            if (SubRole is CustomRoles.Admired)
            {
                return Main.roleColors[CustomRoles.Bait];
            }
        }

        if (Main.PlayerStates[target.PlayerId].IsNecromancer)
        {
            return Main.roleColors[CustomRoles.Coven];
        }

        if (customRole.IsImpostorTeamV2() || customRole.IsMadmate())
        {
            return Main.roleColors[CustomRoles.Impostor];
        }

        if (customRole.IsCrewmate())
        {
            return Main.roleColors[CustomRoles.Bait];
        }

        if (customRole.IsCoven() || customRole.Equals(CustomRoles.Enchanted))
        {
            return Main.roleColors[CustomRoles.Coven];
        }

        return "7f8c8d";
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (seer.IsAnySubRole(x => x.IsConverted()) || target.IsAnySubRole(x => x.IsConverted()))
            return false;
        if (seer.Is(CustomRoles.Logos) && target.Is(CustomRoles.Philosopher))
            return true;
        if (seer.Is(CustomRoles.Philosopher) && target.Is(CustomRoles.Logos))
            return true;
        return false;
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        Color TextColor = Utils.GetRoleColor(CustomRoles.Logos).ShadeColor(0.25f);
        Color RoleColor = playerId.GetAbilityUseLimit() > 0 ? Utils.GetRoleColor(CustomRoles.Logos).ShadeColor(0.25f) : Color.red;

        ProgressText.Append(Utils.ColorString(TextColor, Utils.ColorString(Color.white, " - ") + $"({AbilityStage}/4)")
        + Utils.ColorString(Color.white, " - ") + Utils.ColorString(RoleColor, $"({playerId.GetAbilityUseLimit()})"));
        return ProgressText.ToString();
    }

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => player.GetAbilityUseLimit() > 0 ? CustomButton.Get("Sidekick") : null;
}

internal class Philosopher : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Philosopher;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player) return true;

        var taskNum = (int)(((float)completedTaskCount / totalTaskCount) * 100);

        if (!Logos.Stage[0] && Logos.CanUnlockStage1.GetBool() && taskNum >= Logos.Stage1Tasks.GetInt())
        {
            Logos.Stage[0] = true;
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos) || x.Is(CustomRoles.Philosopher))
            .Do(x => x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Logos), GetString("Logos.UnlockStage1Ability"))));
            Logos.AbilityStage++;
            SendRPC();
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos))
            .Do(x => Utils.NotifyRoles(SpecifyTarget: x));
        }

        if (!Logos.Stage[1] && Logos.CanUnlockStage2.GetBool() && taskNum >= Logos.Stage2Tasks.GetInt())
        {
            Logos.Stage[1] = true;
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos) || x.Is(CustomRoles.Philosopher))
            .Do(x => x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Logos), GetString("Logos.UnlockStage2Ability"))));
            Logos.NowCooldown = Logos.Stage2KillCooldown.GetFloat();
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos))
            .Do(x => x.SyncSettings());
            Logos.AbilityStage++;
            SendRPC();
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos))
            .Do(x => Utils.NotifyRoles(SpecifyTarget: x));
        }

        if (!Logos.Stage[2] && Logos.CanUnlockStage3.GetBool() && taskNum >= Logos.Stage3Tasks.GetInt())
        {
            Logos.Stage[2] = true;
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos) || x.Is(CustomRoles.Philosopher))
            .Do(x => x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Logos), GetString("Logos.UnlockStage3Ability"))));
            Logos.AbilityStage++;
            SendRPC();
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos))
            .Do(x => Utils.NotifyRoles(SpecifyTarget: x));
        }

        if (!Logos.Stage[3] && Logos.CanUnlockStage4.GetBool() && taskNum >= Logos.Stage4Tasks.GetInt())
        {
            Logos.Stage[3] = true;
            Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.Logos) || x.Is(CustomRoles.Philosopher))
            .Do(x => x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Logos), GetString("Logos.UnlockStage4Ability"))));
            Logos.AbilityStage++;
            SendRPC();
            Utils.NotifyRoles();
        }

        return true;
    }

    public void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(Logos.AbilityStage);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        Logos.AbilityStage = reader.ReadInt32();
    }
}
