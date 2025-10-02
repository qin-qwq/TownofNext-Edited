using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Eraser : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Eraser;
    private const int Id = 24200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Eraser);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem EraseLimitOpt;
    private static OptionItem CanGuessErasedPlayer;

    private static readonly HashSet<byte> PlayerToErase = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Eraser);
        EraseLimitOpt = IntegerOptionItem.Create(Id + 10, "EraseLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Eraser])
            .SetValueFormat(OptionFormat.Times);
        CanGuessErasedPlayer = BooleanOptionItem.Create(Id + 11, "EraserCanGuessErasedPlayer", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Eraser]);
    }
    public override void Init()
    {
        PlayerToErase.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(EraseLimitOpt.GetInt());

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0 && !PlayerToErase.Contains(target.PlayerId) && !target.Is(CustomRoles.Stubborn))
        {
            return killer.CheckDoubleTrigger(target, () =>
            {
                killer.RpcGuardAndKill();
                killer.SetKillCooldown();
                killer.RpcRemoveAbilityUse();
                PlayerToErase.Add(target.PlayerId);
            });
        }
        else return true;
    }
    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (PlayerToErase.Contains(target.PlayerId) && !CanGuessErasedPlayer.GetBool() && !role.IsAdditionRole())
        {
            guesser.ShowInfoMessage(isUI, GetString("EraserTryingGuessErasedPlayer"));
            return true;
        }
        return false;
    }
    public override void NotifyAfterMeeting()
    {
        foreach (var pc in PlayerToErase.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null) continue;
            if (!player.IsAlive()) continue;

            player.RPCPlayCustomSound("Oiiai");
            player.Notify(GetString("LostRoleByEraser"));
        }
    }
    public override void AfterMeetingTasks()
    {
        foreach (var pc in PlayerToErase.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null) continue;
            if (!player.IsAlive()) continue;

            if (player.HasGhostRole())
            {
                Logger.Info($"Canceled {player.GetNameWithRole()} because player have ghost role", "Eraser");
                return;
            }
            CustomRoles EraserRole = player.IsPlayerImpostorTeam() ? CustomRoles.ImpostorTOHE : CustomRoles.CrewmateTOHE;

            player.GetRoleClass()?.OnRemove(player.PlayerId);
            player.RpcChangeRoleBasis(EraserRole);
            player.RpcSetCustomRole(EraserRole);
            Main.DesyncPlayerList.Remove(player.PlayerId);
            player.GetRoleClass()?.OnAdd(player.PlayerId);
            player.ResetKillCooldown();
            player.SetKillCooldown();
            Logger.Info($"{player.GetNameWithRole()} Erase by Eraser", "Eraser");
            PlayerToErase.Clear();
        }
        Utils.MarkEveryoneDirtySettings();
    }
}
