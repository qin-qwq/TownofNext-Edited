using AmongUs.GameOptions;
using System.Text;
using TONE.Modules;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Crewmate;

internal class NiceHacker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.NiceHacker;
    private const int Id = 33700;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => UsePets.GetBool() ? CustomRoles.Crewmate : CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateInvestigative;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    public static OptionItem HackerLimit;
    public static OptionItem HackerCooldown;
    public static OptionItem HackerDuration;

    public (bool, float) InAbility = (false, 0);

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceHacker);
        HackerLimit = FloatOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceHacker])
            .SetValueFormat(OptionFormat.Times);
        HackerCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.AbilityCooldown, new(0f, 60f, 1f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceHacker])
            .SetValueFormat(OptionFormat.Seconds);
        HackerDuration = FloatOptionItem.Create(Id + 14, GeneralOption.AbilityDuration, new(0f, 60f, 1f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceHacker])
            .SetValueFormat(OptionFormat.Seconds);
        NiceHackerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 16, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NiceHacker])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = HackerCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override void Init()
    {
        InAbility = (false, 0);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(HackerLimit.GetFloat());
        InAbility = (false, 0);
    }

    public override void OnPet(PlayerControl pc)
    {
        OnEnterVent(pc, null);
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (InAbility.Item1) return;
        if (pc.GetAbilityUseLimit() <= 0)
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
            return;
        }

        pc.RpcRemoveAbilityUse();
        InAbility = (true, HackerDuration.GetFloat());
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (InAbility.Item1)
        {
            InAbility.Item2 -= Time.fixedDeltaTime;

            var admins = AdminProvider.CalculateAdmin();
            var builder = new StringBuilder(512);

            foreach (var admin in admins)
            {
                var entry = admin.Value;
                if (entry.TotalPlayers <= 0)
                {
                    continue;
                }

                builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
                builder.Append(": ");
                builder.Append(entry.TotalPlayers);

                builder.Append('\n');
            }

            if (!builder.ToString().IsNullOrWhiteSpace()) player.Notify(builder.ToString(), 1f, hasPriority: true, sendInLog: false);

            if (InAbility.Item2 <= 0 || !player.IsAlive())
            {
                if (player.IsModded() && MapBehaviour.Instance)
                {
                    if (MapBehaviour.Instance.IsOpen) MapBehaviour.Instance.Close();
                }
                InAbility = (false, 0f);
            }
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        if (!UsePets.GetBool())
        {
            hud.AbilityButton.buttonLabelText.text = GetString("NiceHackerVentButtonText");
            hud.AbilityButton.SetUsesRemaining((int)id.GetAbilityUseLimit());
        }
        else
        {
            hud.PetButton.OverrideText(GetString("NiceHackerVentButtonText"));
        }
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting)
    {
        if (!UsePets.GetBool()) return CustomButton.Get("NiceHacker");
        return null;
    }
    public override Sprite GetPetButtonSprite(PlayerControl player)
    {
        if (UsePets.GetBool()) return CustomButton.Get("NiceHacker");
        return null;
    }
}
