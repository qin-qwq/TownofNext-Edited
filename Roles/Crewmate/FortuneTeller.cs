using System;
using TONE.Modules;
using TONE.Roles.Core;
using TONE.Roles.Coven;
using TONE.Roles.Neutral;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Crewmate;

internal class FortuneTeller : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.FortuneTeller;
    private const int Id = 8000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.FortuneTeller);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateInvestigative;
    //==================================================================\\

    private static OptionItem CheckLimitOpt;
    private static OptionItem RoleNumber;
    private static OptionItem ImpostorRoleNumber;
    private static OptionItem CrewmateRoleNumber;
    private static OptionItem NeutralRoleNumber;
    private static OptionItem CovenRoleNumber;
    private static OptionItem AccurateCheckMode;
    private static OptionItem ShowSpecificRole;
    private static OptionItem RandomActiveRoles;

    private readonly HashSet<byte> didVote = [];
    private readonly HashSet<byte> targetList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        RandomActiveRoles = BooleanOptionItem.Create(Id + 11, "RandomActiveRoles", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        RoleNumber = IntegerOptionItem.Create(Id + 12, "CheckRoleNumber", new(1, 30, 1), 6, TabGroup.CrewmateRoles, false).SetParent(RandomActiveRoles)
            .SetValueFormat(OptionFormat.Pieces);
        ImpostorRoleNumber = IntegerOptionItem.Create(Id + 13, "DoomsayerObserveImpostorRoleNumber", new(0, 10, 1), 2,TabGroup.CrewmateRoles, false).SetParent(RandomActiveRoles)
            .SetValueFormat(OptionFormat.Pieces);
        CrewmateRoleNumber = IntegerOptionItem.Create(Id + 14, "DoomsayerObserveCrewmateRoleNumber", new(0, 10, 1), 2, TabGroup.CrewmateRoles, false).SetParent(RandomActiveRoles)
            .SetValueFormat(OptionFormat.Pieces);
        NeutralRoleNumber = IntegerOptionItem.Create(Id + 15, "DoomsayerObserveNeutralRoleNumber", new(0, 10, 1), 2, TabGroup.CrewmateRoles, false).SetParent(RandomActiveRoles)
            .SetValueFormat(OptionFormat.Pieces);
        CovenRoleNumber = IntegerOptionItem.Create(Id + 16, "DoomsayerObserveCovenRoleNumber", new(0, 10, 1), 0, TabGroup.CrewmateRoles, false).SetParent(RandomActiveRoles)
            .SetValueFormat(OptionFormat.Pieces);
        AccurateCheckMode = BooleanOptionItem.Create(Id + 17, "AccurateCheckMode", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        ShowSpecificRole = BooleanOptionItem.Create(Id + 18, "ShowSpecificRole", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        FortuneTellerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 19, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CheckLimitOpt.GetInt());
    }
    private static string GetTargetRoleList(CustomRoles[] roles)
    {
        return roles != null ? string.Join("\n", roles.Select(role => $"    ★ {GetRoleName(role)}")) : "";
    }
    public override void OnMeetingShapeshift(PlayerControl pc, PlayerControl target)
    {
        CheckVote(pc, target);
    }
    public override bool CheckVote(PlayerControl player, PlayerControl target)
    {
        if (player.GetRoleClass().HasVoted) return true;
        if (player == null || target == null) return true;
        if (didVote.Contains(player.PlayerId)) return true;
        didVote.Add(player.PlayerId);

        var abilityUse = player.GetAbilityUseLimit();
        if (abilityUse < 1)
        {
            SendMessage(GetString("FortuneTellerCheckReachLimit"), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTeller").ToUpper()));
            return true;
        }

        if (RandomActiveRoles.GetBool())
        {
            if (targetList.Contains(target.PlayerId))
            {
                SendMessage(GetString("FortuneTellerAlreadyCheckedMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTeller").ToUpper()));
                return true;
            }
        }

        player.RpcRemoveAbilityUse();

        abilityUse = player.GetAbilityUseLimit();
        if (player.PlayerId == target.PlayerId)
        {
            SendMessage(GetString("FortuneTellerCheckSelfMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTeller").ToUpper()));
            return true;
        }

        string msg;

        if ((player.AllTasksCompleted() || AccurateCheckMode.GetBool()) && ShowSpecificRole.GetBool())
        {
            if (target.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target.PlayerId].Count > 0)
            {
                var realTarget = GetPlayerById(VoodooMaster.Dolls[target.PlayerId].Where(x => GetPlayerById(x).IsAlive()).ToList().RandomElement());
                SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), realTarget.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
                msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(realTarget.GetCustomRole().ToString()));
            }
            else if (Illusionist.IsCovIllusioned(target.PlayerId))
            {
                msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCrewmate()).ToList().RandomElement().ToString()));
            }
            else if (Illusionist.IsNonCovIllusioned(target.PlayerId))
            {
                msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCoven()).ToList().RandomElement().ToString()));
            }
            else if (target.Is(CustomRoles.Narc))
                msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(CustomRoles.Sheriff.ToString()));
            else
                msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(target.GetCustomRole().ToString()));


            if (Lich.IsCursed(target))
                msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(CustomRoles.Lich.ToString()));

        }
        else if (RandomActiveRoles.GetBool())
        {
            bool targetIsVM = false;
            if (target.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target.PlayerId].Count > 0)
            {
                target = GetPlayerById(VoodooMaster.Dolls[target.PlayerId].Where(x => GetPlayerById(x).IsAlive()).ToList().RandomElement());
                SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
                targetIsVM = true;
            }
            targetList.Add(target.PlayerId);
            var targetRole = target.GetCustomRole();
            if (Illusionist.IsCovIllusioned(target.PlayerId)) targetRole = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCrewmate() && !role.IsGhostRole()).ToList().RandomElement();
            else if (Illusionist.IsNonCovIllusioned(target.PlayerId)) targetRole = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCoven()).ToList().RandomElement();
            else if (target.Is(CustomRoles.Narc)) targetRole = CustomRoles.Sheriff;

            if (Lich.IsCursed(target)) targetRole = CustomRoles.Lich;

            var rand = IRandom.Instance;
            List<CustomRoles> roleList = [];
            ChooseRole(Custom_Team.Impostor);
            ChooseRole(Custom_Team.Crewmate);
            ChooseRole(Custom_Team.Neutral);
            ChooseRole(Custom_Team.Coven);
            if (roleList.Count - RoleNumber.GetInt() + 1 > 0)
            {
                var removeRole = roleList.Shuffle().Take(roleList.Count - RoleNumber.GetInt() + 1);
                foreach (var role in removeRole)
                {
                    roleList.Remove(role);
                }
            }
            roleList.Add(targetRole);
            for (int i = roleList.Count - 1; i > 0; i--)
            {
                int j = rand.Next(0, i + 1);
                (roleList[j], roleList[i]) = (roleList[i], roleList[j]);
            }
            var text = GetTargetRoleList([.. roleList]);
            var targetName = target.GetRealName();
            if (targetIsVM) targetName = Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().GetRealName();
            msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
            void ChooseRole(Custom_Team team)
            {
                var num = team switch
                {
                    Custom_Team.Coven => CovenRoleNumber.GetInt(),
                    Custom_Team.Crewmate => CrewmateRoleNumber.GetInt(),
                    Custom_Team.Impostor => ImpostorRoleNumber.GetInt(),
                    Custom_Team.Neutral => NeutralRoleNumber.GetInt(),
                    _ => 0,
                };
                if (targetRole.GetCustomRoleTeam() == team) num--;
                if (num <= 0) return;
                var activeRoleList = CustomRolesHelper.AllRoles.Where(role => (role.IsEnable() || role.RoleExist(countDead: true)) && role != targetRole && role.GetCustomRoleTeam() == team && !role.IsGhostRole() && role != CustomRoles.FortuneTeller
                && role != CustomRoles.GM).ToList();
                var count = Math.Min(num, activeRoleList.Count);
                for (var i = 0; i < count; i++)
                {
                    int randomIndex = rand.Next(activeRoleList.Count);
                    roleList.Add(activeRoleList[randomIndex]);
                    activeRoleList.RemoveAt(randomIndex);
                }
            }
        }
        else
        {
            List<CustomRoles[]> completeRoleList = EnumHelper.Achunk<CustomRoles>(chunkSize: 6, shuffle: true, exclude: (x) => !x.IsGhostRole() && !x.IsAdditionRole() && !x.IsVanilla() && x is not CustomRoles.NotAssigned and not CustomRoles.ChiefOfPolice and not CustomRoles.Killer and not CustomRoles.GM and not CustomRoles.Apocalypse and not CustomRoles.Coven);

            var targetRole = target.GetCustomRole();
            string text = string.Empty;

            if (Illusionist.IsCovIllusioned(target.PlayerId)) targetRole = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCrewmate() && !role.IsGhostRole()).ToList().RandomElement();
            else if (Illusionist.IsNonCovIllusioned(target.PlayerId)) targetRole = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCoven()).ToList().RandomElement();
            else if (target.Is(CustomRoles.Narc)) targetRole = CustomRoles.Sheriff;

            if (Lich.IsCursed(target)) targetRole = CustomRoles.Lich;

            text = GetTargetRoleList(completeRoleList.FirstOrDefault(x => x.Contains(targetRole)));

            if (text == string.Empty)
            {
                msg = string.Format(GetString("FortuneTellerCheck.Null"), target.GetRealName());
            }
            else
            {
                msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
            }
        }

        player.GetRoleClass().HasVoted = true;
        SendMessage(GetString("FortuneTellerCheck") + "\n" + msg + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), abilityUse), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTeller").ToUpper()), sendOption: Hazel.SendOption.Reliable);
        SendMessage(GetString("VoteHasReturned"), player.PlayerId, title: ColorString(GetRoleColor(CustomRoles.FortuneTeller), string.Format(GetString("VoteAbilityUsed"), GetString("FortuneTeller"))), noReplay: true);
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        didVote.Clear();
    }
}
