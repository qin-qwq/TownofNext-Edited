using TONE.Modules;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Impostor;

internal class Inhibitor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Inhibitor;
    private const int Id = 34200;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem InhibitMax;
    private static OptionItem InhibitCDIncrease;

    private static List<byte> InhibitList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Inhibitor);
        InhibitCDIncrease = FloatOptionItem.Create(Id + 11, "InhibitCDIncrease", new(0f, 90f, 5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Inhibitor])
            .SetValueFormat(OptionFormat.Percent);
        InhibitMax = IntegerOptionItem.Create(Id + 12, "InhibitMax", new(1, 30, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Inhibitor])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        InhibitList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(InhibitMax.GetInt());

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0)
        {
            if (target.PlayerId != _Player.PlayerId && target.CanAbilityLimitBeManip())
            {
                return killer.CheckDoubleTrigger(target, () =>
                {
                    killer.SetKillCooldown(5f);
                    killer.RpcRemoveAbilityUse();

                    if (!killer.Is(CustomRoles.Admired))
                    {
                        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Inhibitor), GetString("InhibitorInhibitPlayer")));

                        _ = new LateTask(() =>
                        {
                            if (!GameStates.InGame || target == null || !target.IsAlive()) return;
                            if (!GameStates.IsMeeting)
                            {
                                target.RpcRemoveAbilityUse();
                                var targetCooldown = Main.AllPlayerKillCooldown[target.PlayerId] * (1 + InhibitCDIncrease.GetFloat() / 100);
                                target.SetKillCooldownV3(targetCooldown);
                                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Inhibitor), GetString("PlayerInhibited")));
                            }
                            else if (target != null) InhibitList.Add(target.PlayerId);
                        }, 7f, "Inhibitor Ability Effective");
                    }
                    else
                    {
                        target.RpcIncreaseAbilityUseLimitBy(1);
                        var targetCooldown = Main.AllPlayerKillCooldown[target.PlayerId] * (1 - InhibitCDIncrease.GetFloat() / 100);
                        target.SetKillCooldownV3(targetCooldown);

                        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Catalyst), GetString("CatalystCatalyzePlayer")));
                        target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Catalyst), GetString("PlayerCatalyzed")));

                        killer.RPCPlayCustomSound("Onichian");
                        target.RPCPlayCustomSound("Onichian");
                    }
                });
            }
            else
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Inhibitor), GetString("InhibitorInvalidTarget")));
                return true;
            }
        }
        else return true;
    }
    public override void AfterMeetingTasks()
    {
        foreach (var target in Main.EnumerateAlivePlayerControls())
        {
            if (!InhibitList.Contains(target.PlayerId)) continue;
            target.RpcRemoveAbilityUse();
            var targetCooldown = Main.AllPlayerKillCooldown[target.PlayerId] * (1 + InhibitCDIncrease.GetFloat() / 100);
            target.SetKillCooldownV3(targetCooldown);
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Inhibitor), GetString("PlayerInhibited")));
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("InhibitorInhibitText"));
    }
    // public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Inhibit");
}