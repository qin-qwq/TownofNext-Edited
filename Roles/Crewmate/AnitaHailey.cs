using AmongUs.GameOptions;
using System.Linq;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class AnitaHailey : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.AnitaHailey;
    private const int Id = 33400;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem APTX4869Cooldown;
    private static OptionItem APTX4869Max;

    private static readonly HashSet<byte> APTX4869Players = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.AnitaHailey);
        APTX4869Cooldown = FloatOptionItem.Create(Id + 10, "APTX4869Cooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.AnitaHailey])
            .SetValueFormat(OptionFormat.Seconds);
        APTX4869Max = IntegerOptionItem.Create(Id + 11, "APTX4869Max", new(1, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.AnitaHailey])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        APTX4869Players.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(APTX4869Max.GetInt());
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = APTX4869Cooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() >= 1;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() < 1) return false;
        if (killer == null || target == null) return false;

        if (target.PlayerId != _Player.PlayerId)
        {
            if (!APTX4869Players.Contains(target.PlayerId))
            {
                APTX4869Players.Add(target.PlayerId);
                killer.RpcRemoveAbilityUse();

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.AnitaHailey), GetString("TargetGetAPTX4869")));
                killer.SetKillCooldown();

                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.AnitaHailey), GetString("YouGetAPTX4869")));
                target.SetKillCooldownV3(300f, forceAnime: !DisableShieldAnimations.GetBool());
                target.ResetKillCooldown();
            }
            else
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.AnitaHailey), GetString("TargetHaveAPTX4869")));
            }
        }

        return false;
    }

    public override void AfterMeetingTasks()
    {
        APTX4869Players.Clear();
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("AnitaHaileyAPTX4869text"));
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (APTX4869Players.Contains(target.PlayerId))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.AnitaHailey), "☯");
        }
        return string.Empty;
    }

    public static bool HaveAPTX4869(byte id) => APTX4869Players.Contains(id);
}
