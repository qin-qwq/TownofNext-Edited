using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class CopyCat : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.CopyCat;
    private const int Id = 11500;
    public static readonly HashSet<byte> playerIdList = [];

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CopyCrewVar;
    private static OptionItem CopyTeamChangingAddon;
    private static OptionItem CopyOnlyEnabledRoles;

    private static float CurrentKillCooldown = new();
    private static readonly Dictionary<byte, List<CustomRoles>> OldAddons = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CopyCat);
        KillCooldown = FloatOptionItem.Create(Id + 10, "CopyCatCopyCooldown", new(0f, 180f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat])
            .SetValueFormat(OptionFormat.Seconds);
        CopyCrewVar = BooleanOptionItem.Create(Id + 13, "CopyCrewVar", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat]);
        CopyTeamChangingAddon = BooleanOptionItem.Create(Id + 14, "CopyTeamChangingAddon", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat]);
        CopyOnlyEnabledRoles = BooleanOptionItem.Create(Id + 15, "CopyOnlyEnabledRoles", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        CurrentKillCooldown = new();
        OldAddons.Clear();
    }

    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
        CurrentKillCooldown = KillCooldown.GetFloat();
        OldAddons[playerId] = [];
    }
    public override void Remove(byte playerId) //only to be used when copycat's role is going to be changed permanently
    {
        // Copy cat role wont be removed for now i guess
        // playerIdList.Remove(playerId);
    }
    public static bool CanCopyTeamChangingAddon() => CopyTeamChangingAddon.GetBool();
    public static bool NoHaveTask(byte playerId, bool ForRecompute) => playerIdList.Contains(playerId) && (playerId.GetPlayer().GetCustomRole().IsDesyncRole() || ForRecompute);
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => playerIdList.Contains(pc.PlayerId);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? CurrentKillCooldown : 300f;
    public static void UnAfterMeetingTasks()
    {
        foreach (var playerId in playerIdList.ToArray())
        {
            var pc = playerId.GetPlayer();
            if (pc == null) continue;

            if (!pc.IsAlive())
            {
                if (!pc.HasGhostRole())
                {
                    pc.RpcSetCustomRole(CustomRoles.CopyCat, false, false);
                }
                continue;
            }
            ////////////           /*remove the settings for current role*/             /////////////////////

            var pcRole = pc.GetCustomRole();
            if (pcRole is not CustomRoles.Sidekick and not CustomRoles.Jackal and not CustomRoles.Refugee && !(!pc.IsAlive() && pcRole is CustomRoles.Retributionist))
            {
                if (pcRole != CustomRoles.CopyCat)
                {
                    pc.GetRoleClass()?.OnRemove(pc.PlayerId);
                    pc.RpcChangeRoleBasis(CustomRoles.CopyCat);
                    pc.RpcSetCustomRole(CustomRoles.CopyCat, false, false);
                    foreach (var addon in OldAddons[pc.PlayerId])
                    {
                        pc.RpcSetCustomRole(addon, false, false);
                    }
                }
            }
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
            OldAddons[pc.PlayerId].Clear();
        }
    }

    private static bool BlackList(CustomRoles role)
    {
        return role is CustomRoles.CopyCat or
            CustomRoles.Doomsayer or // CopyCat cannot guessed roles because he can be know others roles players
            CustomRoles.EvilGuesser or
            CustomRoles.NiceGuesser or
            CustomRoles.Famine;
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        CustomRoles role = target.Is(CustomRoles.Narc) ? CustomRoles.Sheriff : target.GetCustomRole();
        if (BlackList(role))
        {
            killer.Notify(GetString("CopyCatCanNotCopy"));
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            return false;
        }
        if (CopyCrewVar.GetBool())
        {
            role = role switch
            {
                CustomRoles.Stealth or CustomRoles.Medusa or CustomRoles.Pitfall => CustomRoles.Grenadier, // 隐形者，美杜莎 => 掷雷兵
                CustomRoles.TimeThief => CustomRoles.TimeManager, // 蚀时者 => 时间操控者
                CustomRoles.Consigliere => CustomRoles.Overseer, // 军师 => 预言家
                CustomRoles.Mercenary => CustomRoles.Addict, // 嗜血杀手 => 瘾君子
                CustomRoles.Miner => CustomRoles.Mole, // 矿工 => 鼹鼠
                CustomRoles.Godfather => CustomRoles.ChiefOfPolice, // 教父 => 警察局长
                CustomRoles.Twister => CustomRoles.TimeMaster, // 龙卷风 => 时间之主
                CustomRoles.Disperser => CustomRoles.Transporter, // 分散者 => 传送师
                CustomRoles.Eraser => CustomRoles.Cleanser, // 抹除者 => 清洗者
                CustomRoles.Visionary => CustomRoles.Oracle, // 幻想家 => 神谕
                CustomRoles.Workaholic => CustomRoles.Snitch, // 工作狂 => 告密者
                CustomRoles.Sunnyboy => CustomRoles.Doctor, // 阳光开朗大男孩 => 法医
                CustomRoles.Councillor => CustomRoles.Judge, // 邪恶法官 => 法官
                CustomRoles.Taskinator => CustomRoles.Benefactor, // 任务执行者 => 恩人
                CustomRoles.EvilTracker => CustomRoles.TrackerTOHE, // 邪恶追踪者 => 侦查员
                CustomRoles.AntiAdminer => CustomRoles.Telecommunication, // 监管者 => 通信员
                CustomRoles.Pursuer => CustomRoles.Deceiver, // 起诉人 => 赝品商
                CustomRoles.CursedWolf => CustomRoles.Veteran, // 呪狼 => 老兵
                CustomRoles.Swooper or CustomRoles.Wraith => CustomRoles.Chameleon, // 隐匿者，魅影 => 变色龙
                CustomRoles.Vindicator or CustomRoles.Pickpocket => CustomRoles.Mayor, // 卫道士，小偷 => 市长
                CustomRoles.Opportunist or CustomRoles.BloodKnight or CustomRoles.Wildling => CustomRoles.Guardian, // 投机者，嗜血骑士，野人 => 守护者
                CustomRoles.Cultist or CustomRoles.Virus or CustomRoles.Gangster or CustomRoles.Ritualist => CustomRoles.Admirer, // 魅魔，病毒，歹徒，大祭司 => 仰慕者
                CustomRoles.Arrogance or CustomRoles.Juggernaut or CustomRoles.Berserker => CustomRoles.Reverie, // 狂妄杀手，天启，狂战士 => 遐想者
                CustomRoles.Baker when Baker.CurrentBread() is 0 => CustomRoles.Overseer, // 面包师 0 => 预言家
                CustomRoles.Baker when Baker.CurrentBread() is 1 => CustomRoles.Deputy, // 面包师 1 => 捕快
                CustomRoles.Baker when Baker.CurrentBread() is 2 => CustomRoles.Medic, // 面包师 2 => 医生
                CustomRoles.PotionMaster when PotionMaster.CurrentPotion() is 0 => CustomRoles.Overseer, // 药剂师 0 => 预言家
                CustomRoles.PotionMaster when PotionMaster.CurrentPotion() is 1 => CustomRoles.Medic, // 药剂师 1 => 医生
                CustomRoles.Sacrifist => CustomRoles.Alchemist, // 献祭者 => 炼金术士
                CustomRoles.MoonDancer or CustomRoles.Harvester or CustomRoles.Bandit => CustomRoles.Merchant, // 月光舞者，收割者，强盗 => 商人
                CustomRoles.Jinx => CustomRoles.Crusader, // 扫把星 => 十字军
                CustomRoles.Trickster or CustomRoles.Illusionist => CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCrewmate() && !BlackList(role)).ToList().RandomElement(), // 骗术师，幻术师 => 随机
                CustomRoles.Instigator => CustomRoles.Requiter, // 教唆者 => 清算者
                CustomRoles.Jackal => CustomRoles.ChiefOfPolice, // 豺狼 => 警察局长
                CustomRoles.Sidekick => CustomRoles.Sheriff, // 跟班 => 警长
                _ => role
            };
        }
        if (Lich.IsCursed(target)) role = CustomRoles.Lich;
        if (role.IsCrewmate() && (role.IsEnable() || !CopyOnlyEnabledRoles.GetBool()))
        {
            if (role != CustomRoles.CopyCat)
            {
                Dictionary<byte, List<CustomRoles>> CurrentAddons = new()
                {
                    [killer.PlayerId] = []
                };
                foreach (var addon in killer.GetCustomSubRoles())
                {
                    CurrentAddons[killer.PlayerId].Add(addon);
                }

                killer.RpcChangeRoleBasis(role);
                killer.RpcSetCustomRole(role, false, false);
                killer.GetRoleClass()?.OnAdd(killer.PlayerId);
                killer.SyncSettings();

                foreach (var addon in CurrentAddons[killer.PlayerId])
                {
                    if (!CustomRolesHelper.CheckAddonConfilct(addon, killer))
                    {
                        OldAddons[killer.PlayerId].Add(addon);
                        Main.PlayerStates[killer.PlayerId].RemoveSubRole(addon);
                        Logger.Info($"{killer.GetNameWithRole()} had incompatible addon {addon.ToString()}, removing addon", "CopyCat");
                    }
                }
            }
            if (CopyTeamChangingAddon.GetBool())
            {
                if (target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Rascal)) killer.RpcSetCustomRole(CustomRoles.Madmate);
                if (target.Is(CustomRoles.Charmed)) killer.RpcSetCustomRole(CustomRoles.Charmed);
                if (target.Is(CustomRoles.Infected)) killer.RpcSetCustomRole(CustomRoles.Infected);
                if (target.Is(CustomRoles.Recruit)) killer.RpcSetCustomRole(CustomRoles.Recruit);
                if (target.Is(CustomRoles.Contagious)) killer.RpcSetCustomRole(CustomRoles.Contagious);
                if (target.Is(CustomRoles.Soulless)) killer.RpcSetCustomRole(CustomRoles.Soulless);
                if (target.Is(CustomRoles.Admired) || target.Is(CustomRoles.Narc)) killer.RpcSetCustomRole(CustomRoles.Admired);
                if (target.Is(CustomRoles.Enchanted)) killer.RpcSetCustomRole(CustomRoles.Enchanted);
            }
            killer.RpcGuardAndKill(killer);
            killer.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(role)));
            return false;

        }
        killer.Notify(GetString("CopyCatCanNotCopy"));
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return false;
    }
    public static string CopycatReminder(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (playerIdList.Contains(seen.PlayerId) && !seen.Is(CustomRoles.CopyCat) && !seer.IsAlive() && seen.IsAlive())
        {
            return $"<size=1.5><i>{CustomRoles.CopyCat.ToColoredString()}</i></size>";
        }
        return string.Empty;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("CopyButtonText"));
    }
}
