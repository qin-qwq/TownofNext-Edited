using static TONE.Options;

namespace TONE.Roles.AddOns.Common;

public class Guesser : IAddon
{
    public CustomRoles Role => CustomRoles.Guesser;
    private const int Id = 22200;
    public AddonTypes Type => AddonTypes.Guesser;

    public static OptionItem GCanGuessAdt;
    public static OptionItem GCanGuessTaskDoneSnitch;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Guesser, tab: TabGroup.Addons, canSetNum: true, teamSpawnOptions: true);
        GCanGuessAdt = BooleanOptionItem.Create(Id + 20, "GCanGuessAdt", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 21, "GCanGuessTaskDoneSnitch", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}

