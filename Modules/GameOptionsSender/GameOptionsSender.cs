using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using TONE.Modules.Rpc;
using UnityEngine;

namespace TONE.Modules;

public abstract class GameOptionsSender
{
    #region Static
    public readonly static List<GameOptionsSender> AllSenders = new(100) { new NormalGameOptionsSender() };

    public static void SendAllGameOptions()
    {
        AllSenders.RemoveAll(s => !s.AmValid()); // .AmValid() has a virtual property, so it doesn't always return true
        var AllSendersArray = AllSenders.ToArray();
        foreach (GameOptionsSender sender in AllSendersArray)
        {
            if (sender.IsDirty) sender.SendGameOptions();
            sender.IsDirty = false;
        }
    }
    #endregion

    public abstract IGameOptions BasedGameOptions { get; }
    public abstract bool IsDirty { get; protected set; }


    public virtual void SendGameOptions()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var opt = BuildSendableGameOptions();
        var currentGameMode = AprilFoolsMode.IsAprilFoolsModeToggledOn //April fools mode toggled on by host
            ? opt.AprilFoolsOnMode : opt.GameMode; //Change game mode, same as well as in "RpcSyncSettings()"

        // option => byte[]
        MessageWriter writer = MessageWriter.Get(SendOption.None);
        writer.Write(opt.Version);
        writer.StartMessage(0);
        writer.Write((byte)currentGameMode);
        if (opt.TryCast<NormalGameOptionsV10>(out var normalOpt))
            NormalGameOptionsV10.Serialize(writer, normalOpt);
        else if (opt.TryCast<HideNSeekGameOptionsV10>(out var hnsOpt))
            HideNSeekGameOptionsV10.Serialize(writer, hnsOpt);
        else
        {
            writer.Recycle();
            Logger.Error("Option Cast Failed", this.ToString());
        }
        writer.EndMessage();

        // Create into array
        var byteArray = new Il2CppStructArray<byte>(writer.Length - 1);
        // MessageWriter.ToByteArray
        Buffer.BlockCopy(writer.Buffer.CastFast<Array>(), 1, byteArray.CastFast<Array>(), 0, writer.Length - 1);

        SendOptionsArray(byteArray);
        writer.Recycle();
    }
    public virtual void SendOptionsArray(Il2CppStructArray<byte> optionArray)
    {
        try
        {
            byte logicOptionsIndex = 0;
            foreach (var logicComponent in GameManager.Instance.LogicComponents.GetFastEnumerator())
            {
                if (logicComponent.TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, logicOptionsIndex, -1);
                }
                logicOptionsIndex++;
            }
        }
        catch (System.Exception error)
        {
            Logger.Fatal(error.ToString(), "GameOptionsSender.SendOptionsArray");
        }
    }
    protected virtual void SendOptionsArray(Il2CppStructArray<byte> optionArray, byte LogicOptionsIndex, int targetClientId)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var message = new SendOptionsArray(optionArray);

        if (targetClientId < 0)
        {
            RpcUtils.LateBroadcastReliableMessage(message);
        }
        else
        {
            RpcUtils.LateSpecificSendMessage(message, targetClientId);
        }
    }
    public abstract IGameOptions BuildGameOptions();

    protected IGameOptions BuildSendableGameOptions()
    {
        return SanitizeForOfficialServer(BuildGameOptions());
    }

    protected static IGameOptions SanitizeForOfficialServer(IGameOptions opt)
    {
        if (!GameStates.IsVanillaServer || GameStates.IsLocalGame || opt == null || !opt.TryCast(out NormalGameOptionsV10 normalOpt))
            return opt;

        int originalMaxPlayers = normalOpt.MaxPlayers;
        int originalImpostors = normalOpt.NumImpostors;
        int originalKillDistance = normalOpt.KillDistance;
        float originalPlayerSpeed = normalOpt.PlayerSpeedMod;
        bool changed = false;

        if (normalOpt.MaxPlayers > 15)
        {
            normalOpt.SetInt(Int32OptionNames.MaxPlayers, 15);
            changed = true;
        }

        int impostors = Mathf.Clamp(normalOpt.NumImpostors, 1, 3);
        if (impostors != normalOpt.NumImpostors)
        {
            normalOpt.SetInt(Int32OptionNames.NumImpostors, impostors);
            changed = true;
        }

        int killDistance = Mathf.Clamp(normalOpt.KillDistance, 0, 2);
        if (killDistance != normalOpt.KillDistance)
        {
            normalOpt.SetInt(Int32OptionNames.KillDistance, killDistance);
            changed = true;
        }

        float playerSpeed = Mathf.Clamp(normalOpt.PlayerSpeedMod, Main.MinSpeed, 3f);
        if (!Mathf.Approximately(playerSpeed, normalOpt.PlayerSpeedMod))
        {
            normalOpt.SetFloat(FloatOptionNames.PlayerSpeedMod, playerSpeed);
            changed = true;
        }

        if (changed)
        {
            Logger.Warn(
                $"Clamped outgoing official game options: MaxPlayers={originalMaxPlayers}->{normalOpt.MaxPlayers}, NumImpostors={originalImpostors}->{normalOpt.NumImpostors}, KillDistance={originalKillDistance}->{normalOpt.KillDistance}, PlayerSpeedMod={originalPlayerSpeed:0.###}->{normalOpt.PlayerSpeedMod:0.###}",
                nameof(GameOptionsSender));
        }

        return normalOpt.CastFast<IGameOptions>();
    }

    public virtual bool AmValid() => true;
}
