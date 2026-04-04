using AmongUs.GameOptions;
using UnityEngine;

namespace TONE.Roles.Vanilla;

internal class PhantomTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.PhantomTONE;
    private const int Id = 450;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorVanilla;
    //==================================================================\\

    private static OptionItem InvisCooldown;
    private static OptionItem InvisDuration;

    private (bool, float) IsInvisible = (false, 0);

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.PhantomTONE);
        InvisCooldown = IntegerOptionItem.Create(Id + 2, GeneralOption.PhantomBase_InvisCooldown, new(1, 180, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PhantomTONE])
            .SetValueFormat(OptionFormat.Seconds);
        InvisDuration = IntegerOptionItem.Create(Id + 3, GeneralOption.PhantomBase_InvisDuration, new(5, 180, 5), 30, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PhantomTONE])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add(byte playerId)
    {
        IsInvisible = (false, 0);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = InvisCooldown.GetInt();
        AURoleOptions.PhantomDuration = InvisDuration.GetInt();
    }

    public override bool CanUseKillButton(PlayerControl pc) => !Main.Invisible.Contains(pc.PlayerId);

    public override bool OnCheckVanish(PlayerControl phantom)
    {
        phantom.RpcMakeInvisible(true);
        IsInvisible = (true, InvisDuration.GetInt());
        return false;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (IsInvisible.Item1)
        {
            IsInvisible.Item2 -= Time.fixedDeltaTime;

            if (IsInvisible.Item2 <= 0)
            {
                IsInvisible = (false, 0f);
                PhantomAppear(player);
            }
        }
    }
    
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (IsInvisible.Item1)
        {
            IsInvisible = (false, 0f);
            PhantomAppear(_Player);
        }
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (IsInvisible.Item1)
        {
            IsInvisible = (false, 0f);
            PhantomAppear(target);
        }
    }

    public static void PhantomAppear(PlayerControl phantom)
    {
        if (!phantom) return;
        phantom.RpcMakeVisible(true);
    }
}
