using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.AddOns;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Archaeologist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Archaeologist;
    private const int Id = 32100;
    public override CustomRoles ThisRoleBase => UsePets.GetBool() ? CustomRoles.Crewmate : CustomRoles.Engineer;
    public override bool IsExperimental => true;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\
    private static List<CustomRoles> addons = [];
    private static readonly HashSet<byte> SlateRoles = [];
    private static byte RevivedPlayerId = byte.MaxValue;

    public static OptionItem VentCooldown;
    private static OptionItem InvisDuration;
    private static OptionItem FreezeTime;
    private static OptionItem FreezeRadius;
    private static OptionItem BugleSpeed;
    private static OptionItem BugleTime;
    private static OptionItem CrystalExtraVotes;
    private static OptionItem TalismanDuration;
    private static OptionItem CurseKillCooldown;

    private static byte AntiqueID = 251;
    private static bool FixNextSabo = false;
    private static bool IsProtected = false;
    private static bool HasGrail = false;
    private static bool LifeConnection = false;
    private static float Votes = 0;


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Archaeologist, 1, zeroOne: false);
        VentCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.EngineerBase_VentCooldown, new(0f, 70f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Seconds);
        InvisDuration = FloatOptionItem.Create(Id + 12, "SwooperDuration", new(5f, 70f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Seconds);
        FreezeTime = FloatOptionItem.Create(Id + 13, "FreezeTime", new(0f, 10f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Seconds);
        FreezeRadius = FloatOptionItem.Create(Id + 14, "FreezeRadius", new(0.5f, 10f, 0.5f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Multiplier);
        BugleSpeed = FloatOptionItem.Create(Id + 15, "BugleSpeed", new(0f, 5f, 0.25f), 2.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Multiplier);
        BugleTime = FloatOptionItem.Create(Id + 16, "BugleTime", new(0f, 10f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Seconds);
        CrystalExtraVotes = FloatOptionItem.Create(Id + 17, "CrystalExtraVotes", new(1f, 5f, 1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Votes);
        TalismanDuration = FloatOptionItem.Create(Id + 18, "TalismanDuration", new(5f, 60f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Seconds);
        CurseKillCooldown = FloatOptionItem.Create(Id + 19, "CurseKillCooldown", new(0f, 100f, 2.5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Archaeologist])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        IsProtected = false;
        AntiqueID = 251;
        FixNextSabo = false;
        HasGrail = false;
        Votes = 0;
        LifeConnection = false;
        addons.Clear();
        addons.AddRange(GroupedAddons[AddonTypes.Helpful]);
        RevivedPlayerId = byte.MaxValue;
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                if (GameStates.IsInTask)
                {
                    RandomAntique();
                }
            }, 8f, "Archaeologist In Start");
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = VentCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override void AfterMeetingTasks()
    {
        RandomAntique();
    }

    private static void SendRPC(PlayerControl pc)
    {
        if (!pc.IsNonHostModdedClient()) return;
        var msg = new RpcSetArchaeologist(PlayerControl.LocalPlayer.NetId, FixNextSabo, AntiqueID, RevivedPlayerId);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        FixNextSabo = reader.ReadBoolean();
        AntiqueID = reader.ReadByte();
        RevivedPlayerId = reader.ReadByte();
    }

    public override void OnPet(PlayerControl pc)
    {
        OnEnterVent(pc, null);
    }
    public override void OnEnterVent(PlayerControl player, Vent vent)
    {
        switch (AntiqueID)
        {
            case 1: // 灵魂回响镜 - 知道何时有玩家死亡
                player.RpcGuardAndKill();
                player.RpcSetCustomRole(CustomRoles.Seer, false, false);
                break;
            case 2: // 复活圣杯 - 复活一名死亡的玩家
                player.RpcGuardAndKill();
                player.Notify(GetString("GrailNeedReport"), 5f);
                HasGrail = true;
                break;
            case 3: // 相位斗篷 - 使你短暂隐形
                player.RpcGuardAndKill();
                player.RpcMakeInvisible();
                player.Notify(GetString("SwooperInvisState"), InvisDuration.GetFloat() - 1);
                _ = new LateTask(() =>
                {
                    player.RpcMakeVisible();
                    player.RpcGuardAndKill();
                    player.Notify(GetString("SwooperInvisStateOut"), 5f);
                }, InvisDuration.GetFloat());
                break;
            case 4: // 引力石板 - 将所有玩家拉向自己位置
                player.RpcGuardAndKill();
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    target.RpcTeleport(player.GetCustomPosition());
                    target.RPCPlayCustomSound("Teleport");
                }
                break;
            case 5: // 寒冰宝珠 - 冻结附近玩家
                player.RpcGuardAndKill();
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    var pos = player.transform.position;
                    var dis = Utils.GetDistance(pos, target.transform.position);
                    if (dis > FreezeRadius.GetFloat()) continue;

                    var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
                    Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                    target.Notify(GetString("OrbsAndFreeze"), 5f);
                    target.MarkDirtySettings();
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                        target.MarkDirtySettings();
                        RPC.PlaySoundRPC(Sounds.TaskComplete, target.PlayerId);
                    }, FreezeTime.GetFloat());
                }
                break;
            case 6: // 战争号角 - 所有玩家移动速度提升
                player.RpcGuardAndKill();
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
                    Main.AllPlayerSpeed[target.PlayerId] = BugleSpeed.GetFloat();
                    target.Notify(GetString("BugleAndSpeedIncrease"), 5f);
                    target.MarkDirtySettings();
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - BugleSpeed.GetFloat() + tmpSpeed;
                        target.MarkDirtySettings();
                        RPC.PlaySoundRPC(Sounds.TaskComplete, target.PlayerId);
                    }, BugleTime.GetFloat());
                }
                break;
            case 7: // 智慧卷轴 - 帮一名玩家完成一个任务
                player.RpcGuardAndKill();
                var randomPlayer = Main.AllAlivePlayerControls.Where(pc => player.PlayerId != pc.PlayerId && pc.Is(Custom_Team.Crewmate) && Utils.HasTasks(pc.Data, false)).ToList().RandomElement();

                if (randomPlayer == null)
                {
                    player.Notify(GetString("TaskManager_FailCompleteRandomTasks"));
                }

                var allNotCompletedTasks = randomPlayer.Data.Tasks.ToArray().Where(pcTask => !pcTask.Complete).ToList();

                if (allNotCompletedTasks.Count > 0)
                {
                    randomPlayer.RpcCompleteTask(allNotCompletedTasks.RandomElement().Id);

                    player.Notify(GetString("TaskManager_YouCompletedRandomTask"));
                    randomPlayer.Notify(GetString("TaskManager_CompletedRandomTaskForPlayer"));
                }
                break;
            case 8: // 能量水晶 - 恢复所有船员的技能使用次数
                player.RpcGuardAndKill();
                foreach (var target in Main.AllPlayerControls.Where(x => x.IsPlayerCrewmateTeam()))
                    target.RpcIncreaseAbilityUseLimitBy(1);
                break;
            case 9: // 激励圣物 - 会议投票时获得额外投票权
                player.RpcGuardAndKill();
                player.Notify(GetString("VotesIncreased"));
                Votes += CrystalExtraVotes.GetFloat();
                break;
            case 10: // 契约卷轴 - 与一名玩家建立生命链接
                player.RpcGuardAndKill();
                player.Notify(GetString("LifeConnection"));
                LifeConnection = true;
                break;
            case 11: // 牺牲匕首 - 牺牲自己，为内鬼增加负面效果，为船员增加正面效果
                player.RpcGuardAndKill();
                player.RpcMurderPlayer(player);
                player.SetDeathReason(PlayerState.DeathReason.Sacrifice);
                foreach (var target in Main.AllAlivePlayerControls.Where(x => x.IsPlayerImpostorTeam() || x.IsPlayerNeutralTeam() || x.IsPlayerCovenTeam()))
                {
                    target.Notify(GetString("AnAncienCurseAffectsYou"));
                    target.SetKillCooldown(CurseKillCooldown.GetFloat(), forceAnime: true);
                }
                foreach (var target in Main.AllAlivePlayerControls.Where(x => x.IsPlayerCrewmateTeam()))
                {
                    CustomRoles addon = addons.RandomElement();
                    target.Notify(GetString("AnAncienBlessingsAffectsYou"));
                    target.RpcSetCustomRole(addon);
                }
                break;
            case 12: // 太阳护符 - 使自己获得伤害免疫
                player.RpcGuardAndKill();
                IsProtected = true;
                player.RPCPlayCustomSound("Shield");
                player.Notify(GetString("ArShielded"), TalismanDuration.GetInt());

                _ = new LateTask(() =>
                {
                    IsProtected = false;
                    player.Notify(GetString("ArShieldedOut"));

                }, TalismanDuration.GetInt(), "Archaeologist Shield Is Out");
                break;
            case 13: // 真理石板 - 揭示一名玩家的真实身份
                player.RpcGuardAndKill();
                var pcList = Main.AllAlivePlayerControls.Where(pc => pc.PlayerId != player.PlayerId && !pc.GetCustomRole().IsRevealingRole(pc)).ToList();

                PlayerControl rp = pcList.RandomElement();
                if (pcList.Any() && !SlateRoles.Contains(rp.PlayerId))
                {
                    SlateRoles.Add(rp.PlayerId);
                }
                break;
            case 14: // 时光沙漏 - 重置所有玩家的击杀/技能冷却时间
                player.RpcGuardAndKill();
                foreach (var target in Main.AllAlivePlayerControls) target.SetKillCooldown();
                break;
            case 15: // 预言卷轴 - 修复冒名顶替者下次破坏
                player.RpcGuardAndKill();
                FixNextSabo = true;
                break;
            case 16: // 无线按钮 - 召开一次会议
                if (!UsePets.GetBool()) player?.MyPhysics?.RpcBootFromVent(vent.Id);
                player?.NoCheckStartMeeting(null);
                break;
            case 17: // 万能钥匙 - 打开所有的门
                player.Notify(GetString("ArOpenAllDoors"));
                DoorsReset.OpenAllDoors();
                break;
            default: // just in case
                break;
        }

        AntiqueID = 251;
        SendRPC(player);
    }

    public override void UpdateSystem(ShipStatus __instance, SystemTypes systemType, byte amount, PlayerControl player)
    {
        if (!FixNextSabo) return;
        FixNextSabo = false;

        switch (systemType)
        {
            case SystemTypes.Reactor:
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 17);
                }
                break;
            case SystemTypes.Laboratory:
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, 67);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, 66);
                }
                break;
            case SystemTypes.LifeSupp:
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, 67);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, 66);
                }
                break;
            case SystemTypes.Comms:
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 17);
                }
                break;
        }
    }
    public override void SwitchSystemUpdate(SwitchSystem __instance, byte amount, PlayerControl player)
    {
        if (!FixNextSabo) return;
        FixNextSabo = false;

        __instance.ActualSwitches = 0;
        __instance.ExpectedSwitches = 0;

        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()} instant - fix-lights", "SwitchSystem");
    }

    public override int AddRealVotesNum(PlayerVoteArea PVA) => (int)Votes;
    public override void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
    {
        for (var i = 0; i < Votes; i++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = votedPlayer.TargetPlayerId,
                VotedForId = votedPlayer.VotedFor
            });
        }
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (LifeConnection)
        {
            var pcList = Main.AllAlivePlayerControls.Where(pc => pc.PlayerId != target.PlayerId && !Pelican.IsEaten(pc.PlayerId) && !Guardian.CannotBeKilled(pc) && !Medic.IsProtected(pc.PlayerId)
            && !pc.Is(CustomRoles.Pestilence) && !pc.Is(CustomRoles.Necromancer) && !pc.Is(CustomRoles.PunchingBag) && !pc.Is(CustomRoles.Solsticer) && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)).ToList();

            if (pcList.Any())
            {
                PlayerControl rp = pcList.RandomElement();
                rp.SetDeathReason(PlayerState.DeathReason.Targeted);
                rp.RpcMurderPlayer(rp);
                rp.SetRealKiller(target);
            }
            LifeConnection = false;
            return true;
        }
        if (!IsProtected) return true;

        killer.SetKillCooldown(time: 5f);
        return false;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || !seer.IsAlive() || isForMeeting || !isForHud) return string.Empty;

        var str = new StringBuilder();
        switch (AntiqueID)
        {
            case 1: // 灵魂回响镜
                str.Append(GetString("PotionStore") + GetString("Ara"));
                break;
            case 2: // 复活圣杯
                str.Append(GetString("PotionStore") + GetString("Arb"));
                break;
            case 3: // 相位斗篷
                str.Append(GetString("PotionStore") + GetString("Arc"));
                break;
            case 4: // 引力石板
                str.Append(GetString("PotionStore") + GetString("Ard"));
                break;
            case 5: // 寒冰宝珠
                str.Append(GetString("PotionStore") + GetString("Are"));
                break;
            case 6: // 战争号角
                str.Append(GetString("PotionStore") + GetString("Arf"));
                break;
            case 7: // 智慧卷轴
                str.Append(GetString("PotionStore") + GetString("Arg"));
                break;
            case 8: // 能量水晶
                str.Append(GetString("PotionStore") + GetString("Arh"));
                break;
            case 9: // 激励圣物
                str.Append(GetString("PotionStore") + GetString("Ari"));
                break;
            case 10: // 契约卷轴
                str.Append(GetString("PotionStore") + GetString("Arj"));
                break;
            case 11: // 牺牲匕首
                str.Append(GetString("PotionStore") + GetString("Ark"));
                break;
            case 12: // 太阳护符
                str.Append(GetString("PotionStore") + GetString("Arl"));
                break;
            case 13: // 真理石板
                str.Append(GetString("PotionStore") + GetString("Arm"));
                break;
            case 14: // 时光沙漏
                str.Append(GetString("PotionStore") + GetString("Arn"));
                break;
            case 16: // 无线按钮
                str.Append(GetString("PotionStore") + GetString("Arp"));
                break;
            case 17: // 万能钥匙
                str.Append(GetString("PotionStore") + GetString("Arq"));
                break;
            default: // just in case
                break;
        }
        if (FixNextSabo) str.Append(GetString("PotionStore") + GetString("Aro"));
        if (UsePets.GetBool()) str.Append("\n" + Utils.GetAbilityTimeDisplay(seer, seen));
        return str.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (!UsePets.GetBool())
        {
            hud.AbilityButton.OverrideText(GetString("ArchaeologistVentButtonText"));
        }
        else
        {
            hud.PetButton.OverrideText(GetString("ArchaeologistVentButtonText"));
        }
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (deadBody == null || deadBody.Object == null) return true;
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;
        if (reporter.Is(CustomRoles.Archaeologist) && _Player?.PlayerId == reporter.PlayerId && HasGrail)
        {
            var deadPlayer = deadBody.Object;
            var deadPlayerId = deadPlayer.PlayerId;
            var deadBodyObject = deadBody.GetDeadBody();

            RevivedPlayerId = deadPlayerId;
            //AllRevivedPlayerId.Add(deadPlayerId);

            deadPlayer.RpcTeleport(deadBodyObject.transform.position);
            deadPlayer.RpcRevive();
            HasGrail = false;
            return false;
        }
        else if ((reporter.PlayerId == RevivedPlayerId) && deadBody.PlayerId == RevivedPlayerId)
        {
            var countDeadBody = UnityEngine.Object.FindObjectsOfType<DeadBody>().Count(bead => bead.ParentId == deadBody.PlayerId);
            if (countDeadBody >= 2) return true;

            reporter.Notify(GetString("Altruist_YouTriedReportRevivedDeadBody"));
            SendRPC(reporter);
            return false;
        }
        return true;
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => seer.Is(CustomRoles.Archaeologist) && SlateRoles.Contains(target.PlayerId);

    public void RandomAntique()
    {
        var player = _Player;
        if (!player.IsAlive() || player == null) return;

        var rand = IRandom.Instance;
        AntiqueID = (byte)rand.Next(1, 18);
        FixNextSabo = false;
        HasGrail = false;
        LifeConnection = false;

        switch (AntiqueID)
        {
            case 1: // 灵魂回响镜 - 知道何时有玩家死亡
                player.Notify(GetString("GotMirror"), 15f);
                break;
            case 2: // 复活圣杯 - 复活一名死亡的玩家
                player.Notify(GetString("GotGrail"), 15f);
                break;
            case 3: // 相位斗篷 - 使你短暂隐形
                player.Notify(GetString("GotCloak"), 15f);
                break;
            case 4: // 引力石板 - 将所有玩家拉向自己位置
                player.Notify(GetString("GotFlagstone"), 15f);
                break;
            case 5: // 寒冰宝珠 - 冻结附近玩家
                player.Notify(GetString("GotOrbs"), 15f);
                break;
            case 6: // 战争号角 - 所有玩家移动速度提升
                player.Notify(GetString("GotBugle"), 15f);
                break;
            case 7: // 智慧卷轴 - 帮一名船员完成一个任务
                player.Notify(GetString("GotReel"), 15f);
                break;
            case 8: // 能量水晶 - 恢复所有船员的技能使用次数
                player.Notify(GetString("GotCrystal"), 15f);
                break;
            case 9: // 激励圣物 - 会议投票时获得额外投票权
                player.Notify(GetString("GotRelic"), 15f);
                break;
            case 10: // 契约卷轴 - 与一名玩家建立生命链接
                player.Notify(GetString("GotIndenture"), 15f);
                break;
            case 11: // 牺牲匕首 - 牺牲自己，为内鬼增加负面效果，为船员增加正面效果
                player.Notify(GetString("GotDagger"), 15f);
                break;
            case 12: // 太阳护符 - 使自己获得伤害免疫
                player.Notify(GetString("GotTalisman"), 15f);
                break;
            case 13: // 真理石板 - 揭示一名玩家的真实身份
                player.Notify(GetString("GotTruth"), 15f);
                break;
            case 14: // 时光沙漏 - 重置所有玩家的技能冷却时间
                player.Notify(GetString("GotHourglass"), 15f);
                break;
            case 15: // 预言卷轴 - 修复冒名顶替者下次破坏
                player.Notify(GetString("GotProphecy"), 15f);
                break;
            case 16: // 无线按钮 - 召开一次会议
                player.Notify(GetString("GetButton"), 15f);
                break;
            case 17: // 万能钥匙 - 打开所有的门
                player.Notify(GetString("GetKey"), 15f);
                break;
            default: // just in case
                break;
        }

        SendRPC(player);
    }
}
