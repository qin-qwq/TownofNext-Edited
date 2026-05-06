using static TONE.MeetingHudStartPatch;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Crewmate;

internal class SuperStar : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.SuperStar;
    private const int Id = 7150;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public static OptionItem EveryOneKnowSuperStar; // You should always have this enabled TBHHH 💀💀
    private static OptionItem ImpKnowSuperStarDead;
    private static OptionItem NeutralKnowSuperStarDead;
    private static OptionItem CovenKnowSuperStarDead;

    private static readonly HashSet<byte> SuperStarDead = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SuperStar);
        EveryOneKnowSuperStar = BooleanOptionItem.Create(7152, "EveryOneKnowSuperStar", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);
        ImpKnowSuperStarDead = BooleanOptionItem.Create(Id + 10, "ImpKnowSuperStarDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);
        NeutralKnowSuperStarDead = BooleanOptionItem.Create(Id + 11, "NeutralKnowSuperStarDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);
        CovenKnowSuperStarDead = BooleanOptionItem.Create(Id + 12, "CovenKnowSuperStarDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);
    }

    public override void Init()
    {
        SuperStarDead.Clear();
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
            => seen.Is(CustomRoles.SuperStar) && (seer.PlayerId == seen.PlayerId || EveryOneKnowSuperStar.GetBool()) ? ColorString(GetRoleColor(CustomRoles.SuperStar), "★") : string.Empty;

    public override bool GlobalKillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
    {
        // if SuperStar killed and seer is SuperStar, return true for show kill flash
        if (target.PlayerId == _Player.PlayerId && seer.PlayerId == _Player.PlayerId) return true;

        // Hide kill flash for some team
        if (!ImpKnowSuperStarDead.GetBool() && seer.GetCustomRole().IsImpostor()) return false;
        if (!NeutralKnowSuperStarDead.GetBool() && seer.GetCustomRole().IsNeutral()) return false;
        if (!CovenKnowSuperStarDead.GetBool() && seer.GetCustomRole().IsCoven()) return false;

        seer.Notify(ColorString(GetRoleColor(CustomRoles.SuperStar), GetString("OnSuperStarDead")));
        return true;
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (isSuicide && target.IsDisconnected()) return;

        if (inMeeting)
        {
            //Death Message
            foreach (var pc in Main.EnumeratePlayerControls())
            {
                if (!ImpKnowSuperStarDead.GetBool() && pc.IsPlayerImpostorTeam()) continue;
                if (!NeutralKnowSuperStarDead.GetBool() && pc.IsPlayerNeutralTeam()) continue;
                if (!CovenKnowSuperStarDead.GetBool() && pc.IsPlayerCovenTeam()) continue;

                SendMessage(string.Format(GetString("SuperStarDead"), target.GetRealName()), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.SuperStar), GetString("SuperStar").ToUpper()));
            }
        }
        else
        {
            if (!SuperStarDead.Contains(target.PlayerId))
                SuperStarDead.Add(target.PlayerId);
        }
    }

    public override void OnOthersMeetingHudStart(PlayerControl targets)
    {
        foreach (var csId in SuperStarDead)
        {
            if (!ImpKnowSuperStarDead.GetBool() && targets.IsPlayerImpostorTeam()) continue;
            if (!NeutralKnowSuperStarDead.GetBool() && targets.IsPlayerNeutralTeam()) continue;
            if (!CovenKnowSuperStarDead.GetBool() && targets.IsPlayerCovenTeam()) continue;
            AddMsg(string.Format(GetString("SuperStarDead"), Main.AllPlayerNames[csId]), targets.PlayerId, ColorString(GetRoleColor(CustomRoles.SuperStar), GetString("SuperStar").ToUpper()));
        }
    }
    public override void MeetingHudClear()
    {
        SuperStarDead.Clear();
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        return !Main.EnumerateAlivePlayerControls().Any(x =>
                x.PlayerId != killer.PlayerId &&
                x.PlayerId != target.PlayerId &&
                GetDistance(x.transform.position, target.transform.position) < 2f);
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role is CustomRoles.SuperStar)
        {
            pc.ShowInfoMessage(isUI, GetString("GuessSuperStar"));
            return true;
        }
        return false;
    }
    public static bool VisibleToEveryone(PlayerControl target) => target.Is(CustomRoles.SuperStar) && EveryOneKnowSuperStar.GetBool();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
}
