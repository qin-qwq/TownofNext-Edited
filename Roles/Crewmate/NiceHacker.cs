using AmongUs.GameOptions;
using System.Text;
using TONE.Modules;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

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

            Dictionary<string, int> list = GetAllPlayerLocationsCount();
            var sb = new StringBuilder();

            foreach (KeyValuePair<string, int> location in list)
                sb.Append($"\n<color=#75fa4c>{location.Key}:</color> {location.Value}");

            player.Notify(sb.ToString(), 1f, hasPriority: true, sendInLog: false);

            if (InAbility.Item2 <= 0)
            {
                InAbility = (false, 0f);
            }
        }
    }

    public static void MapHandle(PlayerControl pc, MapBehaviour map, MapOptions opts)
    {
        map.countOverlayAllowsMovement = true;

        if (pc.GetRoleClass() is NiceHacker nh && nh.InAbility.Item1)
        {
            opts.Mode = MapOptions.Modes.CountOverlay;
            _ = new LateTask(() => { MapCountdown(pc, map, opts, nh.InAbility.Item2); }, 1f, "Hacker.StartCountdown");
        }
    }

    private static void MapCountdown(PlayerControl pc, MapBehaviour map, MapOptions opts, float seconds)
    {
        map.countOverlayAllowsMovement = true;

        if (!map.IsOpen) return;

        if (seconds <= 0)
        {
            map.Close();
            opts.Mode = pc.GetCustomRole().IsMadmate() ? MapOptions.Modes.Sabotage : MapOptions.Modes.Normal;
            return;
        }

        _ = new LateTask(() => { MapCountdown(pc, map, opts, seconds - 1); }, 1f, "HackerAbilityCountdown");
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
