using Hazel;
using System.Text;

namespace TONE;

public abstract class GameModeBase
{
    public abstract CustomGameMode GameMode { get; }
    public virtual bool NormalSelectRoles => false;
    public virtual bool NormalSelectAddons => false;
    public virtual bool CanCloseDoors => false;
    public virtual bool CanReport => false;
    public virtual bool NormalTaskText => false;
    public virtual bool OpeningHours => true;

    public static CustomGameMode GetGameMode()
    {
        if (Options.CurrentGameMode == CustomGameMode.RoundUp) return CustomGameMode.Standard;
        return Options.CurrentGameMode;
    }

    /// <summary>
    /// Variable resets when the game starts
    /// </summary>
    public virtual void Init()
    { }
    /// <summary>
    /// When Role is applied in the game, beginning or during the game
    /// </summary>
    public virtual void Add()
    { }

    public virtual void SetupCustomOption()
    { }
    public virtual void SelectRoles()
    { }
    public virtual void SetPredicate() => GameEndCheckerForNormal.predicate = new GameEndCheckerForNormal.NormalGameEndPredicate();

    public virtual void ReceiveRPC(MessageReader reader)
    { }

    public virtual string GetGameState(string taskText = null, bool forGameEnd = false) => string.Empty;
    public virtual void SummaryText(StringBuilder sb, List<byte> cloneRoles, bool sendMessage = false)
    {
        if (!sendMessage) sb.Append($"</b>\n");
        foreach (byte id in cloneRoles.ToArray())
        {
            if (!EndGamePatch.SummaryText.TryGetValue(id, out string loser)) continue;
            if (loser.Contains("<INVALID:NotAssigned>")) continue;
            if (!sendMessage) sb.Append('\n').Append(loser);
            else sb.Append($"\n　 ").Append(loser);
        }
    }
    public virtual void AppendKcount(StringBuilder sub)
    {
        var allAlivePlayers = Main.EnumerateAlivePlayerControls();
        int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor) && !pc.Is(CustomRoles.Narc));
        int madnum = allAlivePlayers.Count(pc => (pc.GetCustomRole().IsMadmate() && !pc.Is(CustomRoles.Narc)) || pc.Is(CustomRoles.Madmate));
        int neutralnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNK());
        int apocnum = allAlivePlayers.Count(pc => pc.IsNeutralApocalypse() || pc.IsTransformedNeutralApocalypse());
        int covnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Coven));

        sub.Append(string.Format(Translator.GetString("Remaining.ImpostorCount"), impnum));

        if (Options.ShowMadmatesInLeftCommand.GetBool())
            sub.Append(string.Format("\n\r" + Translator.GetString("Remaining.MadmateCount"), madnum));

        if (Options.ShowApocalypseInLeftCommand.GetBool())
            sub.Append(string.Format("\n\r" + Translator.GetString("Remaining.ApocalypseCount"), apocnum));

        if (Options.ShowCovenInLeftCommand.GetBool())
            sub.Append(string.Format("\n\r" + Translator.GetString("Remaining.CovenCount"), covnum));

        sub.Append(string.Format("\n\r" + Translator.GetString("Remaining.NeutralCount"), neutralnum));
    }
}