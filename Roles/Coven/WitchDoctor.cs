using AmongUs.GameOptions;
using TONE.Modules;
using TONE.Roles.Core;
using UnityEngine;

namespace TONE.Roles.Coven;

internal class WitchDoctor : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.WitchDoctor;
    private const int Id = 34500;
    public override bool IsDesyncRole => true;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem AbilityLimit;

    private static readonly List<PlayerControl> tempPlayerList = [];

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CovenRoles, Role, 1, zeroOne: false);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityLimit = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(0, 15, 1), 1, TabGroup.CovenRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        tempPlayerList.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityLimit.GetInt());
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AbilityCooldown.GetFloat();
    }

    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!killer || !target) return false;

        // Prevent killing other coven members
        if (target.IsPlayerCovenTeam())
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.WitchDoctor), Translator.GetString("CovenDontKillOtherCoven")));
            return false; // Cancel the kill
        }

        return true; // Allow the kill otherwise
    }

    public override void UnShapeShiftButton(PlayerControl pc)
    {
        if (pc.GetAbilityUseLimit() < 1 && !HasNecronomicon(pc)) return;

        var abilityRangeSorted = pc.Data.Role.GetPlayersInAbilityRangeSorted(RoleBehaviour.GetTempPlayerList());
        var player = abilityRangeSorted.Count <= 0 ? null : abilityRangeSorted[0];

        if (player)
        {
            if (pc.GetAbilityUseLimit() >= 1 && !player.IsPlayerCovenTeam() && !tempPlayerList.Contains(player))
            {
                Ritualist.ConvertRole(pc, player);
                pc.RpcRemoveAbilityUse();
                tempPlayerList.Add(player);
                if (Main.CurrentServerIsVanilla && Options.BypassRateLimitAC.GetBool())
                {
                    Main.Instance.StartCoroutine(Utils.NotifyEveryoneAsync());
                }
                else
                {
                    Utils.NotifyRoles();
                }
                return;
            }

            if (tempPlayerList.Contains(player))
            {
                var role = CustomRoles.CrewmateTONE;
                var roleList = CustomRolesHelper.AllRoles.Where(role => role.IsCoven() && role.IsEnable() && !role.RoleExist(countDead: true)).ToList();
                role = roleList.RandomElement();
                // if every enabled coven role is already in the game then use one of them anyways
                if (role == CustomRoles.Crewmate || role == CustomRoles.CrewmateTONE)
                    role = CustomRolesHelper.AllRoles.Where(role => role.IsCoven() && role.IsEnable()).ToList().RandomElement();

                if (player.Is(CustomRoles.Enchanted))
                {
                    Main.PlayerStates[player.PlayerId].RemoveSubRole(CustomRoles.Enchanted);
                }

                player.GetRoleClass().OnRemove(player.PlayerId);
                player.RpcSetCustomRole(role);
                player.RpcChangeRoleBasis(role);
                player.GetRoleClass().OnAdd(player.PlayerId);

                if (Main.CurrentServerIsVanilla && Options.BypassRateLimitAC.GetBool())
                {
                    Main.Instance.StartCoroutine(Utils.NotifyEveryoneAsync());
                }
                else
                {
                    Utils.NotifyRoles();
                }

                player.ResetKillCooldown();
                player.SetKillCooldown(forceAnime: true);

                tempPlayerList.Remove(player);
                return;
            }

            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.WitchDoctor), Translator.GetString("Jackal_RecruitFailed")));
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(Translator.GetString("Recruit"));
        hud.AbilityButton.SetUsesRemaining((int)playerId.GetAbilityUseLimit());
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Sidekick");
}