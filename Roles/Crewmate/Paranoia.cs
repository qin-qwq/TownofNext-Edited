using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal partial class Paranoia : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Paranoia;
    private const int Id = 31900;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem ParanoiaNumOfUseButton;

    private static readonly Dictionary<byte, int> ParanoiaUsedButtonCount = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Paranoia);
        ParanoiaNumOfUseButton = IntegerOptionItem.Create(Id + 10, "ParanoiaNumOfUseButton", (1, 20, 1), 2, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        ParanoiaUsedButtonCount.Clear();
    }
    public override void Add(byte playerId)
    {
        ParanoiaUsedButtonCount[playerId] = 0;
    }
    public override void Remove(byte playerId)
    {
        ParanoiaUsedButtonCount[playerId] = 0;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown =
                !ParanoiaUsedButtonCount.TryGetValue(playerId, out var count) || count < ParanoiaNumOfUseButton.GetInt()
                ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                : 300f;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (ParanoiaUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < ParanoiaNumOfUseButton.GetInt())
        {
            ParanoiaUsedButtonCount[pc.PlayerId] += 1;
            pc?.MyPhysics?.RpcBootFromVent(vent.Id);
            pc?.NoCheckStartMeeting(null);
        }
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId)
        => ParanoiaUsedButtonCount.TryGetValue(physics.myPlayer.PlayerId, out var count)
        && count >= ParanoiaNumOfUseButton.GetInt();
}