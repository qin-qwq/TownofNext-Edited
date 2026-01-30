using AmongUs.GameOptions;
using System;
using System.Text;
using TMPro;
using TONE.Roles.AddOns.Common;
using TONE.Roles.Core;
using TONE.Roles.Crewmate;
using UnityEngine;
using static TONE.SabotageSystemPatch;
using static TONE.Translator;

namespace TONE;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
class HudManagerStartPatch
{
    public static void Postfix(HudManager __instance)
    {
        __instance.gameObject.AddComponent<OptionShower>().hudManager = __instance;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
class HudManagerUpdatePatch
{
    public static bool ShowDebugText = false;
    public static int LastCallNotifyRolesPerSecond = 0;
    public static int LastSetNameDesyncCount = 0;
    public static int LastFPS = 0;
    public static int NowFrameCount = 0;
    public static float FrameRateTimer = 0.0f;
    public static TextMeshPro LowerInfoText;
    public static GameObject TempLowerInfoText;
    public static void Postfix(HudManager __instance)
    {
        if (!GameStates.IsModHost || __instance == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        //Õúüµè£Òüæ
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if ((!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame)
                && player.CanMove)
            {
                player.Collider.offset = new Vector2(0f, 127f);
            }
        }
        //Õúüµè£Òüæ×ğúÚÖñ
        if (player.Collider.offset.y == 127f)
        {
            if (!Input.GetKey(KeyCode.LeftControl) || (AmongUsClient.Instance.IsGameStarted && GameStates.IsOnlineGame))
            {
                player.Collider.offset = new Vector2(0f, -0.3636f);
            }
        }

        if (!AmongUsClient.Instance.IsGameStarted || GameStates.IsHideNSeek) return;

        Utils.CountAlivePlayers(sendLog: false, checkGameEnd: false);

        if (SetHudActivePatch.IsActive)
        {
            if (Options.CurrentGameMode == CustomGameMode.FFA)
            {
                if (LowerInfoText == null)
                {
                    TempLowerInfoText = new GameObject("CountdownText");
                    TempLowerInfoText.transform.position = new Vector3(0f, -2f, 1f);
                    LowerInfoText = TempLowerInfoText.AddComponent<TextMeshPro>();
                    //LowerInfoText.text = string.Format(GetString("CountdownText"));
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    //LowerInfoText = Object.Instantiate(__instance.KillButton.buttonLabelText);
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.outlineColor = Color.black;
                    LowerInfoText.outlineWidth = 20000000f;
                    LowerInfoText.fontSize = 2f;
                }
                LowerInfoText.text = FFAManager.GetHudText();
            }
            if (player.IsAlive())
            {
                // Set default
                __instance.KillButton?.OverrideText(GetString("KillButtonText"));
                __instance.ReportButton?.OverrideText(GetString("ReportButtonText"));
                __instance.SabotageButton?.OverrideText(GetString("SabotageButtonText"));

                player.GetRoleClass()?.SetAbilityButtonText(__instance, player.PlayerId);

                // Set lower info text for modded players
                if (LowerInfoText == null)
                {
                    LowerInfoText = UnityEngine.Object.Instantiate(__instance.KillButton.cooldownTimerText, __instance.transform, true);
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    LowerInfoText.transform.localPosition = new(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.fontSize = LowerInfoText.fontSizeMax = LowerInfoText.fontSizeMin = 2.8f;
                }
                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.Standard:
                        var roleClass = player.GetRoleClass();
                        LowerInfoText.text = roleClass?.GetLowerText(player, player, isForMeeting: Main.MeetingIsStarted, isForHud: true) ?? string.Empty;

                        LowerInfoText.text += "\n" + Spurt.GetSuffix(player, true, isformeeting: Main.MeetingIsStarted);
                        break;
                }

                LowerInfoText.enabled = LowerInfoText.text != "" && LowerInfoText.text != string.Empty;

                if ((!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay) || GameStates.IsMeeting)
                {
                    LowerInfoText.enabled = false;
                }

                if (player.CanUseKillButton())
                {
                    __instance.KillButton.ToggleVisible(player.IsAlive() && GameStates.IsInTask);
                    player.Data.Role.CanUseKillButton = true;
                }
                else
                {
                    __instance.KillButton.SetDisabled();
                    __instance.KillButton.ToggleVisible(false);
                }

                __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
                player.Data.Role.CanVent = player.CanUseVents();

                // Sometimes sabotage button was visible for non-host modded clients
                if (!AmongUsClient.Instance.AmHost && !player.CanUseSabotage())
                    __instance.SabotageButton.Hide();
            }
            else
            {
                __instance.ReportButton.Hide();
                __instance.ImpostorVentButton.Hide();
                __instance.KillButton.Hide();
                __instance.AbilityButton.Show();
                __instance.AbilityButton.OverrideText(GetString(StringNames.HauntAbilityName));
            }
        }


        if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
        {
            __instance.ToggleMapVisible(new MapOptions()
            {
                Mode = MapOptions.Modes.Sabotage,
                AllowMovementWhileMapOpen = true
            });
            if (player.AmOwner)
            {
                player.MyPhysics.inputHandler.enabled = true;
                ConsoleJoystick.SetMode_Task();
            }
        }

        if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) RepairSender.enabled = false;
        if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            RepairSender.enabled = !RepairSender.enabled;
            RepairSender.Reset();
        }
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
            if (Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
            if (Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
class ToggleHighlightPatch
{
    public static void Postfix(PlayerControl __instance /*, [HarmonyArgument(0)] bool active, [HarmonyArgument(1)] RoleTeamTypes team*/)
    {
        if (GameStates.IsHideNSeek) return;

        var player = PlayerControl.LocalPlayer;
        if (!GameStates.IsInTask) return;

        if (player.CanUseKillButton())
        {
            __instance.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", Utils.GetRoleColor(player.GetCustomRole()));
        }
    }
}
[HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
class SetVentOutlinePatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(1)] ref bool mainTarget)
    {
        if (GameStates.IsHideNSeek) return;

        var player = PlayerControl.LocalPlayer;
        Color color = player.GetRoleColor();
        __instance.myRend.material.SetColor("_OutlineColor", color);
        __instance.myRend.material.SetColor("_AddColor", mainTarget ? color : Color.clear);
    }
}
[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
[HarmonyPatch([typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool)])]
class SetHudActivePatch
{
    public static bool IsActive = false;
    public static void Postfix(HudManager __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(2)] bool isActive)
    {
        // Fix vanilla bug when report button displayed in the lobby
        __instance.ReportButton.ToggleVisible(!GameStates.IsLobby && isActive);

        if (!GameStates.IsModHost || GameStates.IsHideNSeek) return;

        IsActive = isActive;

        if (GameStates.IsLobby || !isActive) return;
        if (player == null) return;

        if (player.Is(CustomRoles.Oblivious) || player.Is(CustomRoles.KillingMachine) || Options.CurrentGameMode != CustomGameMode.Standard)
            __instance.ReportButton.ToggleVisible(false);

        if (player.Is(CustomRoles.Mare) && !Utils.IsActive(SystemTypes.Electrical))
            __instance.KillButton.ToggleVisible(false);

        // Check Toggle visible
        __instance.KillButton.ToggleVisible(player.CanUseKillButton());
        __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
        __instance.SabotageButton.ToggleVisible(player.CanUseSabotage());
    }
}
[HarmonyPatch(typeof(VentButton), nameof(VentButton.DoClick))]
class VentButtonDoClickPatch
{
    public static bool Prefix(VentButton __instance)
    {
        if (GameStates.IsHideNSeek) return true;

        var pc = PlayerControl.LocalPlayer;
        {
            if (pc.inVent || __instance.currentTarget == null || !pc.CanMove || !__instance.isActiveAndEnabled) return true;
            if (!pc.Is(CustomRoles.Chameleon)) return true;
            pc?.MyPhysics?.RpcEnterVent(__instance.currentTarget.Id);
            return false;
        }
    }
}
[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
class MapBehaviourShowPatch
{
    public static void Prefix(MapBehaviour __instance, ref MapOptions opts)
    {
        if (GameStates.IsMeeting || GameStates.IsHideNSeek) return;

        var player = PlayerControl.LocalPlayer;

        if (player.GetCustomRole() == CustomRoles.NiceHacker)
        {
            Logger.Info("Modded Client uses Map", "Hacker");
            NiceHacker.MapHandle(player, __instance, opts);
        }
        if (opts.Mode is MapOptions.Modes.Normal or MapOptions.Modes.Sabotage)
        {
            if (player.CanUseSabotage())
                opts.Mode = MapOptions.Modes.Sabotage;
            else
                opts.Mode = MapOptions.Modes.Normal;
        }
    }
}
[HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
class TaskPanelBehaviourPatch
{
    public static void Postfix(TaskPanelBehaviour __instance)
    {
        if (!GameStates.IsModHost) return;
        if (GameStates.IsLobby) return;

        if (GameStates.IsHideNSeek)
        {
            __instance.open = false;
            return;
        }

        PlayerControl player = PlayerControl.LocalPlayer;

        var taskText = __instance.taskText.text;
        if (taskText == "None") return;

        if (player == null) return;

        // Display Description
        if (!player.GetCustomRole().IsVanilla())
        {
            var RoleWithInfo = $"{player.GetDisplayRoleAndSubName(player, false, false)}:\r\n";
            RoleWithInfo += player.GetRoleInfo();

            var AllText = Utils.ColorString(player.GetRoleColor(), RoleWithInfo);

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard or CustomGameMode.TagMode:

                    var lines = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb = new();
                    foreach (var eachLine in lines)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#FF1919FF>") || line.StartsWith("<color=#FF0000FF>")) && sb.Length < 1 && !line.Contains('(')) continue;
                        sb.Append(line + "\r\n");
                    }

                    if (sb.Length > 1)
                    {
                        var text = sb.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb.ToString().Count(s => (s == '\n')) >= 1)
                            text = $"{Utils.ColorString(new Color32(255, 20, 147, byte.MaxValue), GetString("FakeTask"))}\r\n{text}";
                        AllText += $"\r\n\r\n<size=85%>{text}</size>";
                    }

                    if (MeetingStates.FirstMeeting && Options.CurrentGameMode == CustomGameMode.Standard)
                    {
                        AllText += $"\r\n\r\n</color><size=70%>{GetString("PressF1ShowMainRoleDes")}";
                        /*if (Main.PlayerStates.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var ps) && ps.SubRoles.Count >= 1)
                            AllText += $"\r\n{GetString("PressF2ShowAddRoleDes")}";
                        AllText += $"\r\n{GetString("PressF3ShowRoleSettings")}";
                        if (ps.SubRoles.Count >= 1)
                            AllText += $"\r\n{GetString("PressF4ShowAddOnsSettings")}";*/
                        AllText += "</size>";
                    }
                    break;
                case CustomGameMode.FFA:
                    Dictionary<byte, string> SummaryText2 = [];
                    foreach (var id in Main.PlayerStates.Keys)
                    {
                        string name = Main.AllPlayerNames[id].RemoveHtmlTags().Replace("\r\n", string.Empty);
                        string summary = $"{Utils.GetProgressText(id)}  {Utils.ColorString(Main.PlayerColors[id], name)}";
                        if (Utils.GetProgressText(id).Trim() == string.Empty) continue;
                        SummaryText2[id] = summary;
                    }

                    List<(int, byte)> list2 = [];
                    foreach (var id in Main.PlayerStates.Keys) list2.Add((FFAManager.GetRankOfScore(id), id));
                    list2.Sort();
                    foreach (var id in list2.Where(x => SummaryText2.ContainsKey(x.Item2))) AllText += "\r\n" + SummaryText2[id.Item2];

                    AllText = $"<size=70%>{AllText}</size>";

                    break;
                case CustomGameMode.SpeedRun:
                    var lines2 = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb2 = new();
                    foreach (var eachLine in lines2)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#FF1919FF>") || line.StartsWith("<color=#FF0000FF>")) && sb2.Length < 1 && !line.Contains('(')) continue;
                        sb2.Append(line + "\r\n");
                    }

                    if (sb2.Length > 1)
                    {
                        var text = sb2.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb2.ToString().Count(s => (s == '\n')) >= 1)
                            text = $"{Utils.ColorString(new Color32(255, 20, 147, byte.MaxValue), GetString("FakeTask"))}\r\n{text}";
                        AllText += $"\r\n\r\n<size=85%>{text}</size>";
                    }

                    AllText += $"\r\n\r\n<size=80%>{SpeedRun.GetGameState()}</size>";

                    break;
            }

            __instance.taskText.text = AllText;
        }

        // RepairSender display
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            __instance.taskText.text = RepairSender.GetText();
    }
}

class RepairSender
{
    public static bool enabled = false;
    public static bool TypingAmount = false;

    public static int SystemType;
    public static int amount;

    public static void Input(int num)
    {
        if (!TypingAmount)
        {
            //SystemType is being entered
            SystemType *= 10;
            SystemType += num;
        }
        else
        {
            //Amount being entered
            amount *= 10;
            amount += num;
        }
    }
    public static void InputEnter()
    {
        if (!TypingAmount)
        {
            //SystemType is being entered
            TypingAmount = true;
        }
        else
        {
            //Amount being entered
            Send();
        }
    }
    public static void Send()
    {
        ShipStatus.Instance.RpcUpdateSystem((SystemTypes)SystemType, (byte)amount);
        Reset();
    }
    public static void Reset()
    {
        TypingAmount = false;
        SystemType = 0;
        amount = 0;
    }
    public static string GetText()
    {
        return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
    }
}
[HarmonyPatch(typeof(ActionButton), nameof(ActionButton.SetFillUp))]
internal static class ActionButtonSetFillUpPatch
{
    public static void Postfix(ActionButton __instance, [HarmonyArgument(0)] float timer)
    {
        if (__instance.isCoolingDown && timer is <= 90f and > 0f && !PlayerControl.LocalPlayer.shapeshifting)
        {
            RoleTypes roleType = PlayerControl.LocalPlayer.GetCustomRole().GetRoleTypes();

            bool usingAbility = roleType switch
            {
                RoleTypes.Engineer => PlayerControl.LocalPlayer.inVent,
                RoleTypes.Shapeshifter => Main.CheckShapeshift.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out bool shifted) && shifted,
                _ => false
            };

            Color color = usingAbility ? new Color32(255, 129, 166, 255) : Color.white;
            __instance.cooldownTimerText.text = Utils.ColorString(color, Mathf.CeilToInt(timer).ToString());
            __instance.cooldownTimerText.gameObject.SetActive(true);
        }
    }
}
// Credit: EHR
[HarmonyPatch(typeof(InfectedOverlay), nameof(InfectedOverlay.FixedUpdate))]
internal static class SabotageMapPatch
{
    public static Dictionary<SystemTypes, TextMeshPro> TimerTexts = [];

    public static void Postfix(InfectedOverlay __instance)
    {
        float perc = __instance.sabSystem.PercentCool;
        int total = __instance.sabSystem.initialCooldown ? 10 : 30;
        if (SabotageSystemTypeRepairDamagePatch.isCooldownModificationEnabled) total = (int)SabotageSystemTypeRepairDamagePatch.modifiedCooldownSec;

        int remaining = Math.Clamp(total - (int)Math.Ceiling((1f - perc) * total) + 1, 0, total);

        foreach (MapRoom mr in __instance.rooms)
        {
            if (mr.special == null || mr.special.transform == null) continue;

            SystemTypes room = mr.room;

            if (!TimerTexts.TryGetValue(room, out TextMeshPro timerText))
            {
                TimerTexts[room] = timerText = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, mr.special.transform, true);
                timerText.alignment = TextAlignmentOptions.Center;
                timerText.transform.localPosition = mr.special.transform.localPosition;
                timerText.transform.localPosition = new(0, -0.4f, 0f);
                timerText.overflowMode = TextOverflowModes.Overflow;
                timerText.enableWordWrapping = false;
                timerText.color = Color.white;
                timerText.fontSize = timerText.fontSizeMax = timerText.fontSizeMin = 2.5f;
                timerText.sortingOrder = 100;
                timerText.gameObject.SetActive(true);
            }

            bool isActive = Utils.IsActive(room);
            bool isOtherActive = TimerTexts.Keys.Any(Utils.IsActive);
            bool doorBlock = __instance.DoorsPreventingSabotage;
            timerText.text = $"<b><#ff{(isActive || isOtherActive || doorBlock ? "00" : "ff")}00>{(!isActive && !isOtherActive && !doorBlock ? remaining : isActive && !doorBlock ? "▶" : "⊘")}</color></b>";
            timerText.enabled = remaining > 0 || isActive || isOtherActive || doorBlock;
        }
    }
}

[HarmonyPatch(typeof(MapRoom), nameof(MapRoom.DoorsUpdate))]
internal static class MapRoomDoorsUpdatePatch
{
    public static Dictionary<SystemTypes, TextMeshPro> DoorTimerTexts = [];
    private static readonly int Percent = Shader.PropertyToID("_Percent");

    public static bool Prefix(MapRoom __instance)
    {
        if (!__instance.door || !ShipStatus.Instance) return false;

        SystemTypes room = __instance.room;

        float total;
        float timer;

        ISystemType system = ShipStatus.Instance.Systems[SystemTypes.Doors];
        var doorsSystemType = system.TryCast<DoorsSystemType>();
        var autoDoorsSystemType = system.TryCast<AutoDoorsSystemType>();

        if (doorsSystemType != null)
        {
            if (doorsSystemType.initialCooldown > 0f)
            {
                total = 10f;
                timer = doorsSystemType.initialCooldown;
                goto Skip;
            }

            total = 30f;
            timer = doorsSystemType.timers.TryGetValue(room, out float num) ? num : 0f;
            goto Skip;
        }

        if (autoDoorsSystemType != null)
        {
            if (autoDoorsSystemType.initialCooldown > 0.0)
            {
                total = 10f;
                timer = autoDoorsSystemType.initialCooldown;
                goto Skip;
            }

            foreach (OpenableDoor door in ShipStatus.Instance.AllDoors)
            {
                if (door.Room == room)
                {
                    var autoOpenDoor = door.TryCast<AutoOpenDoor>();

                    if (autoOpenDoor != null)
                    {
                        total = 30f;
                        timer = autoOpenDoor.CooldownTimer;
                        goto Skip;
                    }
                }
            }
        }

        total = 0f;
        timer = 0f;

        Skip:

        __instance.door.material.SetFloat(Percent, __instance.Parent.CanUseDoors ? timer / total : 1f);

        if (!DoorTimerTexts.TryGetValue(room, out TextMeshPro doorTimerText))
        {
            DoorTimerTexts[room] = doorTimerText = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, __instance.door.transform, true);
            doorTimerText.alignment = TextAlignmentOptions.Center;
            doorTimerText.transform.localPosition = __instance.door.transform.localPosition;
            doorTimerText.transform.localPosition = new(0, -0.4f, 0f);
            doorTimerText.overflowMode = TextOverflowModes.Overflow;
            doorTimerText.enableWordWrapping = false;
            doorTimerText.color = Color.white;
            doorTimerText.fontSize = doorTimerText.fontSizeMax = doorTimerText.fontSizeMin = 2.5f;
            doorTimerText.sortingOrder = 100;
            doorTimerText.gameObject.SetActive(true);
        }

        var remaining = (int)Math.Ceiling(timer);
        bool canUseDoors = __instance.Parent.CanUseDoors;
        doorTimerText.text = $"<b><#ff{(!canUseDoors ? "00" : "a5")}00a5>{(canUseDoors ? remaining : "⊘")}</color></b>";
        doorTimerText.enabled = remaining > 0 || !canUseDoors;

        return false;
    }
}
