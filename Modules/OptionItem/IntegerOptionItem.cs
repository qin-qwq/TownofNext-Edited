using System;

namespace TONE;

public class IntegerOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, IntegerValueRule rule, bool vanilla) : OptionItem(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue, vanillaStr: vanilla)
{
    // 必須情報
    public IntegerValueRule Rule = rule;

    public static IntegerOptionItem Create(int id, string name, IntegerValueRule rule, int defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new IntegerOptionItem(id, name, defaultValue, tab, isSingleValue, rule, vanillaText);
    }
    public static IntegerOptionItem Create(int id, Enum name, IntegerValueRule rule, int defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new IntegerOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue, rule, vanillaText);
    }
    public static IntegerOptionItem Create(CustomRoles role, int id, Enum name, IntegerValueRule rule, int defaultValue, bool isSingleValue, bool vanillaText = false)
    {
        var tab = role.IsAdditionRole() ? TabGroup.Addons : role switch
        {
            var r when r.IsImpostor() || r.IsMadmate() => TabGroup.ImpostorRoles,
            var r when r.IsCrewmate() => TabGroup.CrewmateRoles,
            var r when r.IsNeutral() => TabGroup.NeutralRoles,
            var r when r.IsCoven() => TabGroup.CovenRoles,
            _ => TabGroup.CrewmateRoles
        };
        var opt = new IntegerOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue, rule, vanillaText);
        opt.SetParent(Options.CustomRoleSpawnChances[role]);
        return opt;
    }

    // Getter
    public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
    public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
    public override string GetString()
    {
        return ApplyFormat(Rule.GetValueByIndex(CurrentValue).ToString());
    }
    public override int GetValue()
        => Rule.RepeatIndex(base.GetValue());

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(Rule.RepeatIndex(value), doSync);
    }
}
