using TOHE.Modules;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;

namespace TOHE.Roles.Impostor;

internal class Sorcerer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sorcerer;
    private const int Id = 32700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Sorcerer);
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    public static OptionItem KillCooldown;
    private static OptionItem RevivedDeadBodyCannotBeReported;
    //private static OptionItem KillerAlwaysCanGetAlertAndArrow;
    private static OptionItem RevivedAbilityUses;
    private static OptionItem RevivedChangeMode;

    private byte RevivedPlayerId = byte.MaxValue;
    //private readonly static HashSet<byte> AllRevivedPlayerId = [];

    [Obfuscation(Exclude = true)]
    private enum RevivedChangeModeSelectList
    {
        GodfatherCount_Refugee,
        GodfatherCount_Madmate
    }

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Sorcerer);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sorcerer])
            .SetValueFormat(OptionFormat.Seconds);
        RevivedDeadBodyCannotBeReported = BooleanOptionItem.Create(Id + 11, "Altruist_RevivedDeadBodyCannotBeReported_Option", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sorcerer]);
        RevivedAbilityUses = IntegerOptionItem.Create(Id + 12, GeneralOption.SkillLimitTimes, new(1, 20, 1), 2, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sorcerer])
            .SetValueFormat(OptionFormat.Times);
        RevivedChangeMode = StringOptionItem.Create(Id + 13, "RevivedChangeMode", EnumHelper.GetAllNames<RevivedChangeModeSelectList>(), 0, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sorcerer])
            .SetHidden(false);
    }

    public override void Init()
    {
        RevivedPlayerId = byte.MaxValue;
        //AllRevivedPlayerId.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(RevivedAbilityUses.GetInt());
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public void SendRPC()
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(RevivedPlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        RevivedPlayerId = reader.ReadByte();
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (deadBody == null || deadBody.Object == null) return true;
        if (reporter.GetAbilityUseLimit() <= 0) return true;
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;
        if (reporter.Is(CustomRoles.Sorcerer) && _Player?.PlayerId == reporter.PlayerId && reporter.GetAbilityUseLimit() > 0)
        {
            reporter.RpcRemoveAbilityUse();
            var deadPlayer = deadBody.Object;
            var deadPlayerId = deadPlayer.PlayerId;
            var deadBodyObject = deadBody.GetDeadBody();

            RevivedPlayerId = deadPlayerId;
            //AllRevivedPlayerId.Add(deadPlayerId);

            deadPlayer.RpcTeleport(deadBodyObject.transform.position);
            deadPlayer.RpcRevive();
            switch (RevivedChangeMode.GetInt())
            {
                case 0:
                    deadPlayer.RpcGuardAndKill(deadPlayer);
                    deadPlayer.GetRoleClass()?.OnRemove(deadPlayer.PlayerId);
                    deadPlayer.RpcChangeRoleBasis(CustomRoles.Refugee);
                    deadPlayer.RpcSetCustomRole(CustomRoles.Refugee);
                    deadPlayer.GetRoleClass()?.OnAdd(deadPlayer.PlayerId);

                    deadPlayer.ResetKillCooldown();
                    deadPlayer.SetKillCooldown(forceAnime: true);
                    break;
                case 1:
                    deadPlayer.RpcGuardAndKill(deadPlayer);
                    deadPlayer.RpcSetCustomRole(CustomRoles.Madmate);
                    break;
            }

            SendRPC();
            return false;
        }
        if ((RevivedDeadBodyCannotBeReported.GetBool() || reporter.PlayerId == RevivedPlayerId) && deadBody.PlayerId == RevivedPlayerId)
        {
            var countDeadBody = UnityEngine.Object.FindObjectsOfType<DeadBody>().Count(bead => bead.ParentId == deadBody.PlayerId);
            if (countDeadBody >= 2) return true;

            reporter.Notify(Translator.GetString("Altruist_YouTriedReportRevivedDeadBody"));
            SendRPC();
            return false;
        }

        return true;
    }
}
