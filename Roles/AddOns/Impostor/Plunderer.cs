using static TONE.Options;

namespace TONE.Roles.AddOns.Impostor;

public class Plunderer : IAddon
{
    public CustomRoles Role => CustomRoles.Plunderer;
    private const int Id = 34000;
    public AddonTypes Type => AddonTypes.Impostor;

    private static OptionItem CantGetHarmful;
    private static OptionItem CantGetRecruiting;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Plunderer, canSetNum: true, tab: TabGroup.Addons);
        CantGetHarmful = BooleanOptionItem.Create(Id + 10, "Plunderer.CantGetHarmful", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Plunderer]);
        CantGetRecruiting = BooleanOptionItem.Create(Id + 11, "Plunderer.CantGetRecruiting", true, TabGroup.Addons, false)
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
            if (addon is CustomRoles.Lovers
                or CustomRoles.Knighted
                or CustomRoles.Cleansed
                or CustomRoles.Workhorse
                or CustomRoles.LastImpostor
                or CustomRoles.Cyber) continue;
            if (CantGetRecruiting.GetBool() && addon.IsBetrayalAddonV2()) continue;
            if (CantGetHarmful.GetBool() && GroupedAddons[AddonTypes.Harmful].Contains(addon)) continue;

            killer.RpcSetCustomRole(addon);
        }
    }
}
