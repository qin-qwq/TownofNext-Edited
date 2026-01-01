using AmongUs.GameOptions;
using TONE.Roles.AddOns;
using static TONE.Options;

namespace TONE.Roles.Crewmate;

internal class Talented : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Talented;
    private const int Id = 32500;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static List<CustomRoles> addons = [];

    private static OptionItem OnlyCanGetEnabledAddons;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Talented);
        OnlyCanGetEnabledAddons = BooleanOptionItem.Create(Id + 10, "OnlyCanGetEnabledAddons", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Talented]);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Talented);
    }

    public override void Init()
    {
        addons.Clear();

        addons.AddRange(GroupedAddons[AddonTypes.Helpful]);
        if (OnlyCanGetEnabledAddons.GetBool())
        {
            addons = addons.Where(role => role.GetMode() != 0).ToList();
        }
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;

        var rd = IRandom.Instance;
        CustomRoles addon = addons.RandomElement();

        player.RpcSetCustomRole(addon, false, false);
        return true;
    }
}
