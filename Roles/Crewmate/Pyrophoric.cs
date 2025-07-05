using AmongUs.GameOptions;
using UnityEngine;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Double;

namespace TOHE.Roles.Crewmate;

internal partial class Pyrophoric : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pyrophoric;
    private const int Id = 31900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pyrophoric);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem PyrophoricSkillCooldown;
    private static OptionItem PyrophoricVision;
    private static OptionItem PyrophoricKilledMeetings;
    private static OptionItem PyrophoricCanKill;
    private static OptionItem PyrophoricCanRevenge;

    private bool InPyrophoric;
    private bool Revenge;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Pyrophoric, 1);
        PyrophoricSkillCooldown = FloatOptionItem.Create(Id + 10, "PyrophoricSkillCooldown", new(5f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyrophoric])
            .SetValueFormat(OptionFormat.Seconds);
        PyrophoricVision = FloatOptionItem.Create(Id + 11, "PyrophoricVision", new(0f, 5f, 0.25f), 2.0f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyrophoric])
            .SetValueFormat(OptionFormat.Multiplier);
        PyrophoricKilledMeetings = IntegerOptionItem.Create(Id + 12, "PyrophoricKilledMeetings", new(1, 10, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyrophoric])
            .SetValueFormat(OptionFormat.Times);
        PyrophoricCanKill = BooleanOptionItem.Create(Id + 13, "PyrophoricCanKill", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyrophoric]);
        PyrophoricCanRevenge = BooleanOptionItem.Create(Id + 14, "PyrophoricCanRevenge", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyrophoric]);
    }
    public override void Init()
    {
        InPyrophoric = false;
        Revenge = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(PyrophoricKilledMeetings.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = PyrophoricSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
        if (InPyrophoric)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, PyrophoricVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, PyrophoricVision.GetFloat());
        }
        else
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!InPyrophoric)
        {
            InPyrophoric = true;
            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.Notify(GetString("AbilityInUse"), 5f);
            pc.MarkDirtySettings();
        }
        else
        {
            pc.Notify(GetString("InPyrophoric"));
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("PyrophoricVentButtonText");
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Ignite");
    public override void AfterMeetingTasks()
    {
        if (Revenge)
        {
            var pcList = Main.AllAlivePlayerControls.Where(pc => !Medic.IsProtected(pc.PlayerId) && !pc.GetCustomRole().IsTNA() && !pc.Is(CustomRoles.Necromancer) && !pc.Is(CustomRoles.PunchingBag) && !pc.Is(CustomRoles.Solsticer) && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)).ToList();
            if (pcList.Any())
            {
                PlayerControl re = pcList.RandomElement();
                re.SetDeathReason(PlayerState.DeathReason.Revenge);
                re.RpcExileV2();
                re.SetRealKiller(_Player);
                Main.PlayerStates[re.PlayerId].SetDead();
                Revenge = false;
            }
        }
        if (InPyrophoric)
        {
            if (_Player.IsAlive())
            {
                _Player.RpcRemoveAbilityUse();
                if (_Player.GetAbilityUseLimit() <= 0)
                {
                    _Player.RpcExileV2();
                    _Player.SetDeathReason(PlayerState.DeathReason.Torched);
                    _Player.SetRealKiller(_Player);
                    Main.PlayerStates[_Player.PlayerId].SetDead();
                }
            }
        }
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (target.IsDisconnected()) return;
        if (!PyrophoricCanKill.GetBool()) return;
        if (!InPyrophoric) return;

        killer.RpcMurderPlayer(killer);
        killer.SetDeathReason(PlayerState.DeathReason.Torched);
        killer.SetRealKiller(target);
    }
    public override void CheckExile(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        if (!PyrophoricCanRevenge.GetBool()) return;
        if (!InPyrophoric) return;

        name += string.Format(Translator.GetString("SomeoneMissing"));
        Revenge = true;
    }
}
