using UnityEngine;

namespace TONE.Patches;

// Thanks Galster (https://github.com/Galster-dev)

/*
 * Info for those who port this code to their mod or view the code
 * We patch CoStartGameHost so it's not work now in normal game
 * But work for AU code
 * So not used, execpt vanilla Hide&Seek
*/

[HarmonyPatch(typeof(GameStartManager))]
class AllMapIconsPatch
{
    // Vanilla players getting error when trying get dleks map icon
    [HarmonyPatch(nameof(GameStartManager.Start)), HarmonyPostfix]
    [Obfuscation(Exclude = true)]
    public static void Postfix_AllMapIcons(GameStartManager __instance)
    {
        if (__instance == null) return;

        if (GameStates.IsNormalGame && Main.NormalOptions.MapId == 3)
        {
            Main.NormalOptions.MapId = 0;
            __instance.UpdateMapImage(MapNames.Skeld);

            if (!Options.RandomMapsMode.GetBool())
                CreateOptionsPickerPatch.SetDleks = true;
        }
        else if (GameStates.IsHideNSeek && Main.HideNSeekOptions.MapId == 3)
        {
            Main.HideNSeekOptions.MapId = 0;
            __instance.UpdateMapImage(MapNames.Skeld);

            if (!Options.RandomMapsMode.GetBool())
                CreateOptionsPickerPatch.SetDleks = true;
        }

        MapIconByName DleksIncon = Object.Instantiate(__instance, __instance.gameObject.transform).AllMapIcons[0];
        DleksIncon.Name = MapNames.Dleks;
        DleksIncon.MapImage = Utils.LoadSprite($"TONE.Resources.Images.DleksBanner.png", 100f);
        DleksIncon.NameImage = Utils.LoadSprite($"TONE.Resources.Images.DleksBanner-Wordart.png", 100f);

        __instance.AllMapIcons.Add(DleksIncon);
    }
}
[HarmonyPatch(typeof(StringOption), nameof(StringOption.Start))]
class AutoSelectDleksPatch
{
    private static void Postfix(StringOption __instance)
    {
        if (__instance.Title == StringNames.GameMapName)
        {
            // vanilla clamps this to not auto select dleks
            __instance.Value = GameOptionsManager.Instance.CurrentGameOptions.MapId;
        }
    }
}
