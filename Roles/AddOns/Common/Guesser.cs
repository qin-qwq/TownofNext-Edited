using UnityEngine;
using static TONE.Options;

namespace TONE.Roles.AddOns.Common;

public class Guesser : IAddon
{
    public CustomRoles Role => CustomRoles.Guesser;
    private const int Id = 22200;
    public AddonTypes Type => AddonTypes.Guesser;

    public static OptionItem ImpCanBeGuesser;
    public static OptionItem CrewCanBeGuesser;
    public static OptionItem NeutralCanBeGuesser;
    public static OptionItem CovenCanBeGuesser;
    public static OptionItem GCanGuessAdt;
    public static OptionItem GCanGuessTaskDoneSnitch;
    public static OptionItem AdvancedSettings;
    public static OptionItem GImpMax;
    public static OptionItem GCrewMax;
    public static OptionItem GNeuMax;
    public static OptionItem GCovenMax;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Guesser, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeGuesser = BooleanOptionItem.Create(Id + 10, "ImpCanBeGuesser", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CrewCanBeGuesser = BooleanOptionItem.Create(Id + 11, "CrewCanBeGuesser", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        NeutralCanBeGuesser = BooleanOptionItem.Create(Id + 12, "NeutralCanBeGuesser", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CovenCanBeGuesser = BooleanOptionItem.Create(Id + 16, "CovenCanBeGuesser", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessAdt = BooleanOptionItem.Create(Id + 13, "GCanGuessAdt", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 14, "GCanGuessTaskDoneSnitch", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        AdvancedSettings = BooleanOptionItem.Create(Id + 17, "DoomsayerAdvancedSettings", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GImpMax = IntegerOptionItem.Create(Id + 18, "GuesserImpMax", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(AdvancedSettings);
        GCrewMax = IntegerOptionItem.Create(Id + 19, "GuesserCrewMax", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(AdvancedSettings);
        GNeuMax = IntegerOptionItem.Create(Id + 20, "GuesserNeuMax", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(AdvancedSettings);
        GCovenMax = IntegerOptionItem.Create(Id + 21, "GuesserCovenMax", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(AdvancedSettings);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}

