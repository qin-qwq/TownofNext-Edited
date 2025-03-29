using static TOHE.RoleBase;

namespace TOHE.Roles.AddOns.Impostor;

internal class Underdog : IAddon
{
    //===========================SETUP================================\\
    public CustomRoles Role => CustomRoles.Underdog;
    private const int Id = 2700;
    public AddonTypes Type => AddonTypes.Impostor;
    //==================================================================\\

    public static OptionItem UnderdogMaximumPlayersNeededToKill;
    public static OptionItem UnderdogKillMaxCooldown;
    public static OptionItem UnderdogKillMinCooldown;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Underdog, canSetNum: true, tab: TabGroup.Addons);
        UnderdogMaximumPlayersNeededToKill = IntegerOptionItem.Create(Id + 5, "UnderdogMaximumPlayersNeededToKill", new(1, 15, 1), 9, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Players);
        UnderdogKillMaxCooldown = FloatOptionItem.Create(Id + 6, "UnderdogKillMaxCooldown", new(0f, 180f, 2.5f), 35f, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Seconds);
        UnderdogKillMinCooldown = FloatOptionItem.Create(Id + 7, "UnderdogKillMinCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Underdog])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}

