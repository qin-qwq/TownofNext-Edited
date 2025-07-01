using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Transporter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Transporter;
    private const int Id = 7400;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem TransporterTeleportMax;
    private static OptionItem TransporterTeleportCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Transporter);
        TransporterTeleportMax = IntegerOptionItem.Create(7402, "TransporterTeleportMax", new(1, 100, 1), 10, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Times);
        TransporterTeleportCooldown = FloatOptionItem.Create(Id + 10, "TransporterTeleportCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(Id + 11, TabGroup.CrewmateRoles, CustomRoles.Transporter);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(TransporterTeleportMax.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = TransporterTeleportCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive() && player.GetAbilityUseLimit() >= 1)
        {
            Logger.Info($"Transporter: {player.GetNameWithRole().RemoveHtmlTags()} completed the task", "Transporter");

            var rd = IRandom.Instance;
            List<PlayerControl> AllAlivePlayer = Main.AllAlivePlayerControls.Where(x => x.CanBeTeleported()).ToList();

            if (AllAlivePlayer.Count >= 2)
            {
                player.RpcRemoveAbilityUse();
                var target1 = AllAlivePlayer.RandomElement();
                var positionTarget1 = target1.GetCustomPosition();

                AllAlivePlayer.Remove(target1);

                var target2 = AllAlivePlayer.RandomElement();
                var positionTarget2 = target2.GetCustomPosition();

                target1.RpcTeleport(positionTarget2);
                target2.RpcTeleport(positionTarget1);

                AllAlivePlayer.Clear();

                target1.RPCPlayCustomSound("Teleport");
                target2.RPCPlayCustomSound("Teleport");

                target1.Notify(ColorString(GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target2.GetRealName())));
                target2.Notify(ColorString(GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target1.GetRealName())));
            }
            else
            {
                player.Notify(ColorString(GetRoleColor(CustomRoles.Impostor), Translator.GetString("ErrorTeleport")));
            }
        }

        return true;
    }
    public override void OnEnterVent(PlayerControl player, Vent currentVent)
    {
        if (player.IsAlive() && player.GetAbilityUseLimit() >= 1)
        {
            Logger.Info($"Transporter: {player.GetNameWithRole().RemoveHtmlTags()} completed the task", "Transporter");

            var rd = IRandom.Instance;
            List<PlayerControl> AllAlivePlayer = Main.AllAlivePlayerControls.Where(x => x.CanBeTeleported()).ToList();

            if (AllAlivePlayer.Count >= 2)
            {
                player.RpcRemoveAbilityUse();
                var target1 = AllAlivePlayer.RandomElement();
                var positionTarget1 = target1.GetCustomPosition();

                AllAlivePlayer.Remove(target1);

                var target2 = AllAlivePlayer.RandomElement();
                var positionTarget2 = target2.GetCustomPosition();

                target1.RpcTeleport(positionTarget2);
                target2.RpcTeleport(positionTarget1);

                AllAlivePlayer.Clear();

                target1.RPCPlayCustomSound("Teleport");
                target2.RPCPlayCustomSound("Teleport");

                target1.Notify(ColorString(GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target2.GetRealName())));
                target2.Notify(ColorString(GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target1.GetRealName())));
            }
            else
            {
                player.Notify(ColorString(GetRoleColor(CustomRoles.Impostor), Translator.GetString("ErrorTeleport")));
            }
        }
    }
}
