using AmongUs.GameOptions;
using static TONE.Options;

namespace TONE.Roles.AddOns.Common;

public class Drunkard : IAddon
{
    public CustomRoles Role => CustomRoles.Drunkard;
    private const int Id = 33800;
    public AddonTypes Type => AddonTypes.Harmful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Drunkard, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        Main.AllPlayerSpeed[playerId] *= -1;
    }
    public void Remove(byte playerId)
    {
        Main.AllPlayerSpeed[playerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        playerId.GetPlayer()?.MarkDirtySettings();
    }
}
