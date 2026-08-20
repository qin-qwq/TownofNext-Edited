using AmongUs.GameOptions;

namespace TONE.Roles.Vanilla;

internal class JudgeTONE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.JudgeTONE;
    private const int Id = 34600;
    public static readonly HashSet<byte> playerIdList = [];
    public override CustomRoles ThisRoleBase => CustomRoles.Judge;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    public static OptionItem JudgeTaskRequirementPercentage;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.JudgeTONE);
        JudgeTaskRequirementPercentage = IntegerOptionItem.Create(Id + 2, GeneralOption.JudgeBase_JudgeTaskRequirementPercentage, new(0, 100, 5), 50, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.JudgeTONE])
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.JudgeTaskRequirementPercentage = JudgeTaskRequirementPercentage.GetInt();
    }
}
