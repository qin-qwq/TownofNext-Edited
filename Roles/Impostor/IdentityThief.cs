using TONE.Modules;
using TONE.Roles.Core;
using static TONE.Options;

namespace TONE.Roles.Impostor;

internal class IdentityThief : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.IdentityThief;
    private const int Id = 34100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.IdentityThief);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem KillCooldown;

    private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = [];
    public static readonly Dictionary<byte, string> ChangeName = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.IdentityThief);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.IdentityThief])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        OriginalPlayerSkins.Clear();
        ChangeName.Clear();
    }

    public override void Add(byte playerId)
    {
        OriginalPlayerSkins.TryAdd(playerId, Camouflage.PlayerSkins[playerId]);
    }

    public override void Remove(byte playerId)
    {
        OriginalPlayerSkins.Remove(playerId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || Camouflage.IsCamouflage || Camouflager.AbilityActivated || Utils.IsActive(SystemTypes.MushroomMixupSabotage)) return true;
        if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out bool isShapeshifitng) && isShapeshifitng)
        {
            Logger.Info("Target was shapeshifting", "IdentityThief");
            return true;
        }

        string tname = target.GetRealName(isMeeting: true);

        ChangeName.Remove(killer.PlayerId);
        ChangeName.Add(killer.PlayerId, tname);

        var targetSkin = new NetworkedPlayerInfo.PlayerOutfit()
            .Set(tname, target.CurrentOutfit.ColorId, target.CurrentOutfit.HatId, target.CurrentOutfit.SkinId, target.CurrentOutfit.VisorId, target.CurrentOutfit.PetId, target.CurrentOutfit.NamePlateId);

        killer.SetNewOutfit(targetSkin, false, false);
        Camouflage.PlayerSkins[killer.PlayerId] = targetSkin;
        Logger.Info("Changed killer skin", "IdentityThief");

        RPC.SyncAllPlayerNames();
        Utils.NotifyRoles(SpecifyTarget: killer, NoCache: true);
        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        ChangeName.Clear();
        _Player.SetNewOutfit(OriginalPlayerSkins[_Player.PlayerId], false, false);
        Camouflage.PlayerSkins[_Player.PlayerId] = OriginalPlayerSkins[_Player.PlayerId];
    }
}
