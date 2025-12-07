using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Transporter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Transporter;
    private const int Id = 7400;
    public override bool IsDesyncRole => Change;
    public override CustomRoles ThisRoleBase => Change ? CustomRoles.Phantom : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    public static OptionItem TransporterConstructCooldown;
    private static OptionItem TransporterTeleportCooldown;

    private readonly Dictionary<Vector2, Portal> TransporterLocation = [];
    private static readonly Dictionary<byte, long> LastTP = [];
    private bool Change;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Transporter);
        TransporterConstructCooldown = FloatOptionItem.Create(Id + 10, "TransporterConstructCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Seconds);
        TransporterTeleportCooldown = FloatOptionItem.Create(Id + 11, "TransporterTeleportCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(Id + 12, TabGroup.CrewmateRoles, CustomRoles.Transporter);
    }

    public override void Init()
    {
        TransporterLocation.Clear();
        LastTP.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(2);
        Change = false;
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = TransporterConstructCooldown.GetFloat();
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
        if (TransporterLocation.Count >= 2) TransporterLocation.Remove(TransporterLocation.ElementAt(0).Key);
        TransporterLocation.Add(new Vector2(xLoc, yLoc), new(pc.GetCustomPosition(), pc.PlayerId));
    }

    public override void OnPet(PlayerControl pc)
    {
        OnCheckVanish(pc);
    }

    public override bool OnCheckVanish(PlayerControl player)
    {
        var totalMarked = TransporterLocation.Count;
        if (totalMarked >= 2 || player.GetAbilityUseLimit() <= 0) return false;

        player.RpcRemoveAbilityUse();
        TransporterLocation.Add(player.GetCustomPosition(), new(player.GetCustomPosition(), player.PlayerId));
        player.Notify(GetString("TransporterCreated"));

        SendRPC();
        return false;
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

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (UsePets.GetBool()) return true;
        if (completedTaskCount == totalTaskCount)
        {
            player.RpcChangeRoleBasis(CustomRoles.Phantom);
            Change = true;          
        }
        return true;
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        if (UsePets.GetBool())
        {
            hud.PetButton.OverrideText(GetString("TransporterButtonText"));        
        }
        else
        {
            hud.AbilityButton.OverrideText(GetString("TransporterButtonText"));
            hud.AbilityButton.SetUsesRemaining((int)id.GetAbilityUseLimit());
        }
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting)
    {
        if (!UsePets.GetBool()) return CustomButton.Get("Teleport");
        return null;
    }
    public override Sprite GetPetButtonSprite(PlayerControl player)
    {
        if (UsePets.GetBool()) return CustomButton.Get("Teleport");
        return null;
    }
}
