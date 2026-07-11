using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using static TONE.Options;
using static TONE.Utils;

namespace TONE.Roles.Impostor;

internal class Speaker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Speaker;
    private const int Id = 1600;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem SkillLimit;

    private static readonly HashSet<byte> PlayerList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Speaker);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Speaker])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimit = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(1, 15, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Speaker])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        PlayerList.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(SkillLimit.GetInt());

        var pc = GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0 && !PlayerList.Contains(target.PlayerId))
        {
            return killer.CheckDoubleTrigger(target, () =>
            {
                killer.RpcRemoveAbilityUse();
                PlayerList.Add(target.PlayerId);
                NotifyRoles(SpecifyTarget: target);
                SendRPC(target.PlayerId);
            });
        }
        else return true;
    }

    private void SendRPC(byte targetId = 255)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(targetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte targetId = reader.ReadByte();

        if (targetId != 255)
        {
            PlayerList.Add(targetId);
        }
        else
        {
            PlayerList.Clear();
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (PlayerList.Contains(seen.PlayerId))
        {
            return ColorString(GetRoleColor(CustomRoles.Speaker), " ❖");
        }

        return string.Empty;
    }

    public override void AfterMeetingTasks()
    {
        PlayerList.Clear();
        SendRPC();
    }

    public static bool IsSpoken(byte target)
    {
        if (PlayerList.Count < 1) return false;
        if (PlayerList.Contains(target)) return true;
        return false;
    }
}
