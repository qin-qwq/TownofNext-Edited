using Hazel;

namespace TOHE.Roles.Crewmate;

internal class Brave : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Brave;
    private const int Id = 31400;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem HeartPlayerThreshold;
    private static OptionItem ShieldPlayerThreshold;
    private static OptionItem SwordPlayerThreshold;
    private static OptionItem KillCooldown;

    private bool HasHeart;
    private bool HasShield;
    private bool HasSword;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Brave);

        HeartPlayerThreshold = IntegerOptionItem.Create(Id + 10, "BraveHeartThreshold", new(1, 15, 1), 12, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        ShieldPlayerThreshold = IntegerOptionItem.Create(Id + 11, "BraveShieldThreshold", new(1, 15, 1), 9, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        SwordPlayerThreshold = IntegerOptionItem.Create(Id + 12, "BraveSwordThreshold", new(1, 15, 1), 6, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Players);
        KillCooldown = FloatOptionItem.Create(Id + 13, "BraveSwordCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Brave])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        HasHeart = HasShield = HasSword = false;
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;

    private static int RemainingPlayers => Main.AllAlivePlayerControls.Length;

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody)
    {
        CheckStageUpgrade();
    }

    private void CheckStageUpgrade()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var remaining = Main.AllAlivePlayerControls.Length;
        var pid = _Player.PlayerId;

        if (!HasHeart && remaining <= HeartPlayerThreshold.GetInt())
        {
            HasHeart = true;
            Main.PlayerStates[_Player.PlayerId].SubRoles.Add(CustomRoles.Seer);
            Utils.SendMessage("解锁能力勇者之心，可以看到场上被淘汰的玩家数（获得灵媒附加职业）", pid,
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Brave), "【 ★ 勇者信息 ★ 】"));
            SendRPC(pid, 1);
        }

        if (!HasShield && remaining <= ShieldPlayerThreshold.GetInt())
        {
            HasShield = true;
            Utils.SendMessage("解锁能力勇者之盾，可以免受袭击类技能的伤害！", pid,
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Brave), "【 ★ 勇者信息 ★ 】"));
            SendRPC(pid, 2);
        }

        if (!HasSword && remaining <= SwordPlayerThreshold.GetInt())
        {
            HasSword = true;
            Utils.SendMessage("解锁能力勇者之剑，可以袭击！", pid,
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Brave), "【 ★ 勇者信息 ★ 】"));
            SendRPC(pid, 3);
        }
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        return !HasShield;
    }
    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = HasSword ? 300f : KillCooldown.GetFloat();
    }

    public override bool CanUseKillButton(PlayerControl pc)
    {
        return HasSword && pc.IsAlive();
    }
    private static void SendRPC(byte playerId, byte stage)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncBraveStage, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(stage);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        byte stage = reader.ReadByte();
        
        if (Main.PlayerStates.TryGetValue(playerId, out var ps) && ps.RoleClass is Brave brave)
        {
            switch (stage)
            {
                case 1:
                    brave.HasHeart = true;
                    ps.SubRoles.Add(CustomRoles.Seer);
                    Utils.SendMessage("解锁能力勇者之心，可以看到场上被淘汰的玩家数（获得灵媒附加职业）", 
                        playerId, 
                        Utils.ColorString(Utils.GetRoleColor(CustomRoles.Brave), "【 ★ 勇者信息 ★ 】"));
                    break;
                case 2:
                    brave.HasShield = true;
                    Utils.SendMessage("解锁能力勇者之盾，可以免受袭击类技能的伤害！", 
                        playerId,
                        Utils.ColorString(Utils.GetRoleColor(CustomRoles.Brave), "【 ★ 勇者信息 ★ 】"));
                    break;
                case 3:
                    brave.HasSword = true;
                    Utils.SendMessage("解锁能力勇者之剑，可以袭击！", 
                        playerId,
                        Utils.ColorString(Utils.GetRoleColor(CustomRoles.Brave), "【 ★ 勇者信息 ★ 】"));
                    break;
            }
        }
    }
}
