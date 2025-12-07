using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Modules;

public static class AbilityTimeManager
{
    public static readonly Dictionary<byte, long> AbilityCooldown = [];

    public static void Initializate()
    {
        AbilityCooldown.Clear();
    }

    public static bool HasAbilityCD(this PlayerControl pc) => AbilityCooldown.ContainsKey(pc.PlayerId);
    public static bool HasAbilityCD(this byte playerId) => AbilityCooldown.ContainsKey(playerId);

    public static long RemainingCD(this PlayerControl pc)
    {
       return AbilityCooldown.GetValueOrDefault(pc.PlayerId, -1) + pc.DefaultAbilityCD() - Utils.TimeStamp + 1;
    }

    public static void RpcAddAbilityCD(this byte playerId) => RpcAddAbilityCD(playerId.GetPlayer(), true);

    public static void RpcAddAbilityCD(this PlayerControl pc, bool rpc = true)
    {
        if (!pc.HasAbilityCD() && pc.DefaultAbilityCD() != -10)
        {
            AbilityCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());

            if (rpc) SendRPC(pc);
        }
    }

    public static void RpcRemoveAbilityCD(this PlayerControl pc, bool rpc = true)
    {
        if (pc.HasAbilityCD())
        {
            AbilityCooldown.Remove(pc.PlayerId);

            if (rpc) SendRPC(pc);
        }        
    }

    public static long DefaultAbilityCD(this PlayerControl pc)
    {
        var role = pc.GetCustomRole();

        int cd = role switch
        {
            CustomRoles.Cleaner => (int)Cleaner.CleanCooldown.GetFloat(),
            CustomRoles.Transporter => (int)Transporter.TransporterConstructCooldown.GetFloat(),
            CustomRoles.Veteran => (int)Veteran.VeteranSkillCooldown.GetFloat() + (int)Veteran.VeteranSkillDuration.GetFloat(),
            _ => -10
        };

        return cd;
    }

    public static void SendRPC(PlayerControl pc)
    {
        if (!pc.IsNonHostModdedClient()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        Utils.SendRPC(CustomRPC.SyncAbilityCD, pc.PlayerId, AbilityCooldown.GetValueOrDefault(pc.PlayerId, -1).ToString());
    }
}
