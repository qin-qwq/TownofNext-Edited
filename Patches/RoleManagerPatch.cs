namespace TONE.Patches;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.TryAssignSpecialGhostRoles))]
internal static class TryAssignSpecialGhostRolesPatch
{
    public static bool Prefix()
    {
        return false;
    }
}