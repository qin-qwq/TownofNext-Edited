using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Crewmater : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Crewmater;
    private const int Id = 32100;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Crewmater);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Crewmater);
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.GetPlayerTaskState().IsTaskFinished && player.IsAlive())
        {
            CustomRoles role = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCrewmate() && !role.Is(CustomRoles.Crewmater)).ToList().RandomElement();
            player.RpcChangeRoleBasis(role);
            player.GetRoleClass()?.OnRemove(_Player.PlayerId);
            player.RpcSetCustomRole(role);
            player.GetRoleClass()?.OnAdd(_Player.PlayerId);
            player.Notify(string.Format(GetString("RevenantTargeted"), Utils.GetRoleName(role)));
            player.RpcGuardAndKill(player);
            player.ResetKillCooldown();
            player.SetKillCooldown(forceAnime: true);
            player.MarkDirtySettings();
            return true;
        }
        return false;
    }
}
