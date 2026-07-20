using System;
using System.IO;
using System.Text.Json;

namespace TONE.Modules;

public static class FactionOption
{
    private static readonly string FactionOptionFiles = @$"{Main.Path}/TONE-DATA/FactionOption.json";

    private static List<FactionOptionData> FactionOptionList = [];
    private static List<FactionOptionData> GetDefault() =>
    [
        new() { MinPlayers = 10, MaxPlayers = 12, MaxImpostors = 2, MaxNonNeutralKilling = 1, MaxNeutralKilling = 1, MaxNeutralApocalypse = 0, MaxCovens = 0 },
        new() { MinPlayers = 13, MaxPlayers = 14, MaxImpostors = 3, MaxNonNeutralKilling = 1, MaxNeutralKilling = 1, MaxNeutralApocalypse = 0, MaxCovens = 0 },
        new() { MinPlayers = 15, MaxPlayers = 15, MaxImpostors = 3, MaxNonNeutralKilling = 2, MaxNeutralKilling = 1, MaxNeutralApocalypse = 0, MaxCovens = 0 },
    ];

    public static void Load()
    {
        if (!File.Exists(FactionOptionFiles))
        {
            FactionOptionList = GetDefault();
            Save();
            return;
        }

        var json = File.ReadAllText(FactionOptionFiles);
        var loaded = JsonSerializer.Deserialize<List<FactionOptionData>>(json);
        if (loaded != null && loaded.Count > 0)
            FactionOptionList = loaded;
        else
            FactionOptionList = GetDefault();
    }

    public static void Save()
    {
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;

        try
        {
            var json = JsonSerializer.Serialize(FactionOptionList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FactionOptionFiles, json);
        }
        catch (Exception error)
        {
            Logger.Error($"Error: {error}", "FactionOption.Save");
        }
    }

    public static FactionOptionData GetConfig(int playerCount)
    {
        return FactionOptionList.FirstOrDefault(c => playerCount >= c.MinPlayers && playerCount <= c.MaxPlayers) ?? null;
    }

    public static void ChangeSettings()
    {
        var config = GetConfig(Main.AllPlayerControls.Count);
        if (!Options.ChangeFactionSettings.GetBool() || config == null)
            return;

        if (Options.UseVariableImp.GetBool())
        {
            SetMinMaxOptions(Options.ImpRolesMinPlayer, Options.ImpRolesMaxPlayer, config.MaxImpostors);
        }
        else
        {
            if (Main.NormalOptions.NumImpostors != config.MaxImpostors)
                Main.NormalOptions.NumImpostors = config.MaxImpostors;
        }

        SetMinMaxOptions(Options.NonNeutralKillingRolesMinPlayer, Options.NonNeutralKillingRolesMaxPlayer, config.MaxNonNeutralKilling);
        SetMinMaxOptions(Options.NeutralKillingRolesMinPlayer, Options.NeutralKillingRolesMaxPlayer, config.MaxNeutralKilling);
        SetMinMaxOptions(Options.NeutralApocalypseRolesMinPlayer, Options.NeutralApocalypseRolesMaxPlayer, config.MaxNeutralApocalypse);
        SetMinMaxOptions(Options.CovenRolesMinPlayer, Options.CovenRolesMaxPlayer, config.MaxCovens);

        Logger.Info($"Change Faction Setting: Imp: {config.MaxImpostors}, NNK: {config.MaxNonNeutralKilling}, NK: {config.MaxNeutralKilling}, NA: {config.MaxNeutralApocalypse}, Coven: {config.MaxCovens}", "FactionOption.ChangeSettings");
    }

    private static void SetMinMaxOptions(OptionItem minOption, OptionItem maxOption, int newValue)
    {
        SetOptionIfChanged(minOption, newValue);
        SetOptionIfChanged(maxOption, newValue);
    }

    private static void SetOptionIfChanged(OptionItem option, int newValue)
    {
        if (option.GetInt() != newValue)
            option.SetValue(newValue);
    }

    public class FactionOptionData
    {
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int MaxImpostors { get; set; }
        public int MaxNonNeutralKilling { get; set; }
        public int MaxNeutralKilling { get; set; }
        public int MaxNeutralApocalypse { get; set; }
        public int MaxCovens { get; set; }
    }
}