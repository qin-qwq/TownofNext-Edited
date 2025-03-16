/*using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class YF : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.YF;
    private const int Id = 31700;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem YFKillCooldown;
    private static OptionItem EveryOneKnowYF;
    public static OptionItem CanGuess;
    private static OptionItem YFVision;
    private static OptionItem CanLaunchMeeting;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.YF);
        YFKillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(5f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.YF])
            .SetValueFormat(OptionFormat.Seconds);
        EveryOneKnowYF = BooleanOptionItem.Create(Id + 3, "EveryOneKnowYF", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.YF]);
        CanGuess = BooleanOptionItem.Create(Id + 4, GeneralOption.CanGuess, false, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.YF]);
        YFVision = FloatOptionItem.Create(Id + 5, "YFVision", new(0f, 5f, 0.05f), 0.25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.YF])
            .SetValueFormat(OptionFormat.Multiplier);
        CanLaunchMeeting = BooleanOptionItem.Create(Id + 6, GeneralOption.CanUseMeetingButton, false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.YF]);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = YFKillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public static bool VisibleToEveryone(PlayerControl target) => target.Is(CustomRoles.YF) && EveryOneKnowYF.GetBool();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role is CustomRoles.YF)
        {
            pc.ShowInfoMessage(isUI, GetString("GuessYF"));
            return true;
        }
        return false;
    }
    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (!CanGuess.GetBool())
        {
            Logger.Info($"Guess Disabled for this player {guesser.PlayerId}", "GuessManager");
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessDisabled"));
            return true;
        }
        return false;
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(1f);
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, YFVision.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, YFVision.GetFloat());
    }
    public override bool OnCheckStartMeeting(PlayerControl reporter) => CanLaunchMeeting.GetBool();
}*/
