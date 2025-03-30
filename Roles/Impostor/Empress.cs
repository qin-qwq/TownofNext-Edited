using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Empress : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Empress;
    private const int Id = 32900;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem EmpressShapeshiftCooldown;
    private static OptionItem ShowShapeshiftAnimationsOpt;
    private static OptionItem EnableAwakening;
    private static OptionItem ProgressPerKill;
    private static OptionItem ProgressPerSkill;
    private static OptionItem ProgressPerSecond;

    private static float AwakeningProgress;
    private static bool IsAwakened;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Empress);
        EmpressShapeshiftCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Empress])
            .SetValueFormat(OptionFormat.Seconds);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 11, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Empress]);
        EnableAwakening = BooleanOptionItem.Create(Id + 12, "EnableAwakening", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Empress]);
        ProgressPerKill = FloatOptionItem.Create(Id + 13, "ProgressPerKill", new(0f, 100f, 10f), 40f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSkill = FloatOptionItem.Create(Id + 14, "ProgressPerSkill", new(0f, 100f, 10f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
        ProgressPerSecond = FloatOptionItem.Create(Id + 15, "ProgressPerSecond", new(0.1f, 3f, 0.1f), 0.5f, TabGroup.ImpostorRoles, false)
            .SetParent(EnableAwakening)
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void Init()
    {
        AwakeningProgress = 0;
        IsAwakened = false;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!EnableAwakening.GetBool() || AwakeningProgress >= 100 || GameStates.IsMeeting || isForMeeting) return string.Empty;
        return string.Format(GetString("AwakeningProgress") + ": {0:F0}% / {1:F0}%", AwakeningProgress, 100);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (AwakeningProgress < 100)
        {
            AwakeningProgress += ProgressPerSecond.GetFloat() * Time.fixedDeltaTime;
        }
        else CheckAwakening(player);
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!IsAwakened)
        {
            AwakeningProgress += ProgressPerKill.GetFloat();
        }
        return true;
    }

    private static void CheckAwakening(PlayerControl player)
    {
        if (AwakeningProgress >= 100 && !IsAwakened && EnableAwakening.GetBool() && player.IsAlive())
        {
            IsAwakened = true;
            player.Notify(GetString("SuccessfulAwakening"), 5f);
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = EmpressShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(GetString("EmpressButtonText"));
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Teleport");

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool() || shapeshifter.PlayerId == target.PlayerId) return true;

        DoTP(shapeshifter, target);

        return false;
    }

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting) return;

        DoTP(shapeshifter, target);
    }

    private void DoTP(PlayerControl shapeshifter, PlayerControl target)
    {
        if (target.CanBeTeleported())
        {
            if (!IsAwakened)
            {
                AwakeningProgress += ProgressPerSkill.GetFloat();                
            }
            if (IsAwakened)
            {
                target.SetKillCooldown(5f);
                target.ResetKillCooldown();
            }
            target.RpcTeleport(shapeshifter.GetCustomPosition());

            shapeshifter.RPCPlayCustomSound("Teleport");
            target.RPCPlayCustomSound("Teleport");
        }
        else
        {
            shapeshifter.Notify(ColorString(GetRoleColor(CustomRoles.Empress), GetString("ErrorTeleport")));
        }
    }
}
