namespace TONE.Patches;

[HarmonyPatch(typeof(ProgressTracker), nameof(ProgressTracker.FixedUpdate))]
internal static class ProgressTrackerFixedUpdatePatch
{
    public static void Postfix(ProgressTracker __instance)
    {
        if (GameModeBase.GetGameMode().GetGameModeClass().ShowTimerBar && IntroCutsceneDestroyPatch.timerBar && IntroCutsceneDestroyPatch.timerBar.isActiveAndEnabled)
        {
            __instance.gameObject.SetActive(false);
        }
    }
}