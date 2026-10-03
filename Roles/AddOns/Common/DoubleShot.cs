using static TONE.Options;

namespace TONE.Roles.AddOns.Common;

public class DoubleShot : IAddon
{
    public CustomRoles Role => CustomRoles.DoubleShot;
    public static readonly HashSet<byte> IsActive = [];
    public AddonTypes Type => AddonTypes.Guesser;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(19200, CustomRoles.DoubleShot, tab: TabGroup.Addons, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    {
        IsActive.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
