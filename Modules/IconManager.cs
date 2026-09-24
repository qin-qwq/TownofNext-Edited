using System;
using System.Text;
using System.Text.RegularExpressions;
using TONE.Roles.AddOns.Common;
using TONE.Roles.Core;
using static TONE.Translator;

namespace TONE;

public static class IconManager
{
    private static readonly Regex RichTextTagRegex = new("<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex ColorTagRegex = new("<color=(?<color>#[0-9a-fA-F]{6,8})>", RegexOptions.Compiled);
    private static readonly Regex LegendLineRegex = new(@"^(?<glyph>\S{1,3})\s*[-–—:]\s*(?<desc>.+)$", RegexOptions.Compiled);

    // 忽略玩家ID，生命值等
    private const string IgnoredSymbols = "()[]{}<>【】〈〉（）「」『』、，。！？；：,.!?;:'\"`-_=+*/\\|&%$#@^~";

    public sealed class IconEntry
    {
        public string Glyph;
        public string Color;
        public string Line;
    }

    // 当前语言的图标图例
    public static List<IconEntry> GetIconEntries() => ParseLegend(GetString("Command.icons"));

    public static string GetVisibleMarks(PlayerControl seer, PlayerControl target)
    {
        if (!seer || !target) return string.Empty;

        var sb = new StringBuilder();

        sb.Append(seer.GetRoleClass()?.GetMark(seer, target, true));
        sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

        if (seer.GetCustomRole().IsImpostor() && target.GetPlayerTaskState().IsTaskFinished)
        {
            if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate))
                sb.Append(CustomRoles.Impostor.GetColoredTextByRole("★"));
        }

        foreach (var subRole in target.GetCustomSubRoles())
        {
            switch (subRole)
            {
                case CustomRoles.Lovers:
                    sb.Append(Lovers.GetMarkOthers(seer, target));
                    break;
                case CustomRoles.Mini:
                    sb.Append(Mini.GetMarkOthers(seer, target));
                    break;
                case CustomRoles.Cyber when Cyber.CyberKnown.GetBool():
                    sb.Append(CustomRoles.Cyber.GetColoredTextByRole("★"));
                    break;
            }
        }

        return sb.ToString();
    }

    public static string GetIconInfoFor(PlayerControl seer, PlayerControl target)
    {
        if (!seer || !target) return string.Empty;

        var marks = GetVisibleMarks(seer, target);
        var entries = GetIconEntries();

        var sb = new StringBuilder();
        sb.Append(string.Format(GetString("Command.iconsPlayer"), target.GetRealName(true), target.PlayerId));

        int described = 0;
        foreach (var entry in entries)
        {
            if (!marks.Contains(entry.Glyph, StringComparison.Ordinal)) continue;

            // 某些图标被多个职业使用（例如 ⦿），筛选出颜色匹配的那个
            if (entries.Count(x => x.Glyph == entry.Glyph) > 1 && entry.Color != null && !ContainsColor(marks, entry.Color)) continue;

            sb.Append('\n').Append(entry.Line);
            described++;
        }

        var unknown = GetUnknownGlyphs(marks, entries);
        if (described == 0 && unknown.Length == 0)
            sb.Append('\n').Append(GetString("Command.iconsPlayerNone"));

        else if (unknown.Length > 0)
            sb.Append('\n').Append(string.Format(GetString("Command.iconsPlayerUnknown"), unknown));

        return sb.ToString();
    }

    public static List<IconEntry> ParseLegend(string legend)
    {
        var entries = new List<IconEntry>();

        if (string.IsNullOrEmpty(legend)) return entries;

        foreach (var rawLine in legend.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var match = LegendLineRegex.Match(StripTags(line));
            if (!match.Success) continue;

            var colorMatch = ColorTagRegex.Match(line);
            entries.Add(new IconEntry
            {
                Glyph = match.Groups["glyph"].Value,
                Color = colorMatch.Success ? colorMatch.Groups["color"].Value : null,
                Line = line,
            });
        }

        return entries;
    }

    // 语言缺失的图标
    private static string GetUnknownGlyphs(string marks, List<IconEntry> entries)
    {
        var sb = new StringBuilder();

        foreach (var c in StripTags(marks))
        {
            if (char.IsWhiteSpace(c) || char.IsLetterOrDigit(c)) continue;
            if (IgnoredSymbols.Contains(c)) continue;
            if (entries.Any(x => x.Glyph.Contains(c))) continue;
            if (sb.ToString().Contains(c)) continue;

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool ContainsColor(string text, string color)
    {
        var hex = NormalizeColor(color);
        return hex.Length != 0 && text.Contains(hex, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return string.Empty;

        var hex = color.Trim().TrimStart('#');
        return hex.Length >= 6 ? hex[..6] : hex;
    }

    private static string StripTags(string text) => text == null ? string.Empty : RichTextTagRegex.Replace(text, string.Empty);
}
