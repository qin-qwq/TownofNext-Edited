using AmongUs.GameOptions;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.Options;

namespace TONE.Roles.Neutral;

internal class Tunny : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Tunny;
    private const int Id = 32800;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem CanWaitTime;
    private static OptionItem CanWaitTimeAfterMeeting;
    public static OptionItem SnatchesWin;

    private static readonly Dictionary<byte, float> MoveTime = [];
    private static readonly Dictionary<byte, Vector2> NowPosition = [];
    private bool Prevent = true;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Tunny);
        CanWaitTime = IntegerOptionItem.Create(Id + 10, "CanWaitTime", new(1, 20, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tunny])
            .SetValueFormat(OptionFormat.Seconds);
        CanWaitTimeAfterMeeting = IntegerOptionItem.Create(Id + 11, "CanWaitTimeAfterMeeting", new(1, 20, 1), 8, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tunny])
            .SetValueFormat(OptionFormat.Seconds);
        SnatchesWin = BooleanOptionItem.Create(Id + 12, GeneralOption.SnatchesWin, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tunny]);
    }

    public override void Init()
    {
        MoveTime.Clear();
        NowPosition.Clear();
        Prevent = true;
    }
    public override void Add(byte playerId)
    {
        NowPosition[playerId] = Vector2.zero;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (Prevent || !player.CanMove || !player.IsAlive() || !GameStates.IsInTask) return;

        if (player.GetCustomPosition() != NowPosition[player.PlayerId])
        {
            MoveTime[player.PlayerId] = CanWaitTime.GetInt();
            NowPosition[player.PlayerId] = player.GetCustomPosition();
            return;
        }

        MoveTime[player.PlayerId] -= Time.fixedDeltaTime;
        if (MoveTime[player.PlayerId] <= 0 && NowPosition[player.PlayerId] == player.GetCustomPosition())
        {
            player.RpcMurderPlayer(player);
            player.SetDeathReason(PlayerState.DeathReason.Suffocate);
        }
    }

    public override void AfterMeetingTasks()
    {
        Prevent = true;
        _ = new LateTask(() =>
        {
            Prevent = false;
            MoveTime[_Player.PlayerId] = CanWaitTime.GetInt();
        }, CanWaitTimeAfterMeeting.GetInt(), "TunnyPreventOff");
    }
}