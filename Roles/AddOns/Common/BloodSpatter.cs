using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class BloodSpatter : IAddon
{
    public CustomRoles Role => CustomRoles.BloodSpatter;
    private const int Id = 32400;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem BloodSpatterDuration;

    public static readonly HashSet<byte> PlayerBloodSpatter = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.BloodSpatter, canSetNum: true, teamSpawnOptions: true);
        BloodSpatterDuration = FloatOptionItem.Create(Id + 13, "BloodSpatterDuration", new(0f, 15f, 1f), 8f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.BloodSpatter])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public void Init()
    {
        PlayerBloodSpatter.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    
    public static void BloodSpatterAfterDeathTasks(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId)
        {
            if (target.GetRealKiller() != null)
            {
                if (!target.GetRealKiller().IsAlive()) return;
                killer = target.GetRealKiller();
            }
        }

        if (killer.PlayerId == target.PlayerId) return;

        PlayerBloodSpatter.Add(killer.PlayerId);
        Utils.NotifyRoles(SpecifyTarget: killer);

        _ = new LateTask(() =>
        {
            PlayerBloodSpatter.Remove(killer.PlayerId);
            if (!GameStates.IsMeeting) Utils.NotifyRoles(SpecifyTarget: killer);
        }, BloodSpatterDuration.GetFloat());
    }
}

