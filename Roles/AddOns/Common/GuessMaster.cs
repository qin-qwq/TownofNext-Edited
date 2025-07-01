using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class GuessMaster : IAddon
{
    //===========================SETUP================================\\
    public CustomRoles Role => CustomRoles.GuessMaster;
    private const int Id = 26800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public AddonTypes Type => AddonTypes.Guesser;
    //==================================================================\\

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.GuessMaster, canSetNum: true, teamSpawnOptions: true);
    }

    public void Init()
    {
        playerIdList.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public static void OnGuess(CustomRoles role, bool isMisguess = false, PlayerControl dp = null)
    {
        if (!HasEnabled) return;
        foreach (var gmID in playerIdList)
        {
            var gmPC = Utils.GetPlayerById(gmID);
            if (gmPC == null || !gmPC.IsAlive()) continue;
            if (isMisguess && dp != null)
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(string.Format(GetString("GuessMasterMisguess"), dp.GetRealName()), gmID, Utils.ColorString(Utils.GetRoleColor(CustomRoles.GuessMaster), GetString("GuessMaster").ToUpper()));
                }, 1f, "GuessMaster On Miss Guess");
            }
            else
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(string.Format(GetString("GuessMasterTargetRole"), Utils.GetRoleName(role)), gmID, Utils.ColorString(Utils.GetRoleColor(CustomRoles.GuessMaster), GetString("GuessMaster").ToUpper()));
                }, 1f, "GuessMaster Target Role");

            }
        }
    }
}
