/*using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Vanisher : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Vanisher;
    private const int Id = 32300;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem VanisherSSCD;
    private static OptionItem VanisherSSDuration;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Vanisher);
        VanisherSSCD = FloatOptionItem.Create(Id + 3, "SwooperCooldown", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vanisher])
            .SetValueFormat(OptionFormat.Seconds);
        VanisherSSDuration = FloatOptionItem.Create(Id + 4, "SwooperDuration", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vanisher])
            .SetValueFormat(OptionFormat.Seconds);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 5, GeneralOption.ShowShapeshiftAnimations, false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vanisher]);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = VanisherSSCD.GetFloat();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("VanisherButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Vanisher");
    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        if (target.IsAlive())
        {
            target.RpcMakeInvisible();
            if (target.IsPlayerImpostorTeam())
            {
                target.Notify(GetString("VanisherMakeYouInvisible"));
                _ = new LateTask(() =>
                {
                    target.Notify(GetString("SwooperInvisStateCountdown"), 3f);
                }, VanisherSSDuration.GetFloat() - 10f);
                _ = new LateTask(() =>
                {
                    target.Notify(GetString("SwooperInvisStateCountdownn"), 3f);
                }, VanisherSSDuration.GetFloat() - 5f);
                _ = new LateTask(() =>
                {
                    target.Notify(GetString("SwooperInvisStateOut"), 5f);
                }, VanisherSSDuration.GetFloat());
            }
            _ = new LateTask(() =>
            {
                target.RpcMakeVisible();
            }, VanisherSSDuration.GetFloat());
        }
        if (ShowShapeshiftAnimationsOpt.GetBool()) return true;
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Bait)) return false;
        if (Main.Invisible.Contains(killer.PlayerId))
        {
            target.RpcMurderPlayer(target);
            target.SetRealKiller(killer);
            killer.SetKillCooldown();
            return true;
        }
        return false;
    }
}*/
