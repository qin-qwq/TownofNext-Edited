using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Traitor : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 18200;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanUsesSabotage;

    public static void SetupCustomOption()
    {
        //Traitorは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Traitor, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 15, "CanUseSabotage", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
    }
    public override void Init()
    {
        playerIdList = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return !(target == killer || target.Is(CustomRoleTypes.Impostor));
    }
    
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
        => seer.Is(CustomRoles.Traitor) && target.Is(CustomRoleTypes.Impostor);
}
