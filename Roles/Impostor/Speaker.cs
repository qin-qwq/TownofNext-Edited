using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Core;
using static TONE.CheckForEndVotingPatch;
using static TONE.Options;
using static TONE.Utils;

namespace TONE.Roles.Impostor;

internal class Speaker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Speaker;
    private const int Id = 1600;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem SkillLimit;

    private static readonly Dictionary<byte, (byte, byte)> Target = [];

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
        Target.Clear();
    }

    public override void Add(byte playerId)
    {
        Target[playerId] = (byte.MaxValue, byte.MaxValue);
        playerId.SetAbilityUseLimit(SkillLimit.GetInt());

        var pc = GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0 && target.PlayerId != Target[killer.PlayerId].Item1)
        {
            return killer.CheckDoubleTrigger(target, () =>
            {
                killer.SetKillCooldown(5f);
                killer.RpcRemoveAbilityUse();
                var currentTuple = Target[killer.PlayerId];
                Target[killer.PlayerId] = (target.PlayerId, currentTuple.Item2);
                NotifyRoles(SpecifyTarget: target);
                SendRPC(0, killer, target);
            });
        }
        else return true;
    }

    private void SendRPC(byte typeId, PlayerControl player, PlayerControl target)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(typeId);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte typeId = reader.ReadByte();
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();
        var b = Target[playerId];

        if (!Target.ContainsKey(playerId))
        {
            Target[playerId] = (byte.MaxValue, byte.MaxValue);
        }

        var currentTuple = Target[playerId];

        switch (typeId)
        {
            case 0:
                Target[playerId] = (targetId, currentTuple.Item2);
                break;
            case 1:
                Target[playerId] = (byte.MaxValue, byte.MaxValue);
                break;
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!Target.ContainsKey(seer.PlayerId))
        {
            Target[seer.PlayerId] = (byte.MaxValue, byte.MaxValue);
        }

        if (Target[seer.PlayerId].Item1 != byte.MaxValue && Target[seer.PlayerId].Item1 == seen.PlayerId)
            return ColorString(GetRoleColor(CustomRoles.Speaker), " ❖");
        return string.Empty;
    }

    public override void AfterMeetingTasks()
    {
        Target[_Player.PlayerId] = (byte.MaxValue, byte.MaxValue);
        SendRPC(1, _Player, _Player);
    }

    public void SwapVotes(MeetingHud __instance)
    {
        if (!Target.ContainsKey(_Player.PlayerId))
        {
            Target[_Player.PlayerId] = (byte.MaxValue, byte.MaxValue);
            return;
        }

        var currentTuple = Target[_Player.PlayerId];

        foreach (var pva in __instance.playerStates.ToArray())
        {
            if (pva.TargetPlayerId == _Player.PlayerId && !pva.AmDead)
            {
                currentTuple.Item2 = pva.VotedFor;
                Target[_Player.PlayerId] = currentTuple;
                break;
            }
        }

        if (currentTuple.Item1 == byte.MaxValue || currentTuple.Item2 >= 252 || currentTuple.Item1 == currentTuple.Item2)
            return;

        foreach (var pva in __instance.playerStates.ToArray())
        {
            if (pva.TargetPlayerId == currentTuple.Item1 && !pva.AmDead)
            {
                pva.VotedFor = currentTuple.Item2;
                ReturnChangedPva(pva);
            }
        }
    }
}
