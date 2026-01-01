using static TONE.Options;

namespace TONE.Roles.AddOns.Impostor;

public class Plunderer : IAddon
{
    public CustomRoles Role => CustomRoles.Plunderer;
    private const int Id = 34000;
    public AddonTypes Type => AddonTypes.Impostor;

    private static OptionItem CantGetRecruiting;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Plunderer, canSetNum: true, tab: TabGroup.Addons);
        CantGetRecruiting = BooleanOptionItem.Create(Id + 10, "Plunderer.CantGetRecruiting", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Plunderer]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        foreach (var addon in target.GetCustomSubRoles())
        {
            if (!CustomRolesHelper.CheckAddonConfilct(addon, killer, checkLimitAddons: false, checkConditions: false)) continue;
            if (CantGetRecruiting.GetBool() && addon.IsBetrayalAddonV2()) continue;

            killer.RpcSetCustomRole(addon);
        }
    }
}
