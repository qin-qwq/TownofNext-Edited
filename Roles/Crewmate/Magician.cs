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
    private static OptionItem MagicianCantBeGuess;
    private static OptionItem HideBodies;

    private static bool MagicTime;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Magician);
        MagicCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(0f, 60f, 2.5f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
            .SetValueFormat(OptionFormat.Seconds);
        WaitCooldown = FloatOptionItem.Create(Id + 11, "WaitCooldown", new(2f, 10f, 1f), 2f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
            .SetValueFormat(OptionFormat.Seconds);
        MagicianCantBeGuess = BooleanOptionItem.Create(Id + 12, "MagicianCantBeGuess", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Magician]);
        HideBodies = BooleanOptionItem.Create(Id + 13, "HideBodies", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Magician]);
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
            if (HideBodies.GetBool())
            {
                pc.RpcExileV2();
                pc.SetRealKiller(_Player);
                pc.SetDeathReason(PlayerState.DeathReason.Magic);
                Main.PlayerStates[pc.PlayerId].SetDead();
                MagicTime = true;
                return;
            }
            pc.RpcMurderPlayer(pc);
            pc.SetDeathReason(PlayerState.DeathReason.Magic);
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

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.Magician) return false;
        if (MagicianCantBeGuess.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessMagician"));
            return true;
        }
        return false;
    }

    public override void AfterMeetingTasks()
    {
        MagicTime = false;
    }
}
