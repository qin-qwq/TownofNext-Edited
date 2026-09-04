using System;

namespace TONE;

public class FloatOptionItem(int id, string name, float defaultValue, TabGroup tab, bool isSingleValue, FloatValueRule rule, bool vanilla) : OptionItem(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue, vanillaStr: vanilla)
{
    public FloatValueRule Rule = rule;

    public static FloatOptionItem Create(int id, string name, FloatValueRule rule, float defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new FloatOptionItem(id, name, defaultValue, tab, isSingleValue, rule, vanillaText);
    }
    public static FloatOptionItem Create(int id, Enum name, FloatValueRule rule, float defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new FloatOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue, rule, vanillaText);
    }
    public static FloatOptionItem Create(CustomRoles role, int id, Enum name, FloatValueRule rule, float defaultValue, bool isSingleValue, bool vanillaText = false)
    {
        var tab = role.IsAdditionRole() ? TabGroup.Addons : role switch
        {
            var r when r.IsImpostor() || r.IsMadmate() => TabGroup.ImpostorRoles,
            var r when r.IsCrewmate() => TabGroup.CrewmateRoles,
            var r when r.IsNeutral() => TabGroup.NeutralRoles,
            var r when r.IsCoven() => TabGroup.CovenRoles,
            _ => TabGroup.CrewmateRoles
        };
        var opt = new FloatOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue, rule, vanillaText);
        opt.SetParent(Options.CustomRoleSpawnChances[role]);
        return opt;
    }

    // Getter
    public override int GetInt() => (int)Rule.GetValueByIndex(CurrentValue);
    public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
    public override string GetString()
    {
        return ApplyFormat(((float)((int)(Rule.GetValueByIndex(CurrentValue) * 100) * 1.0) / 100).ToString());
    }
    public override int GetValue()
        => Rule.RepeatIndex(base.GetValue());

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(Rule.RepeatIndex(value), doSync);
    }
}
