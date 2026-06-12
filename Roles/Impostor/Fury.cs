using AmongUs.GameOptions;
using TONE.Modules;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;

namespace TONE.Roles.Impostor;

internal class Fury : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Fury;
    private const int Id = 32000;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem AngryCooldown;
    public static OptionItem AngryDuration;
    private static OptionItem AngryKillCooldown;
    private static OptionItem AngrySpeed;

    private (bool, long) PlayerToAngry = (false, 0);
    private bool Animation = false;
    private static readonly Dictionary<byte, float> tmpSpeed = [];
    private static readonly Dictionary<byte, float> tmpKcd = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fury);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngryCooldown = IntegerOptionItem.Create(Id + 11, "AngryCooldown", new(1, 120, 1), 25, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngryDuration = IntegerOptionItem.Create(Id + 12, "AngryDuration", new(1, 60, 1), 15, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
                .SetValueFormat(OptionFormat.Seconds);
        AngryKillCooldown = FloatOptionItem.Create(Id + 13, "AngryKillCooldown", new(0f, 120f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AngrySpeed = FloatOptionItem.Create(Id + 14, "AngrySpeed", new(0f, 3f, 0.25f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Init()
    {
        tmpSpeed.Clear();
        tmpKcd.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerToAngry = (false, 0);
        Animation = false;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = 1f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckVanish(PlayerControl player)
    {
        if (PlayerToAngry.Item1)
        {
            PlayerToAngry = (false, 0);
            ToCalm(player, true);
            return false;
        }
        if (player.HasAbilityCD()) return false;
        PlayerToAngry = (true, Utils.GetTimeStamp());
        ToAngry(player);
        return false;
    }

    public void ToAngry(PlayerControl player)
    {
        player.RpcAddAbilityCD(includeDuration: true);
        foreach (var target in Main.EnumeratePlayerControls())
        {
            if (!target.IsModded()) target.KillFlash();
            RPC.PlaySoundRPC(Sounds.ImpTransform, target.PlayerId);
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fury), GetString("SeerFuryInRage")));
        }
        tmpSpeed.Remove(player.PlayerId);
        tmpSpeed.Add(player.PlayerId, Main.AllPlayerSpeed[player.PlayerId]);
        tmpKcd.Remove(player.PlayerId);
        tmpKcd.Add(player.PlayerId, Main.AllPlayerKillCooldown[player.PlayerId]);
        Animation = true;
        Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
        player.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Animation = false;
            player.SetKillCooldown(AngryKillCooldown.GetFloat());
            Main.AllPlayerSpeed[player.PlayerId] = AngrySpeed.GetFloat();
            Main.AllPlayerKillCooldown[player.PlayerId] = AngryKillCooldown.GetFloat();
            player.MarkDirtySettings();
        }, 1.5f, "Fury To Angry");
    }

    public void ToCalm(PlayerControl player, bool reset = false)
    {
        if (!player) return;
        if (reset)
        {
            player.RpcRemoveAbilityCD();
            player.RpcAddAbilityCD();
        }
        player.Notify(GetString("FuryInCalm"), 5f);
        Animation = true;
        Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
        player.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Animation = false;
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed[player.PlayerId];
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] - AngryKillCooldown.GetFloat() + tmpKcd[player.PlayerId];
            player.MarkDirtySettings();
        }, 1.5f, "Fury To Calm");
    }

    public override bool CanUseKillButton(PlayerControl pc) => !Animation;

    public override void OnFixedUpdate(PlayerControl pc, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;

        if (PlayerToAngry.Item1)
        {
            if (PlayerToAngry.Item2 + AngryDuration.GetInt() < nowTime)
            {
                ToCalm(pc);
                PlayerToAngry = (false, 0);
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (PlayerToAngry.Item1)
        {
            ToCalm(_Player);
            PlayerToAngry = (false, 0);
        }
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (PlayerToAngry.Item1)
        {
            ToCalm(target);
            PlayerToAngry = (false, 0);
        }
    }

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (PlayerToAngry.Item1)
        {
            killer.SetKillCooldown(AngryKillCooldown.GetFloat());
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("FuryVanishText"));
    }
}
