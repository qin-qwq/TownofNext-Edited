using Hazel;
using System;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using UnityEngine;

namespace TOHE;

// Thanks: https://github.com/tukasa0001/TownOfHost/blob/main/Patches/RandomSpawnPatch.cs
class RandomSpawn
{
    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo))]
    [HarmonyPatch([typeof(Vector2), typeof(ushort)])]
    public class SnapToPatch
    {
        public static void Prefix(CustomNetworkTransform __instance, [HarmonyArgument(1)] ushort minSid)
        {
            if (AmongUsClient.Instance.AmHost) return;
            if (__instance.myPlayer.PlayerId == 255) return;
            Logger.Info($"Player Id {__instance.myPlayer.PlayerId} - old sequence {__instance.lastSequenceId} - new sequence {minSid}", "SnapToPatch");
        }
    }
    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
    public class CustomNetworkTransformHandleRpcPatch
    {
        public static bool Prefix(CustomNetworkTransform __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!__instance.isActiveAndEnabled)
            {
                return false;
            }
            if ((RpcCalls)callId == RpcCalls.SnapTo && GameStates.AirshipIsActive)
            {
                var player = __instance.myPlayer;

                // Players haven't spawned yet
                if (!Main.PlayerStates[player.PlayerId].HasSpawned)
                {
                    // Read the coordinates of the SnapTo destination
                    Vector2 position;
                    {
                        var newReader = MessageReader.Get(reader);
                        position = NetHelpers.ReadVector2(newReader);
                        newReader.Recycle();
                    }
                    Logger.Info($"SnapTo: {player.GetRealName()}, ({position.x}, {position.y})", "RandomSpawn");

                    // if the SnapTo destination is a spring location, proceed to the spring process
                    if (IsAirshipVanillaSpawnPosition(position))
                    {
                        AirshipSpawn(player);
                        return !IsRandomSpawn();
                    }
                    else
                    {
                        Logger.Info("Position is not a spring position", "RandomSpawn");
                    }
                }
            }
            return true;
        }

        private static bool IsAirshipVanillaSpawnPosition(Vector2 position)
        {
            // Using the fact that the coordinates of the spring position are in increments of 0.1
            //The comparison is made with an int type in which the coordinates are multiplied by 10
            //As a countermeasure against errors of the float type and the expansion of errors due to the implementation of ReadVector2
            var decupleXFloat = position.x * 10f;
            var decupleYFloat = position.y * 10f;
            var decupleXInt = Mathf.RoundToInt(decupleXFloat);

            // if the difference between the values multiplied by 10 is closer than 0.1,
            //The original coordinates are not in increments of 0.1, so it is not a spring position.
            if (Mathf.Abs(decupleXInt - decupleXFloat) >= 0.09f)
            {
                return false;
            }
            var decupleYInt = Mathf.RoundToInt(decupleYFloat);
            if (Mathf.Abs(decupleYInt - decupleYFloat) >= 0.09f)
            {
                return false;
            }
            var decuplePosition = (decupleXInt, decupleYInt);
            return decupleVanillaSpawnPositions.Contains(decuplePosition);
        }
        /// <summary>For comparison Ten times the vanilla spring position of the airship</summary>
        private static readonly HashSet<(int x, int y)> decupleVanillaSpawnPositions =
            [
                (-7, 85),  // Walkway in front of the dormitory
                (-7, -10),  // Engine
                (-70, -115),  // Kitchen
                (335, -15),  // Cargo
                (200, 105),  // Archive
                (155, 0),  // Main Hall
            ];
    }
    [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
    public static class SpawnInMinigameSpawnAtPatch
    {
        public static bool Prefix(SpawnInMinigame __instance, [HarmonyArgument(0)] SpawnInMinigame.SpawnLocation spawnPoint)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                return true;
            }

            if (__instance.amClosing != Minigame.CloseState.None)
            {
                return false;
            }
            // Cancel vanilla upwelling if random spawn is enabled
            if (IsRandomSpawn())
            {
                // Vanilla process RpcSnapTo replaced with AirshipSpawn
                __instance.gotButton = true;
                PlayerControl.LocalPlayer.SetKinematic(true);
                PlayerControl.LocalPlayer.NetTransform.SetPaused(true);
                AirshipSpawn(PlayerControl.LocalPlayer);
                DestroyableSingleton<HudManager>.Instance.PlayerCam.SnapToTarget();
                __instance.StopAllCoroutines();
                __instance.StartCoroutine(__instance.CoSpawnAt(PlayerControl.LocalPlayer, spawnPoint));
                return false;
            }
            else
            {
                AirshipSpawn(PlayerControl.LocalPlayer);
                return true;
            }
        }
    }
    public static void AirshipSpawn(PlayerControl player)
    {
        Logger.Info($"Spawn: {player.GetRealName()}", "RandomSpawn");
        if (AmongUsClient.Instance.AmHost)
        {
            if (player.GetRoleClass() is Penguin pg)
            {
                pg.OnSpawnAirship();
            }
            if (GameStates.IsNormalGame)
            {
                // Reset cooldown player
                player.RpcResetAbilityCooldown();
            }

            if (IsRandomSpawn())
            {
                new AirshipSpawnMap().RandomTeleport(player);
            }
            else if (player.Is(CustomRoles.GM))
            {
                new AirshipSpawnMap().FirstTeleport(player);
            }
        }
        Main.PlayerStates[player.PlayerId].HasSpawned = true;
    }
    public static bool IsRandomSpawn()
    {
        if (!RandomSpawnMode.GetBool()) return false;

        switch (Main.NormalOptions.MapId)
        {
            case 0 or 3:
                return RandomSpawnSkeld.GetBool();
            case 1:
                return RandomSpawnMira.GetBool();
            case 2:
                return RandomSpawnPolus.GetBool();
            case 4:
                return RandomSpawnAirship.GetBool();
            case 5:
                return RandomSpawnFungle.GetBool();
            default:
                Logger.Error($"MapIdFailed ID:{Main.NormalOptions.MapId}", "IsRandomSpawn");
                return false;
        }
    }
    public static bool CanSpawnInFirstRound() => SpawnInFirstRound.GetBool();

    [Obfuscation(Exclude = true)]
    private enum RandomSpawnOpt
    {
        RandomSpawnMode,
        RandomSpawn_SpawnInFirstRound,
        RandomSpawn_SpawnRandomLocation,
        RandomSpawn_AirshipAdditionalSpawn,
        RandomSpawn_SpawnRandomVents
    }

    private static OptionItem RandomSpawnMode;
    private static OptionItem SpawnInFirstRound;
    private static OptionItem AirshipAdditionalSpawn;
    private static OptionItem SpawnRandomVents;
    // Skeld && dlekS
    public static OptionItem RandomSpawnSkeld;
    public static OptionItem RandomSpawnSkeldCafeteria;
    public static OptionItem RandomSpawnSkeldWeapons;
    public static OptionItem RandomSpawnSkeldLifeSupp;
    public static OptionItem RandomSpawnSkeldNav;
    public static OptionItem RandomSpawnSkeldShields;
    public static OptionItem RandomSpawnSkeldComms;
    public static OptionItem RandomSpawnSkeldStorage;
    public static OptionItem RandomSpawnSkeldAdmin;
    public static OptionItem RandomSpawnSkeldElectrical;
    public static OptionItem RandomSpawnSkeldLowerEngine;
    public static OptionItem RandomSpawnSkeldUpperEngine;
    public static OptionItem RandomSpawnSkeldSecurity;
    public static OptionItem RandomSpawnSkeldReactor;
    public static OptionItem RandomSpawnSkeldMedBay;
    // Mira HQ
    public static OptionItem RandomSpawnMira;
    public static OptionItem RandomSpawnMiraCafeteria;
    public static OptionItem RandomSpawnMiraBalcony;
    public static OptionItem RandomSpawnMiraStorage;
    public static OptionItem RandomSpawnMiraJunction;
    public static OptionItem RandomSpawnMiraComms;
    public static OptionItem RandomSpawnMiraMedBay;
    public static OptionItem RandomSpawnMiraLockerRoom;
    public static OptionItem RandomSpawnMiraDecontamination;
    public static OptionItem RandomSpawnMiraLaboratory;
    public static OptionItem RandomSpawnMiraReactor;
    public static OptionItem RandomSpawnMiraLaunchpad;
    public static OptionItem RandomSpawnMiraAdmin;
    public static OptionItem RandomSpawnMiraOffice;
    public static OptionItem RandomSpawnMiraGreenhouse;
    //Polus
    public static OptionItem RandomSpawnPolus;
    public static OptionItem RandomSpawnPolusOfficeLeft;
    public static OptionItem RandomSpawnPolusOfficeRight;
    public static OptionItem RandomSpawnPolusAdmin;
    public static OptionItem RandomSpawnPolusComms;
    public static OptionItem RandomSpawnPolusWeapons;
    public static OptionItem RandomSpawnPolusBoilerRoom;
    public static OptionItem RandomSpawnPolusLifeSupp;
    public static OptionItem RandomSpawnPolusElectrical;
    public static OptionItem RandomSpawnPolusSecurity;
    public static OptionItem RandomSpawnPolusDropship;
    public static OptionItem RandomSpawnPolusStorage;
    public static OptionItem RandomSpawnPolusRocket;
    public static OptionItem RandomSpawnPolusLaboratory;
    public static OptionItem RandomSpawnPolusToilet;
    public static OptionItem RandomSpawnPolusSpecimens;
    //AirShip
    public static OptionItem RandomSpawnAirship;
    public static OptionItem RandomSpawnAirshipBrig;
    public static OptionItem RandomSpawnAirshipEngine;
    public static OptionItem RandomSpawnAirshipKitchen;
    public static OptionItem RandomSpawnAirshipCargoBay;
    public static OptionItem RandomSpawnAirshipRecords;
    public static OptionItem RandomSpawnAirshipMainHall;
    public static OptionItem RandomSpawnAirshipNapRoom;
    public static OptionItem RandomSpawnAirshipMeetingRoom;
    public static OptionItem RandomSpawnAirshipGapRoom;
    public static OptionItem RandomSpawnAirshipVaultRoom;
    public static OptionItem RandomSpawnAirshipComms;
    public static OptionItem RandomSpawnAirshipCockpit;
    public static OptionItem RandomSpawnAirshipArmory;
    public static OptionItem RandomSpawnAirshipViewingDeck;
    public static OptionItem RandomSpawnAirshipSecurity;
    public static OptionItem RandomSpawnAirshipElectrical;
    public static OptionItem RandomSpawnAirshipMedical;
    public static OptionItem RandomSpawnAirshipToilet;
    public static OptionItem RandomSpawnAirshipShowers;
    //Fungle
    public static OptionItem RandomSpawnFungle;
    public static OptionItem RandomSpawnFungleKitchen;
    public static OptionItem RandomSpawnFungleBeach;
    public static OptionItem RandomSpawnFungleCafeteria;
    public static OptionItem RandomSpawnFungleRecRoom;
    public static OptionItem RandomSpawnFungleBonfire;
    public static OptionItem RandomSpawnFungleDropship;
    public static OptionItem RandomSpawnFungleStorage;
    public static OptionItem RandomSpawnFungleMeetingRoom;
    public static OptionItem RandomSpawnFungleSleepingQuarters;
    public static OptionItem RandomSpawnFungleLaboratory;
    public static OptionItem RandomSpawnFungleGreenhouse;
    public static OptionItem RandomSpawnFungleReactor;
    public static OptionItem RandomSpawnFungleJungleTop;
    public static OptionItem RandomSpawnFungleJungleBottom;
    public static OptionItem RandomSpawnFungleLookout;
    public static OptionItem RandomSpawnFungleMiningPit;
    public static OptionItem RandomSpawnFungleHighlands;
    public static OptionItem RandomSpawnFungleUpperEngine;
    public static OptionItem RandomSpawnFunglePrecipice;
    public static OptionItem RandomSpawnFungleComms;

    private const int Id = 67_227_001;

    public static void SetupCustomOption()
    {
        RandomSpawnMode = BooleanOptionItem.Create(Id + 87, RandomSpawnOpt.RandomSpawnMode, false, TabGroup.ModSettings, false)
            .HideInFFA()
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        SpawnInFirstRound = BooleanOptionItem.Create(Id + 88, RandomSpawnOpt.RandomSpawn_SpawnInFirstRound, true, TabGroup.ModSettings, false)
            .SetParent(RandomSpawnMode);
        AirshipAdditionalSpawn = BooleanOptionItem.Create(Id + 89, RandomSpawnOpt.RandomSpawn_AirshipAdditionalSpawn, true, TabGroup.ModSettings, false)
            .SetParent(RandomSpawnMode);
        SpawnRandomVents = BooleanOptionItem.Create(Id + 90, RandomSpawnOpt.RandomSpawn_SpawnRandomVents, false, TabGroup.ModSettings, false)
            .SetParent(RandomSpawnMode);
        // Skeld && dlekS
        RandomSpawnSkeld = BooleanOptionItem.Create(Id, "RandomSpawnSkeld", false, TabGroup.ModSettings, false).SetParent(RandomSpawnMode);
        RandomSpawnSkeldCafeteria = BooleanOptionItem.Create(Id + 1, StringNames.Cafeteria, true, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldWeapons = BooleanOptionItem.Create(Id + 2, StringNames.Weapons, true, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldShields = BooleanOptionItem.Create(Id + 3, StringNames.Shields, true, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldStorage = BooleanOptionItem.Create(Id + 4, StringNames.Storage, true, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldLowerEngine = BooleanOptionItem.Create(Id + 5, StringNames.LowerEngine, true, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldUpperEngine = BooleanOptionItem.Create(Id + 6, StringNames.UpperEngine, true, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldLifeSupp = BooleanOptionItem.Create(Id + 7, StringNames.LifeSupp, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldNav = BooleanOptionItem.Create(Id + 8, StringNames.Nav, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldComms = BooleanOptionItem.Create(Id + 9, StringNames.Comms, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldAdmin = BooleanOptionItem.Create(Id + 10, StringNames.Admin, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldElectrical = BooleanOptionItem.Create(Id + 11, StringNames.Electrical, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldSecurity = BooleanOptionItem.Create(Id + 12, StringNames.Security, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldReactor = BooleanOptionItem.Create(Id + 13, StringNames.Reactor, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        RandomSpawnSkeldMedBay = BooleanOptionItem.Create(Id + 14, StringNames.MedBay, false, TabGroup.ModSettings, false).SetParent(RandomSpawnSkeld);
        // Mira HQ
        RandomSpawnMira = BooleanOptionItem.Create(Id + 15, "RandomSpawnMira", false, TabGroup.ModSettings, false).SetParent(RandomSpawnMode);
        RandomSpawnMiraCafeteria = BooleanOptionItem.Create(Id + 16, StringNames.Cafeteria, true, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraComms = BooleanOptionItem.Create(Id + 17, StringNames.Comms, true, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraDecontamination = BooleanOptionItem.Create(Id + 18, StringNames.Decontamination, true, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraReactor = BooleanOptionItem.Create(Id + 19, StringNames.Reactor, true, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraLaunchpad = BooleanOptionItem.Create(Id + 20, StringNames.Launchpad, true, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraAdmin = BooleanOptionItem.Create(Id + 21, StringNames.Admin, true, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraBalcony = BooleanOptionItem.Create(Id + 22, StringNames.Balcony, false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraStorage = BooleanOptionItem.Create(Id + 23, StringNames.Storage, false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraJunction = BooleanOptionItem.Create(Id + 24, "Junction", false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraMedBay = BooleanOptionItem.Create(Id + 25, StringNames.MedBay, false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraLockerRoom = BooleanOptionItem.Create(Id + 26, StringNames.LockerRoom, false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraLaboratory = BooleanOptionItem.Create(Id + 27, StringNames.Laboratory, false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraOffice = BooleanOptionItem.Create(Id + 28, StringNames.Office, false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        RandomSpawnMiraGreenhouse = BooleanOptionItem.Create(Id + 29, StringNames.Greenhouse, false, TabGroup.ModSettings, false).SetParent(RandomSpawnMira);
        // Polus
        RandomSpawnPolus = BooleanOptionItem.Create(Id + 30, "RandomSpawnPolus", false, TabGroup.ModSettings, false).SetParent(RandomSpawnMode);
        RandomSpawnPolusOfficeLeft = BooleanOptionItem.Create(Id + 31, "OfficeLeft", true, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusBoilerRoom = BooleanOptionItem.Create(Id + 32, StringNames.BoilerRoom, true, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusSecurity = BooleanOptionItem.Create(Id + 33, StringNames.Security, true, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusDropship = BooleanOptionItem.Create(Id + 34, StringNames.Dropship, true, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusLaboratory = BooleanOptionItem.Create(Id + 35, StringNames.Laboratory, true, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusSpecimens = BooleanOptionItem.Create(Id + 36, StringNames.Specimens, true, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusOfficeRight = BooleanOptionItem.Create(Id + 37, "OfficeRight", false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusAdmin = BooleanOptionItem.Create(Id + 38, StringNames.Admin, false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusComms = BooleanOptionItem.Create(Id + 39, StringNames.Comms, false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusWeapons = BooleanOptionItem.Create(Id + 40, StringNames.Weapons, false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusLifeSupp = BooleanOptionItem.Create(Id + 41, StringNames.LifeSupp, false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusElectrical = BooleanOptionItem.Create(Id + 42, StringNames.Electrical, false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusStorage = BooleanOptionItem.Create(Id + 43, StringNames.Storage, false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusRocket = BooleanOptionItem.Create(Id + 44, "Rocket", false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        RandomSpawnPolusToilet = BooleanOptionItem.Create(Id + 45, "Toilet", false, TabGroup.ModSettings, false).SetParent(RandomSpawnPolus);
        // Airship
        RandomSpawnAirship = BooleanOptionItem.Create(Id + 46, "RandomSpawnAirship", false, TabGroup.ModSettings, false).SetParent(RandomSpawnMode);
        RandomSpawnAirshipBrig = BooleanOptionItem.Create(Id + 47, StringNames.Brig, true, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipEngine = BooleanOptionItem.Create(Id + 48, StringNames.Engine, true, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipKitchen = BooleanOptionItem.Create(Id + 49, StringNames.Kitchen, true, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipCargoBay = BooleanOptionItem.Create(Id + 50, StringNames.CargoBay, true, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipRecords = BooleanOptionItem.Create(Id + 51, StringNames.Records, true, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipMainHall = BooleanOptionItem.Create(Id + 52, StringNames.MainHall, true, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipNapRoom = BooleanOptionItem.Create(Id + 53, "NapRoom", false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipMeetingRoom = BooleanOptionItem.Create(Id + 54, StringNames.MeetingRoom, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipGapRoom = BooleanOptionItem.Create(Id + 55, StringNames.GapRoom, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipVaultRoom = BooleanOptionItem.Create(Id + 56, StringNames.VaultRoom, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipComms = BooleanOptionItem.Create(Id + 57, StringNames.Comms, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipCockpit = BooleanOptionItem.Create(Id + 58, StringNames.Cockpit, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipArmory = BooleanOptionItem.Create(Id + 59, StringNames.Armory, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipViewingDeck = BooleanOptionItem.Create(Id + 60, StringNames.ViewingDeck, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipSecurity = BooleanOptionItem.Create(Id + 61, StringNames.Security, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipElectrical = BooleanOptionItem.Create(Id + 62, StringNames.Electrical, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipMedical = BooleanOptionItem.Create(Id + 63, StringNames.Medical, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipToilet = BooleanOptionItem.Create(Id + 64, "Toilet", false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        RandomSpawnAirshipShowers = BooleanOptionItem.Create(Id + 65, StringNames.Showers, false, TabGroup.ModSettings, false).SetParent(RandomSpawnAirship);
        // Fungle
        RandomSpawnFungle = BooleanOptionItem.Create(Id + 66, "RandomSpawnFungle", false, TabGroup.ModSettings, false).SetParent(RandomSpawnMode);
        RandomSpawnFungleKitchen = BooleanOptionItem.Create(Id + 67, StringNames.Kitchen, true, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleBeach = BooleanOptionItem.Create(Id + 68, StringNames.Beach, true, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleBonfire = BooleanOptionItem.Create(Id + 69, "Bonfire", true, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleGreenhouse = BooleanOptionItem.Create(Id + 70, StringNames.Greenhouse, true, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleComms = BooleanOptionItem.Create(Id + 71, StringNames.Comms, true, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleHighlands = BooleanOptionItem.Create(Id + 72, StringNames.Highlands, true, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleCafeteria = BooleanOptionItem.Create(Id + 73, StringNames.Cafeteria, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleRecRoom = BooleanOptionItem.Create(Id + 74, StringNames.RecRoom, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleDropship = BooleanOptionItem.Create(Id + 75, StringNames.Dropship, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleStorage = BooleanOptionItem.Create(Id + 76, StringNames.Storage, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleMeetingRoom = BooleanOptionItem.Create(Id + 77, StringNames.MeetingRoom, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleSleepingQuarters = BooleanOptionItem.Create(Id + 78, StringNames.SleepingQuarters, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleLaboratory = BooleanOptionItem.Create(Id + 79, StringNames.Laboratory, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleReactor = BooleanOptionItem.Create(Id + 80, StringNames.Reactor, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleJungleTop = BooleanOptionItem.Create(Id + 81, "JungleTop", false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleJungleBottom = BooleanOptionItem.Create(Id + 82, "JungleBottom", false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleLookout = BooleanOptionItem.Create(Id + 83, StringNames.Lookout, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleMiningPit = BooleanOptionItem.Create(Id + 84, StringNames.MiningPit, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFungleUpperEngine = BooleanOptionItem.Create(Id + 85, StringNames.UpperEngine, false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
        RandomSpawnFunglePrecipice = BooleanOptionItem.Create(Id + 86, "Precipice", false, TabGroup.ModSettings, false).SetParent(RandomSpawnFungle);
    }

    public abstract class SpawnMap
    {
        public abstract Dictionary<OptionItem, Vector2> Positions { get; }
        public virtual void RandomTeleport(PlayerControl player)
        {
            Teleport(player, true);
        }
        public virtual void FirstTeleport(PlayerControl player)
        {
            Teleport(player, false);
        }
        public virtual Vector2 GetAllLocation()
        {
            var locations = Positions.ToArray();

            var location = locations.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault();

            return location.Value;
        }

        private void Teleport(PlayerControl player, bool isRadndom)
        {
            int selectRandomSpawn;

            if (isRadndom && Options.CurrentGameMode != CustomGameMode.FFA)
            {
                selectRandomSpawn = 1;

                if (SpawnRandomVents.GetBool())
                {
                    selectRandomSpawn = 2; // 1 or 2
                }
            }
            else selectRandomSpawn = 1;

            if (selectRandomSpawn == 1)
            {
                var location = GetLocation(!isRadndom);
                Logger.Info($"{player.Data.PlayerName}:{location}", "RandomSpawnInLocation");
                player.RpcTeleport(location, isRandomSpawn: true);
            }
            else
            {
                Logger.Info($"{player.Data.PlayerName}", "RandomSpawnInVent");
                player.RpcRandomVentTeleport();
            }
        }
        public Vector2 GetLocation(bool first = false)
        {
            if (Options.CurrentGameMode == CustomGameMode.TagMode)
            {
                var Locations = Positions.ToArray();
                switch (Main.NormalOptions.MapId)
                {
                    case 0 or 3:
                        return Locations.ToArray()[0..3].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                    case 1:
                        return Locations.ToArray()[0..4].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                    case 2:
                        return Locations.ToArray()[0..2].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                    // Skip Airship
                    case 5:
                        return Locations.ToArray()[0..5].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                    default:
                        Logger.Error($"MapIdFailed ID:{Main.NormalOptions.MapId}", "IsRandomSpawn");
                        break;
                }
            }
            var EnableLocations = Positions.Where(o => o.Key.GetBool()).ToArray();
            var locations = EnableLocations.Length != 0 ? EnableLocations : Positions.ToArray();

            if (first) return locations[0].Value;

            var location = locations.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault();

            if (GameStates.AirshipIsActive && !AirshipAdditionalSpawn.GetBool())
                location = locations.ToArray()[0..6].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault();

            return location.Value;
        }
    }

    public class SkeldSpawnMap : SpawnMap
    {
        public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
        {
            [RandomSpawnSkeldSecurity] = new(-13.5f, -5.5f),
            [RandomSpawnSkeldReactor] = new(-20.5f, -5.5f),
            [RandomSpawnSkeldNav] = new(16.5f, -4.8f),
            [RandomSpawnSkeldComms] = new(4.0f, -15.5f),
            [RandomSpawnSkeldCafeteria] = new(-1.0f, 3.0f),
            [RandomSpawnSkeldWeapons] = new(9.3f, 1.0f),
            [RandomSpawnSkeldLifeSupp] = new(6.5f, -3.8f),
            [RandomSpawnSkeldShields] = new(9.3f, -12.3f),
            [RandomSpawnSkeldStorage] = new(-1.5f, -15.5f),
            [RandomSpawnSkeldAdmin] = new(4.5f, -7.9f),
            [RandomSpawnSkeldElectrical] = new(-7.5f, -8.8f),
            [RandomSpawnSkeldLowerEngine] = new(-17.0f, -13.5f),
            [RandomSpawnSkeldUpperEngine] = new(-17.0f, -1.3f),
            [RandomSpawnSkeldMedBay] = new(-9.0f, -4.0f)
        };
    }
    public class MiraHQSpawnMap : SpawnMap
    {
        public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
        {
            [RandomSpawnMiraCafeteria] = new(25.5f, 2.0f),
            [RandomSpawnMiraBalcony] = new(24.0f, -2.0f),
            [RandomSpawnMiraStorage] = new(19.5f, 4.0f),
            [RandomSpawnMiraLaboratory] = new(9.5f, 12.0f),
            [RandomSpawnMiraReactor] = new(2.5f, 10.5f),
            [RandomSpawnMiraJunction] = new(17.8f, 11.5f),
            [RandomSpawnMiraComms] = new(15.3f, 3.8f),
            [RandomSpawnMiraMedBay] = new(15.5f, -0.5f),
            [RandomSpawnMiraLockerRoom] = new(9.0f, 1.0f),
            [RandomSpawnMiraDecontamination] = new(6.1f, 6.0f),
            [RandomSpawnMiraLaunchpad] = new(-4.5f, 2.0f),
            [RandomSpawnMiraAdmin] = new(21.0f, 17.5f),
            [RandomSpawnMiraOffice] = new(15.0f, 19.0f),
            [RandomSpawnMiraGreenhouse] = new(17.8f, 23.0f)
        };
    }
    public class PolusSpawnMap : SpawnMap
    {
        public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
        {
            [RandomSpawnPolusSpecimens] = new(36.5f, -22.0f),
            [RandomSpawnPolusBoilerRoom] = new(2.3f, -24.0f),
            [RandomSpawnPolusLifeSupp] = new(2.0f, -17.5f),
            [RandomSpawnPolusOfficeLeft] = new(19.5f, -18.0f),
            [RandomSpawnPolusOfficeRight] = new(26.0f, -17.0f),
            [RandomSpawnPolusAdmin] = new(24.0f, -22.5f),
            [RandomSpawnPolusComms] = new(12.5f, -16.0f),
            [RandomSpawnPolusWeapons] = new(12.0f, -23.5f),
            [RandomSpawnPolusElectrical] = new(9.5f, -12.5f),
            [RandomSpawnPolusSecurity] = new(3.0f, -12.0f),
            [RandomSpawnPolusDropship] = new(16.7f, -3.0f),
            [RandomSpawnPolusStorage] = new(20.5f, -12.0f),
            [RandomSpawnPolusRocket] = new(26.7f, -8.5f),
            [RandomSpawnPolusLaboratory] = new(36.5f, -7.5f),
            [RandomSpawnPolusToilet] = new(34.0f, -10.0f)
        };
    }

    public class DleksSpawnMap : SpawnMap
    {
        public static Dictionary<OptionItem, Vector2> TempPositions = new SkeldSpawnMap().Positions
            .ToDictionary(e => e.Key, e => new Vector2(-e.Value.x, e.Value.y));

        public override Dictionary<OptionItem, Vector2> Positions { get; } = TempPositions;
    }
    public class AirshipSpawnMap : SpawnMap
    {
        public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
        {
            [RandomSpawnAirshipBrig] = new(-0.7f, 8.5f),
            [RandomSpawnAirshipEngine] = new(-0.7f, -1.0f),
            [RandomSpawnAirshipKitchen] = new(-7.0f, -11.5f),
            [RandomSpawnAirshipCargoBay] = new(33.5f, -1.5f),
            [RandomSpawnAirshipRecords] = new(20.0f, 10.5f),
            [RandomSpawnAirshipMainHall] = new(15.5f, 0.0f),
            [RandomSpawnAirshipNapRoom] = new(6.3f, 2.5f),
            [RandomSpawnAirshipMeetingRoom] = new(17.1f, 14.9f),
            [RandomSpawnAirshipGapRoom] = new(12.0f, 8.5f),
            [RandomSpawnAirshipVaultRoom] = new(-8.9f, 12.2f),
            [RandomSpawnAirshipComms] = new(-13.3f, 1.3f),
            [RandomSpawnAirshipCockpit] = new(-23.5f, -1.6f),
            [RandomSpawnAirshipArmory] = new(-10.3f, -5.9f),
            [RandomSpawnAirshipViewingDeck] = new(-13.7f, -12.6f),
            [RandomSpawnAirshipSecurity] = new(5.8f, -10.8f),
            [RandomSpawnAirshipElectrical] = new(16.3f, -8.8f),
            [RandomSpawnAirshipMedical] = new(29.0f, -6.2f),
            [RandomSpawnAirshipToilet] = new(30.9f, 6.8f),
            [RandomSpawnAirshipShowers] = new(21.2f, -0.8f)
        };
    }
    public class FungleSpawnMap : SpawnMap
    {
        public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
        {
            [RandomSpawnFungleReactor] = new(21.8f, -7.2f),
            [RandomSpawnFungleGreenhouse] = new(9.2f, -11.8f),
            [RandomSpawnFungleLookout] = new(6.4f, 3.1f),
            [RandomSpawnFungleMiningPit] = new(12.5f, 9.6f),
            [RandomSpawnFungleHighlands] = new(15.5f, 3.9f),    //展望台右の高地
            [RandomSpawnFungleUpperEngine] = new(21.9f, 3.2f),
            [RandomSpawnFungleKitchen] = new(-17.8f, -7.3f),
            [RandomSpawnFungleBeach] = new(-21.3f, 3.0f),   //海岸
            [RandomSpawnFungleCafeteria] = new(-16.9f, 5.5f),
            [RandomSpawnFungleRecRoom] = new(-17.7f, 0.0f),
            [RandomSpawnFungleBonfire] = new(-9.7f, 2.7f),  //焚き火
            [RandomSpawnFungleDropship] = new(-7.6f, 10.4f),
            [RandomSpawnFungleStorage] = new(2.3f, 4.3f),
            [RandomSpawnFungleMeetingRoom] = new(-4.2f, -2.2f),
            [RandomSpawnFungleSleepingQuarters] = new(1.7f, -1.4f),  //宿舎
            [RandomSpawnFungleLaboratory] = new(-4.2f, -7.9f),
            [RandomSpawnFungleJungleTop] = new(4.2f, -5.3f),
            [RandomSpawnFungleJungleBottom] = new(15.9f, -14.8f),
            [RandomSpawnFunglePrecipice] = new(19.8f, 7.3f),   //通信室下の崖
            [RandomSpawnFungleComms] = new(20.9f, 13.4f),
        };
    }
}
