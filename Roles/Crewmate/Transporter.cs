using Hazel;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Core;
using TONE.Roles.Neutral;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Crewmate;

internal class Transporter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Transporter;
    private const int Id = 7400;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public static OptionItem TransporterConstructCooldown;
    private static OptionItem TransporterTeleportCooldown;

    private readonly Dictionary<Vector2, Portal> TransporterLocation = [];
    private static readonly Dictionary<byte, long> LastTP = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Transporter);
        TransporterConstructCooldown = FloatOptionItem.Create(Id + 10, "TransporterConstructCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Seconds);
        TransporterTeleportCooldown = FloatOptionItem.Create(Id + 11, "TransporterTeleportCooldown", new(2.5f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        TransporterLocation.Clear();
        LastTP.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(2);
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);

        int length = TransporterLocation.Count;
        writer.Write(TransporterLocation.ElementAt(length - 1).Key.x); //x coordinate
        writer.Write(TransporterLocation.ElementAt(length - 1).Key.y); //y coordinate

        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        float xLoc = reader.ReadSingle();
        float yLoc = reader.ReadSingle();
        TransporterLocation.Add(new Vector2(xLoc, yLoc), new(pc.GetCustomPosition(), pc.PlayerId));
    }

    public override void OnPet(PlayerControl player)
    {
        var totalMarked = TransporterLocation.Count;
        if (totalMarked >= 2 || player.GetAbilityUseLimit() <= 0) return;

        player.RpcRemoveAbilityUse();
        TransporterLocation.Add(player.GetCustomPosition(), new(player.GetCustomPosition(), player.PlayerId));
        player.Notify(GetString("TransporterCreated"));

        SendRPC();
        return;
    }

    private void OnFixedUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (player == null) return;
        if (Pelican.IsEaten(player.PlayerId) || !player.IsAlive()) return;

        byte playerId = player.PlayerId;
        if (TransporterLocation.Count != 2) return;

        var now = GetTimeStamp();
        if (!LastTP.ContainsKey(playerId)) LastTP[playerId] = now;
        if (now - LastTP[playerId] <= TransporterTeleportCooldown.GetFloat()) return;

        Vector2 position = player.GetCustomPosition();
        Vector2 TPto;

        if (Vector2.Distance(position, TransporterLocation.ElementAt(0).Key) <= 1f)
        {
            TPto = TransporterLocation.ElementAt(1).Key;
        }
        else if (Vector2.Distance(position, TransporterLocation.ElementAt(1).Key) <= 1f)
        {
            TPto = TransporterLocation.ElementAt(0).Key;
        }
        else return;

        LastTP[playerId] = now;

        player.RpcTeleport(TPto);
        return;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.PetButton.OverrideText(GetString("TransporterButtonText"));        
    }

    public override Sprite GetPetButtonSprite(PlayerControl player) => CustomButton.Get("Teleport");
}
