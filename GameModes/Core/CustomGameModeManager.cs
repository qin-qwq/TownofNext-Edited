using System;

namespace TONE;

[Obfuscation(Exclude = true)]
[Flags]
public enum CustomGameMode
{
    Standard = 0x01,
    FFA = 0x02,
    SpeedRun = 0x03,
    TagMode = 0x04,
    BonfireNight = 0x05,

    HidenSeekTONE = 0x08, // HidenSeekTONE must be after other game modes
    All = int.MaxValue
}

public static class CustomGameModeManager
{
    public static readonly CustomGameMode[] AllGameModes = EnumHelper.GetAllValues<CustomGameMode>();
    public static readonly Dictionary<CustomGameMode, GameModeBase> GameModeClass = [];

    public static GameModeBase GetGameModeClass(this CustomGameMode gm) => GameModeClass[gm];

    public static CustomGameMode CurrentGameMode
        => Options.GameMode.GetInt() switch
        {
            1 => CustomGameMode.FFA,

            2 => CustomGameMode.SpeedRun,
            3 => CustomGameMode.TagMode,
            4 => CustomGameMode.BonfireNight,
            5 => CustomGameMode.HidenSeekTONE, // HidenSeekTONE must be after other game modes
            _ => CustomGameMode.Standard
        };

    public static readonly string[] gameModes =
    [
        "Standard",
        "FFA",

        "SpeedRun",
        "TagMode",
        "BonfireNight",

        "Hide&SeekTONE", // HidenSeekTONE must be after other game modes
    ];
}