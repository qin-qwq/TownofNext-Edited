using AmongUs.GameOptions;
using TONE.Modules;
using TONE.Roles.Core;
using TONE.Roles.Crewmate;
using TONE.Roles.Neutral;
using UnityEngine;
using static TONE.Options;

namespace TONE.Roles.Impostor;

internal class Disturber : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Disturber;
    private const int Id = 34400;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem FogRadius;
    private static OptionItem DisturberAbilityUses;
    private static OptionItem AbilityCooldown;
    private static OptionItem AbilityDuration;

    private readonly Dictionary<Vector2, Fog> FogLocation = [];
    private static readonly Dictionary<byte, Vector2> LastPosition = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Disturber);
        FogRadius = FloatOptionItem.Create(Id + 10, "FogRadius", new(0.5f, 100f, 0.5f), 1f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Disturber])
            .SetValueFormat(OptionFormat.Multiplier);
        DisturberAbilityUses = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(1, 20, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Disturber])
            .SetValueFormat(OptionFormat.Times);
        AbilityCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.AbilityCooldown, new(2.5f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Disturber])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityDuration = FloatOptionItem.Create(Id + 13, GeneralOption.AbilityDuration, new(2.5f, 120f, 2.5f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Disturber])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        FogLocation.Clear();
        LastPosition.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(DisturberAbilityUses.GetInt());
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = AbilityCooldown.GetFloat();
    }

    public override bool OnCheckVanish(PlayerControl phantom)
    {
        if (phantom.GetAbilityUseLimit() < 1) return false;

        var location = phantom.GetCustomPosition();

        phantom.RpcRemoveAbilityUse();
        FogLocation.Add(location, new(location, phantom.PlayerId));
        phantom.Notify(Translator.GetString("FogCreated"));

        _ = new LateTask(() =>
        {
            if (!GameStates.IsInTask) return;
            FogLocation[location].Despawn();
            FogLocation.Remove(location);
        }, AbilityDuration.GetFloat(), "Fog Disperse");

        return false;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        FogLocation.Values.Do(x => x.Despawn());
        FogLocation.Clear();
        LastPosition.Clear();
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.OverrideText(Translator.GetString("DisturberButtonText"));
    }

    private void OnFixedUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (player == null) return;
        if (Pelican.IsEaten(player.PlayerId) || !player.IsAlive() || player.Is(CustomRoles.Disturber) ||
            (player.GetRoleClass() is Lighter li && li.Timer != 0)) return;

        if (FogLocation.Count == 0) return;

        foreach (var kvp in FogLocation)
        {
            if (Utils.GetDistance(kvp.Key, player.GetCustomPosition()) <= FogRadius.GetFloat())
            {
                player.RpcTeleport(LastPosition.GetValueOrDefault(player.PlayerId, player.GetCustomPosition()));
            }
            else
            {
                LastPosition[player.PlayerId] = player.GetCustomPosition();
            }
        }
    }
}