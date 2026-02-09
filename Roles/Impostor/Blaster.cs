using AmongUs.GameOptions;
using TONE.Roles.Crewmate;
using UnityEngine;
using static TONE.Translator;

namespace TONE.Roles.Impostor;

internal class Blaster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Blaster;
    private const int Id = 32300;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Bomb");

    public static OptionItem BomberRadius;
    public static OptionItem BlasterCanKill;
    public static OptionItem BlasterKillCD;
    public static OptionItem BombCooldown;
    public static OptionItem BombDelayTime;
    public static OptionItem ImpostorsSurviveBombs;

    private static readonly Dictionary<byte, List<Vector3>> BombPosition = [];
    private static readonly HashSet<byte> WaitBomb = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Blaster);
        BomberRadius = FloatOptionItem.Create(Id + 2, "BomberRadius", new(0.5f, 100f, 0.5f), 2f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blaster])
            .SetValueFormat(OptionFormat.Multiplier);
        BlasterCanKill = BooleanOptionItem.Create(Id + 3, GeneralOption.CanKill, false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blaster]);
        BlasterKillCD = FloatOptionItem.Create(Id + 4, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(BlasterCanKill)
            .SetValueFormat(OptionFormat.Seconds);
        BombCooldown = FloatOptionItem.Create(Id + 5, "BombCooldown", new(5f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blaster])
            .SetValueFormat(OptionFormat.Seconds);
        BombDelayTime = FloatOptionItem.Create(Id + 6, "BombDelayTime", new(5f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blaster])
            .SetValueFormat(OptionFormat.Seconds);
        ImpostorsSurviveBombs = BooleanOptionItem.Create(Id + 7, "ImpostorsSurviveBombs", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blaster]);
    }

    public override void Init()
    {
        BombPosition.Clear();
        WaitBomb.Clear();
    }
    public override void Add(byte playerId)
    {
        BombPosition[playerId] = [];
    }

    public override bool CanUseKillButton(PlayerControl pc) => BlasterCanKill.GetBool() && pc.IsAlive();
    public override void SetKillCooldown(byte id)
    {
        if (BlasterCanKill.GetBool())
            Main.AllPlayerKillCooldown[id] = BlasterKillCD.GetFloat();
        else
            Main.AllPlayerKillCooldown[id] = 300f;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = BombCooldown.GetFloat();
    }
    public override bool OnCheckVanish(PlayerControl pc)
    {
        if (WaitBomb.Contains(pc.PlayerId)) return false;
        var BombPlace = pc.GetCustomPosition();
        BombPosition[pc.PlayerId].Add(pc.transform.position);
        pc.Notify(string.Format(GetString("BlasterMark"), BombDelayTime.GetFloat()));
        WaitBomb.Add(pc.PlayerId);
        _ = new LateTask(() =>
        {
            if (!GameStates.IsInTask) return;
            foreach (var player in Main.EnumerateAlivePlayerControls())
            {
                foreach (var pos in BombPosition[pc.PlayerId].ToArray())
                {
                    var dis = Utils.GetDistance(pos, player.transform.position);
                    if (dis > BomberRadius.GetFloat()) continue;
                    if (player.GetCustomRole().IsImpostor() && ImpostorsSurviveBombs.GetBool()) continue;
                    if (player.IsTransformedNeutralApocalypse()) continue;
                    if (Medic.IsProtected(player.PlayerId) || player.inVent || player.Is(CustomRoles.Solsticer)) continue;
                    else
                    {
                        player.SetDeathReason(PlayerState.DeathReason.Bombed);
                        player.RpcMurderPlayer(player);
                        player.SetRealKiller(pc);
                    }
                }
            }
            pc.RpcResetAbilityCooldown();
            BombPosition[pc.PlayerId].Clear();
            WaitBomb.Remove(pc.PlayerId);
        }, BombDelayTime.GetFloat(), "Blaster Boom");
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        WaitBomb.Clear();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("BlasterPhantomText"));
    }
}
