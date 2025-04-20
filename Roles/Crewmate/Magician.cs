using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Magician : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Magician;
    private const int Id = 33600;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem MagicCooldown;
    private static OptionItem WaitCooldown;

    private static bool MagicTime;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Magician);
        MagicCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(0f, 60f, 2.5f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
            .SetValueFormat(OptionFormat.Seconds);
        WaitCooldown = FloatOptionItem.Create(Id + 11, "WaitCooldown", new(0f, 10f, 1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
            .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Magician);
    }

    public override void Init()
    {
        MagicTime = false;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = MagicCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("MagicianText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Magic");

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (pc.GetPlayerTaskState().IsTaskFinished)
        {
            pc.Notify(GetString("CantMagic"), 5f);
            return;
        }
        pc.Notify(GetString("YouWillDie"), 5f);
        _ = new LateTask(() =>
        {
            pc.RpcMurderPlayer(pc);
            MagicTime = true;
        }, WaitCooldown.GetFloat());
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if ((Main.UnreportableBodies.Contains(player.PlayerId)))
        {
            player.Notify(GetString("CantMagicTwo"), 5f);
            MagicTime = false;
        }
        if (MagicTime)
        {
            player.RpcRevive();
            player.RpcRandomVentTeleport();
            MagicTime = false;
        }
        return true;
    }

    public override void AfterMeetingTasks()
    {
        MagicTime = false;
    }
}
