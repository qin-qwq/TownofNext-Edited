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
    RoundUp = 0x05,
    CopsAndRobbers = 0x06,
    BonfireNight = 0x07,

    HidenSeekTONE = 0x99, // HidenSeekTONE must be after other game modes
    All = int.MaxValue
}

public static class CustomGameModeManager
{
    public static readonly CustomGameMode[] AllGameModes = EnumHelper.GetAllValues<CustomGameMode>();
    public static readonly Dictionary<CustomGameMode, GameModeBase> GameModeClass = [];

    public static GameModeBase GetGameModeClass(this CustomGameMode gm) => GameModeClass[gm];

    public static readonly string[] gameModes =
    [
        "Standard",
        "FFA",

        "SpeedRun",
        "TagMode",
        "RoundUp",
        "CopsAndRobbers",

        "Hide&SeekTONE", // HidenSeekTONE must be after other game modes
    ];
}