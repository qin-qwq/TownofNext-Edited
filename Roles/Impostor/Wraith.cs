using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TONE.Roles.Core;
using TONE.Roles.Double;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Impostor;

internal class Wraith : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Wraith;
    private const int Id = 18500;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem SuicideCooldown;
    public static OptionItem KillCooldown;
    public static OptionItem PreventSeeRolesDeath;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Wraith);
        SuicideCooldown = FloatOptionItem.Create(Id + 10, "SuicideCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
                .SetValueFormat(OptionFormat.Seconds);
        KillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
                .SetValueFormat(OptionFormat.Seconds);
        PreventSeeRolesDeath = BooleanOptionItem.Create(Id + 12, "PreventSeeRolesDeath", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Wraith]);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = SuicideCooldown.GetFloat();
    }

    public override bool OnCheckVanish(PlayerControl phantom)
    {
        phantom.RpcMurderPlayer(phantom);
        return false;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        if (player.IsAlive()) return;

        ChangeToGhost(player);
    }

    public void ChangeToGhost(PlayerControl player)
    {
        CustomRoles role = CustomRoles.Wraithh;
        player.GetRoleClass()?.OnRemove(player.PlayerId);
        player.RpcSetCustomRole(role);
        player.RpcSetRoleDesync(role.GetRoleTypes(), player.GetClientId());
        player.GetRoleClass()?.OnAdd(player.PlayerId);
        Utils.NotifyRoles(SpecifyTarget: player);
        player.SyncSettings();
        _ = new LateTask(() =>
        {

            player.RpcResetAbilityCooldown();

            if (SendRoleDescriptionFirstMeeting.GetBool())
            {
                var host = PlayerControl.LocalPlayer;
                var name = host.Data.PlayerName;
                var lp = player;
                var sb = new StringBuilder();
                var conf = new StringBuilder();
                var role = player.GetCustomRole();
                var rlHex = Utils.GetRoleColorCode(role);
                sb.Append(Utils.GetRoleTitle(role) + lp.GetRoleInfo(true));
                if (CustomRoleSpawnChances.TryGetValue(CustomRoles.Wraith, out var opt))
                    Utils.ShowChildrenSettings(CustomRoleSpawnChances[CustomRoles.Wraith], ref conf);
                var cleared = conf.ToString();
                conf.Clear().Append($"<size={ChatCommands.Csize}>" + $"<color={rlHex}>{GetString(role.ToString())} {GetString("Settings:")}</color>\n" + cleared + "</size>");

                var writer = CustomRpcSender.Create("SendGhostRoleInfo", SendOption.None);
                writer.StartMessage(player.GetClientId());
                {
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SetName)
                        .Write(host.Data.NetId)
                        .Write(Utils.ColorString(Utils.GetRoleColor(role), GetString("GhostTransformTitle")))
                        .EndRpc();
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SendChat)
                        .Write(sb.ToString())
                        .EndRpc();
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SendChat)
                        .Write(conf.ToString())
                        .EndRpc();
                    writer.StartRpc(host.NetId, (byte)RpcCalls.SetName)
                        .Write(host.Data.NetId)
                        .Write(name)
                        .EndRpc();
                }
                writer.EndMessage();
                writer.SendMessage();

                // Utils.SendMessage(sb.ToString(), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(role), GetString("GhostTransformTitle")));

            }

        }, 0.1f, $"SetGuardianAngel for playerId: {player.PlayerId}");
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Suidce");

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("Suicide"));
    }
}

internal class Wraithh : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Wraithh;
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorGhosts;
    //==================================================================\\

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = Wraith.KillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }

    public static bool PreventKnowRole(PlayerControl seer)
    {
        if (!seer.Is(CustomRoles.Wraithh) || seer.IsAlive()) return false;
        if (Wraith.PreventSeeRolesDeath.GetBool())
            return true;
        return false;
    }

    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18 || !killer.RpcCheckAndMurder(target, true)) return true;

        if (target.GetCustomRole().IsImpostor()) return false;

        killer.RpcMurderPlayer(target);
        killer.RpcResetAbilityCooldown();
        return false;
    }
}
