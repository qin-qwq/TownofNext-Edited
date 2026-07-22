using AmongUs.GameOptions;
using TONE.Modules;
using UnityEngine;
using static TONE.Options;

namespace TONE.Roles.Neutral;

internal class Dreamer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Dreamer;
    private const int Id = 33600;
    public override bool IsExperimental => true;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    public static OptionItem FantasyCooldown;
    public static OptionItem FantasyDuration;
    private static OptionItem FantasySpeed;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    private (bool, long) SkillTime = (false, 0);
    private Vector2 RealPosition;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Dreamer, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dreamer])
            .SetValueFormat(OptionFormat.Seconds);
        FantasyCooldown = IntegerOptionItem.Create(Id + 12, "FantasyCooldown", new(1, 180, 1), 25, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dreamer])
            .SetValueFormat(OptionFormat.Seconds);
        FantasyDuration = IntegerOptionItem.Create(Id + 13, "FantasyDuration", new(1, 180, 1), 15, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dreamer])
            .SetValueFormat(OptionFormat.Seconds);
        FantasySpeed = FloatOptionItem.Create(Id + 14, "FantasySpeed", new(0f, 5f, 0.25f), 1.75f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dreamer])
            .SetValueFormat(OptionFormat.Multiplier);
        CanVent = BooleanOptionItem.Create(Id + 16, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dreamer]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 17, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dreamer]);
    }
    public override void Init()
    {
        SkillTime = (false, 0);
        RealPosition = Vector2.zero;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool() && !SkillTime.Item1;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(HasImpostorVision.GetBool());
        AURoleOptions.ShapeshifterCooldown = 1f;

        var speed = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        Main.AllPlayerSpeed[playerId] = SkillTime.Item1 ? FantasySpeed.GetFloat() : speed;
        AURoleOptions.PlayerSpeedMod = SkillTime.Item1 ? FantasySpeed.GetFloat() : speed;
    }

    public override void UnShapeShiftButton(PlayerControl pc)
    {
        if (pc.HasAbilityCD()) return;

        pc.FreezeForOthers();
        SkillTime = (true, Utils.GetTimeStamp());
        RealPosition = pc.GetCustomPosition();
        pc.RpcAddAbilityCD(includeDuration: true);
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!SkillTime.Item1) return true;

        if (!killer.RpcCheckAndMurder(target, true)) return false;

        RPC.PlaySoundRPC(Sounds.KillSound, killer.PlayerId);
        killer.RpcGuardAndKill(target);
        killer.SetKillCooldown();

        target.RpcMurderPlayer(target);
        target.SetRealKiller(killer);
        target.SetDeathReason(PlayerState.DeathReason.Drained);
        return false;
    }

    public override void OnFixedUpdate(PlayerControl pc, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;

        if (SkillTime.Item1)
        {
            if (SkillTime.Item2 + FantasyDuration.GetInt() < nowTime)
            {
                pc.RevertFreeze(RealPosition);
                SkillTime = (false, 0);
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (SkillTime.Item1)
        {
            _Player.RevertFreeze(RealPosition);
            SkillTime = (false, 0);
        }
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (SkillTime.Item1)
        {
            target.RevertFreeze(RealPosition);
            SkillTime = (false, 0);
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(Translator.GetString("DreamerText"));
    }
}
