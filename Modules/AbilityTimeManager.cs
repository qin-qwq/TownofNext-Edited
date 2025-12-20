using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Modules;

public static class AbilityTimeManager
{
    public static readonly Dictionary<byte, long> AbilityCooldown = [];
    public static readonly Dictionary<byte, long> AbilityDuration = [];

    public static void Initializate()
    {
        AbilityCooldown.Clear();
        AbilityDuration.Clear();
    }

    public static bool HasAbilityCD(this PlayerControl pc) => AbilityCooldown.ContainsKey(pc.PlayerId);
    public static bool HasAbilityCD(this byte playerId) => AbilityCooldown.ContainsKey(playerId);

    public static long RemainingCD(this PlayerControl pc)
    {
       return AbilityCooldown.GetValueOrDefault(pc.PlayerId, -1) + AbilityDuration.GetValueOrDefault(pc.PlayerId, -1) + pc.DefaultAbilityCD() - Utils.TimeStamp + 1;
    }

    public static void RpcAddAbilityCD(this byte playerId) => RpcAddAbilityCD(playerId.GetPlayer(), true);

    public static void RpcAddAbilityCD(this PlayerControl pc, bool rpc = true, bool includeDuration = false)
    {
        if (!pc.HasAbilityCD() && pc.DefaultAbilityCD() != -10)
        {
            if (pc.AbilityDruation() != -20 && includeDuration)
            {
                AbilityCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                AbilityDuration.Add(pc.PlayerId, pc.AbilityDruation());
                if (rpc) SendRPC(pc);
            }
            else
            {
                AbilityCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                AbilityDuration.Add(pc.PlayerId, 0);
                if (rpc) SendRPC(pc);
            }
        }
    }

    public static void RpcRemoveAbilityCD(this PlayerControl pc, bool rpc = true)
    {
        if (pc.HasAbilityCD())
        {
            AbilityCooldown.Remove(pc.PlayerId);
            AbilityDuration.Remove(pc.PlayerId);

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
            CustomRoles.Veteran => (int)Veteran.VeteranSkillCooldown.GetFloat(),
            CustomRoles.Archaeologist => (int)Archaeologist.VentCooldown.GetFloat(),
            CustomRoles.TimeMaster => (int)TimeMaster.TimeMasterSkillCooldown.GetFloat(),
            CustomRoles.Addict => (int)Addict.VentCooldown.GetFloat(),
            CustomRoles.Mole => (int)Mole.VentCooldown.GetFloat(),
            CustomRoles.Grenadier => (int)Grenadier.GrenadierSkillCooldown.GetFloat(),
            CustomRoles.Lighter => (int)Lighter.LighterSkillCooldown.GetFloat(),
            CustomRoles.Pacifist => (int)Pacifist.PacifistCooldown.GetFloat(),
            CustomRoles.Pyrophoric => (int)Pyrophoric.PyrophoricSkillCooldown.GetFloat(),
            CustomRoles.Altruist => 0,
            CustomRoles.Dreamer => (int)Dreamer.FantasyCooldown.GetFloat(),
            _ => -10
        };

        return cd;
    }

    public static long AbilityDruation(this PlayerControl pc)
    {
        var role = pc.GetCustomRole();

        int cd = role switch
        {
            CustomRoles.Veteran => (int)Veteran.VeteranSkillDuration.GetFloat(),
            CustomRoles.TimeMaster => TimeMaster.TimeMasterUsePetRewind.GetBool() ? -20 : (int)TimeMaster.TimeMasterSkillDuration.GetFloat(),
            CustomRoles.Addict => (int)Addict.ImmortalTimeAfterVent.GetFloat(),
            CustomRoles.Grenadier => (int)Grenadier.GrenadierSkillDuration.GetFloat(),
            CustomRoles.Lighter => (int)Lighter.LighterSkillDuration.GetFloat(),
            CustomRoles.Dreamer => (int)Dreamer.FantasyDuration.GetFloat(),
            _ => -20
        };

        return cd;
    }

    public static void SendRPC(PlayerControl pc)
    {
        if (!pc.IsNonHostModdedClient()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        Utils.SendRPC(CustomRPC.SyncAbilityCD, pc.PlayerId, AbilityCooldown.GetValueOrDefault(pc.PlayerId, -1).ToString(), AbilityDuration.GetValueOrDefault(pc.PlayerId, -1).ToString());
    }
}
