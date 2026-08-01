namespace TONE;

// 17.4 here
[HarmonyPatch(typeof(MapDecor), nameof(MapDecor.Awake))]
class MapDecorPatch
{
    public static void Postfix(MapDecor __instance)
    {
        var halloweenDecorationIsActive = Options.EnableHalloweenDecorations.GetBool();
        var birthdayDecorationIsActive = Options.EnableBirthdayDecorationSkeld.GetBool();
        var halloweenDecorationObject = __instance.transform.FindChild("HalloweenDecorSkeld");
        var birthdayDecorationObject = __instance.transform.FindChild("BirthdayDecorSkeld");

        if (Options.RandomBirthdayAndHalloweenDecorationSkeld.GetBool() && halloweenDecorationIsActive && birthdayDecorationIsActive)
        {
            var random = IRandom.Instance.Next(0, 100);
            if (random < 50)
                halloweenDecorationObject?.gameObject.SetActive(true);
            else
                birthdayDecorationObject?.gameObject.SetActive(true);
            return;
        }
        if (halloweenDecorationIsActive)
            __instance.transform.FindChild("HalloweenDecorSkeld")?.gameObject.SetActive(true);

        if (birthdayDecorationIsActive)
            __instance.transform.FindChild("BirthdayDecorSkeld")?.gameObject.SetActive(true);
    }
}