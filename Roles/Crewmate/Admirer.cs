using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.AddOns.Common;
using TONE.Roles.Core;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Crewmate;

internal class Admirer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Admirer;
    private const int Id = 24800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Admirer);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem AdmireCooldown;
    private static OptionItem KnowTargetRole;
    private static OptionItem SkillLimit;
    private static OptionItem CanAdmireImp;
    private static OptionItem CanAdmireCrew;
    private static OptionItem CanAdmireNeutral;
    private static OptionItem CanAdmireCoven;
    private static OptionItem MisfireAdmireTarget;
    public static OptionItem CanAdmireBeforeFirstMeeting;
    enum OptionName
    {
        AdmireCooldown,
        AdmirerKnowTargetRole,
        AdmirerSkillLimit,
        CanAdmireImp,
        CanAdmireCrew,
        CanAdmireNeutral,
        CanAdmireCoven,
        MisfireAdmireTarget,
        CanAdmireBeforeFirstMeeting
    }
    public static readonly Dictionary<byte, HashSet<byte>> AdmiredList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Admirer);
        AdmireCooldown = FloatOptionItem.Create(Role, Id + 10, OptionName.AdmireCooldown, new(1f, 180f, 1f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Role, Id + 11, OptionName.AdmirerKnowTargetRole, true, false);
        SkillLimit = IntegerOptionItem.Create(Role, Id + 12, OptionName.AdmirerSkillLimit, new(0, 100, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        CanAdmireImp = BooleanOptionItem.Create(Role, Id + 13, OptionName.CanAdmireImp, true, false);
        CanAdmireCrew = BooleanOptionItem.Create(Role, Id + 14, OptionName.CanAdmireCrew, true, false);
        CanAdmireNeutral = BooleanOptionItem.Create(Role, Id + 15, OptionName.CanAdmireNeutral, true, false);
        CanAdmireCoven = BooleanOptionItem.Create(Role, Id + 16, OptionName.CanAdmireCoven, true, false);
        MisfireAdmireTarget = BooleanOptionItem.Create(Role, Id + 17, OptionName.MisfireAdmireTarget, true, false);
        CanAdmireBeforeFirstMeeting = BooleanOptionItem.Create(Role, Id + 18, OptionName.CanAdmireBeforeFirstMeeting, true, false);
    }
    public override void Init()
    {
        AdmiredList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(SkillLimit.GetInt());
        AdmiredList[playerId] = [];
    }
    public override void Remove(byte playerId)
    {
        AdmiredList.Remove(playerId);
    }
    public static void SendRPC(byte playerId, byte targetId)
    {
        var msg = new RpcSyncAdmiredList(PlayerControl.LocalPlayer.NetId, playerId, targetId);
        RpcUtils.LateBroadcastReliableMessage(msg);

    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        if (!AdmiredList.ContainsKey(playerId))
            AdmiredList.Add(playerId, []);
        else AdmiredList[playerId].Add(targetId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetAbilityUseLimit() >= 1 ? AdmireCooldown.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => (CanAdmireBeforeFirstMeeting.GetBool() || !MeetingStates.FirstMeeting) && player.GetAbilityUseLimit() >= 1;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() < 1) return false;
        if (Mini.Age < 18 && target.Is(CustomRoles.Mini))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }

        if (!AdmiredList.ContainsKey(killer.PlayerId))
            AdmiredList.Add(killer.PlayerId, []);

        var addon = killer.GetBetrayalAddon(true);
        if (killer.GetAbilityUseLimit() > 0)
        {
            if (target.CanBeRecruitedBy(killer))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + addon.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(addon);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("AdmirerAdmired")));
                if (KnowTargetRole.GetBool())
                {
                    AdmiredList[killer.PlayerId].Add(target.PlayerId);
                    SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
                }
            }
            else goto AdmirerFailed;

            killer.RpcRemoveAbilityUse();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);

            target.RpcGuardAndKill(killer);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);

            Logger.Info(target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Admirer.ToString(), "Assign " + CustomRoles.Admirer.ToString());

            return false;
        }

    AdmirerFailed:

        killer.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("AdmirerInvalidTarget")));
        if (MisfireAdmireTarget.GetBool())
        {
            killer.SetDeathReason(PlayerState.DeathReason.Misfire);
            killer.RpcMurderPlayer(killer);
            return false;
        }
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return false;
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => CheckKnowRoleTarget(seer, target) && !Main.PlayerStates[seer.PlayerId].IsNecromancer;

    public static bool CheckKnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        if (AdmiredList.ContainsKey(seer.PlayerId))
        {
            if (AdmiredList[seer.PlayerId].Contains(target.PlayerId)) return true;
            return false;
        }
        else if (AdmiredList.ContainsKey(target.PlayerId))
        {
            if (AdmiredList[target.PlayerId].Contains(seer.PlayerId)) return true;
            return false;
        }
        else return false;
    }

    public static bool CanBeAdmired(PlayerControl target, PlayerControl pc)
    {
        if ((target.IsPlayerImpostorTeam() && !CanAdmireImp.GetBool()) || (target.IsPlayerCrewmateTeam() && !CanAdmireCrew.GetBool())
            || (target.IsPlayerNeutralTeam() && !CanAdmireNeutral.GetBool()) || (target.IsPlayerCovenTeam() && !CanAdmireCoven.GetBool()))
            return false;
        if (AdmiredList.ContainsKey(pc.PlayerId))
        {
            if (AdmiredList[pc.PlayerId].Contains(target.PlayerId))
                return false;
        }
        else AdmiredList.Add(pc.PlayerId, []);

        return target != null && !target.Is(CustomRoles.Narc);
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(GetString("AdmireButtonText"));
    }
}
