using AmongUs.GameOptions;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using Hazel;
using TOHE.Roles.Core;

namespace TOHE.Patches;
/*
 * HUGE THANKS TO
 * ImaMapleTree / 단풍잎 / Tealeaf
 * FOR THE CODE
 */

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
internal static class LocalPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = [];

    public static bool Prefix(PlayerControl __instance)
    {
        if (!Options.UsePets.GetBool()) return true;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return true;
        if (GameStates.IsLobby || !__instance.IsAlive()) return true;
        
        if (__instance.petting) return true;
        __instance.petting = true;

        if (!LastProcess.ContainsKey(__instance.PlayerId)) LastProcess.TryAdd(__instance.PlayerId, Utils.TimeStamp - 2);
        if (LastProcess[__instance.PlayerId] + 1 >= Utils.TimeStamp) return true;

        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, (byte)RpcCalls.Pet);

        LastProcess[__instance.PlayerId] = Utils.TimeStamp;
        return !Options.CancelPetAnimation.GetBool() || !__instance.PetActivatedAbility();
    }

    public static void Postfix(PlayerControl __instance)
    {
        if (!Options.UsePets.GetBool()) return;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return;

        __instance.petting = false;
        
        if (!Options.CancelPetAnimation.GetBool()) _ = new LateTask(() => __instance.MyPhysics?.CancelPet(), 0.4f);
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal static class ExternalRpcPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = [];

    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callID)
    {
        if (GameStates.IsLobby || !Options.UsePets.GetBool() || !AmongUsClient.Instance.AmHost || (RpcCalls)callID != RpcCalls.Pet) return;

        PlayerControl pc = __instance.myPlayer;
        PlayerPhysics physics = __instance;

        if (pc == null || !pc.IsAlive()) return;

        if (!pc.inVent
            && !pc.inMovingPlat
            && !pc.walkingToVent
            && !pc.onLadder
            && !physics.Animations.IsPlayingEnterVentAnimation()
            && !physics.Animations.IsPlayingClimbAnimation()
            && !physics.Animations.IsPlayingAnyLadderAnimation()
            && !Pelican.IsEaten(pc.PlayerId)
            && GameStates.IsInTask)
        {
            CancelPet();
            _ = new LateTask(CancelPet, 0.4f);

            void CancelPet()
            {
                physics.CancelPet();
                MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.CancelPet, SendOption.None);
                AmongUsClient.Instance.FinishRpcImmediately(w);
            }
        }

        if (!LastProcess.ContainsKey(pc.PlayerId)) LastProcess.TryAdd(pc.PlayerId, Utils.TimeStamp - 2);
        if (LastProcess[pc.PlayerId] + 1 >= Utils.TimeStamp) return;

        LastProcess[pc.PlayerId] = Utils.TimeStamp;

        Logger.Info($"Player {pc.GetNameWithRole().RemoveHtmlTags()} petted their pet", "PetActionTrigger");

        _ = new LateTask(() => OnPetUse(pc), 0.2f, $"OnPetUse: {pc.GetNameWithRole().RemoveHtmlTags()}", false);
    }

    public static void OnPetUse(PlayerControl pc)
    {
        if (pc == null ||
            pc.inVent ||
            pc.inMovingPlat ||
            pc.onLadder ||
            pc.walkingToVent ||
            pc.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
            pc.MyPhysics.Animations.IsPlayingClimbAnimation() ||
            pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
            Pelican.IsEaten(pc.PlayerId) ||
            !AmongUsClient.Instance.AmHost ||
            GameStates.IsLobby ||
            AntiBlackout.SkipTasks ||
            !pc.PetActivatedAbility()
            )
            return;

        if (pc.HasAbilityCD())
        {
            pc.Notify(Translator.GetString("AbilityOnCooldown"));

            return;
        }

        var role = pc.GetRoleClass();

        role?.OnPet(pc);

        if (pc.HasAbilityCD()) return;

        pc.RpcAddAbilityCD(includeDuration: true);
    }
}
