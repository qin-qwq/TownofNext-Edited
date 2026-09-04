using System;

namespace TONE;

public class BooleanOptionItem(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanilla) : OptionItem(id, name, defaultValue ? 1 : 0, tab, isSingleValue, vanillaStr: vanilla)
{
    public const string TEXT_true = "ColoredOn";
    public const string TEXT_false = "ColoredOff";

    public static BooleanOptionItem Create(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new BooleanOptionItem(id, name, defaultValue, tab, isSingleValue, vanillaText);
    }
    public static BooleanOptionItem Create(int id, Enum name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new BooleanOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue, vanillaText);
    }
    public static BooleanOptionItem Create(CustomRoles role, int id, Enum name, bool defaultValue, bool isSingleValue, bool vanillaText = false)
    {
        var tab = role.IsAdditionRole() ? TabGroup.Addons : role switch
        {
            var r when r.IsImpostor() || r.IsMadmate() => TabGroup.ImpostorRoles,
            var r when r.IsCrewmate() => TabGroup.CrewmateRoles,
            var r when r.IsNeutral() => TabGroup.NeutralRoles,
            var r when r.IsCoven() => TabGroup.CovenRoles,
            _ => TabGroup.CrewmateRoles
        };
        var opt = new BooleanOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue, vanillaText);
        opt.SetParent(Options.CustomRoleSpawnChances[role]);
        return opt;
    }

    // Getter
    public override string GetString()
    {
        return Translator.GetString(GetBool() ? TEXT_true : TEXT_false);
    }

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(value % 2 == 0 ? 0 : 1, doSync);
    }
}
