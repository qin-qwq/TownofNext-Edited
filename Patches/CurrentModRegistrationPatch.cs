namespace TONE.Patches;

public static class CurrentModRegistrationPatch
{
    public static void AddGuid()
    {
        CurrentModRegistration.ModRegistrationGuidString = "6681dedd-2f2b-4dee-98a1-0a1e74cc9b67";
    }

    public static void RemoveGuid()
    {
        CurrentModRegistration.ModRegistrationGuidString = string.Empty;
    }

    [HarmonyPatch(typeof(CurrentModRegistration), nameof(CurrentModRegistration.TryGetModRegistrationGuid))]
    public static class TryGetModRegistrationGuidPatch
    {
        public static void Postfix(ref bool __result)
        {
            if (!GameStates.IsOnlineGame)
            {
                __result = false;
            }
        }
    }
}