using System;
using TONE.Roles.Core;
using TONE.Roles.Crewmate;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONE;

// Thanks to https://github.com/TownOfNext/TownOfNext/blob/TONX-unofficial/TONX/Patches/MapBehaviourPatch.cs
// Thanks to https://github.com/AU-Avengers/TOU-Mira/blob/main/TownOfUs/Patches/Misc/MapBehaviourPatch.cs
[HarmonyPatch]
public class MapBehaviourPatch
{
    private static Dictionary<PlayerControl, SpriteRenderer> herePoints = new Dictionary<PlayerControl, SpriteRenderer>();
    public static readonly List<List<Vent>> VentNetworks = [];
    public static readonly Dictionary<int, GameObject> VentIcons = [];
    private static Dictionary<PlayerControl, Vector3> preMeetingPostions = new Dictionary<PlayerControl, Vector3>();
    private static bool ShouldShowRealTime => !PlayerControl.LocalPlayer.IsAlive() || Main.GodMode.Value ||
    Options.CurrentGameMode == CustomGameMode.TagMode && PlayerControl.LocalPlayer.GetRoleClass() is TCrewmate tc && tc.DetectState.Item1;

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPostfix]
    public static void Postfix(MapBehaviour __instance)
    {
        if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetRoleClass() is NiceHacker nh && nh.InAbility.Item1)
        {
            __instance.ShowCountOverlay(true, true, true);
            return;
        }
        InitializeMapVentIcon(__instance);
        InitializeCustomHerePoints(__instance);
        if (Options.CurrentGameMode is CustomGameMode.Standard)
        {
            var player = PlayerControl.LocalPlayer;
            var role = player.GetCustomRole();
            var color = Utils.GetRoleColor(role);
            __instance.ColorControl.SetColor(color);
        }
    }

    public static void InitializeMapVentIcon(MapBehaviour __instance)
    {
        // 删除旧图标
        foreach (var icon in VentIcons.Values.Where(x => x))
        {
            Object.Destroy(icon);
        }
        VentIcons.Clear();
        VentNetworks.Clear();

        // 创建新图标
        if (Main.EnableMapVentIcon.Value)
        {
            var task = PlayerControl.LocalPlayer.myTasks.ToArray()
                .FirstOrDefault(x => x.TaskType == TaskTypes.VentCleaning);
            var xPos = Main.NormalOptions.MapId == 3 ? -1 : 1;

            foreach (var vent in ShipStatus.Instance.AllVents)
            {
                if (vent.name.StartsWith("MinerVent-", StringComparison.Ordinal))
                {
                    continue;
                }

                var location = vent.transform.position / ShipStatus.Instance.MapScale;
                location.x *= xPos;
                location.z = -0.99f;

                if (!VentIcons.TryGetValue(vent.Id, out var icon) || icon == null)
                {
                    icon = Object.Instantiate(__instance.HerePoint.gameObject, __instance.HerePoint.transform.parent);
                    var renderer = icon.GetComponent<SpriteRenderer>();
                    renderer.sprite = Utils.LoadSprite("TONE.Resources.Images.Vent.png", 150f);
                    icon.name = $"Vent {vent.Id} Map Icon";
                    icon.transform.localPosition = location;
                    VentIcons[vent.Id] = icon;
                }

                if (task?.IsComplete == false && task.FindConsoles()[0].ConsoleId == vent.Id)
                {
                    icon.transform.localScale *= 0.6f;
                }
                else
                {
                    icon.transform.localScale = Vector3.one;
                }

                HandleMira();

                var network = GetNetworkFor(vent);
                if (network == null)
                {
                    VentNetworks.Add([.. vent.NearbyVents.Where(x => x != null), vent]);
                }
                else
                {
                    if (network.All(x => x != vent))
                    {
                        network.Add(vent);
                    }
                }
            }

            if (AllVentsRegistered())
            {
                for (var i = 0; i < VentNetworks.Count; i++)
                {
                    var ventNetwork = VentNetworks[i];
                    if (ventNetwork.Count == 0)
                    {
                        continue;
                    }

                    foreach (var vent in ventNetwork)
                    {
                        var go = VentIcons[vent.Id];
                        if (go && go.TryGetComponent<SpriteRenderer>(out var sprite))
                        {
                            sprite.color = Palette.PlayerColors[i];
                        }
                    }
                }
            }
        }
    }

    public static void InitializeCustomHerePoints(MapBehaviour __instance)
    {
        // 删除旧图标
        foreach (var oldHerePoint in herePoints)
        {
            if (oldHerePoint.Value == null) continue;
            Object.Destroy(oldHerePoint.Value.gameObject);
        }
        herePoints.Clear();

        // 创建新图标
        if (Options.CurrentGameMode == CustomGameMode.TagMode)
        {
            foreach (var pc in Main.EnumerateAlivePlayerControls().Where(x => x.Is(CustomRoles.TZombie)))
            {
                if (!pc.AmOwner && pc != null)
                {
                    var herePoint = Object.Instantiate(__instance.HerePoint, __instance.HerePoint.transform.parent);
                    herePoint.gameObject.SetActive(false);
                    herePoints.Add(pc, herePoint);
                }
            }
            return;
        }
        foreach (var pc in Main.EnumerateAlivePlayerControls())
        {
            if (!pc.AmOwner && pc != null)
            {
                var herePoint = Object.Instantiate(__instance.HerePoint, __instance.HerePoint.transform.parent);
                herePoint.gameObject.SetActive(false);
                herePoints.Add(pc, herePoint);
            }
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate)), HarmonyPostfix]
    public static void FixedUpdatePostfix(MapBehaviour __instance)
    {
        if (!ShouldShowRealTime) return;
        foreach (var kvp in herePoints)
        {
            var pc = kvp.Key;
            var herePoint = kvp.Value;
            if (herePoint == null) continue;
            herePoint.gameObject.SetActive(false);
            if (pc == null || __instance.countOverlay.gameObject.active) continue;
            herePoint.gameObject.SetActive(true);

            // Thanks to https://github.com/scp222thj/MalumMenu/blob/main/src/Cheats/MinimapHandler.cs

            // 设置图标颜色
            herePoint.material.SetColor(PlayerMaterial.BodyColor, pc.Data.Color);
            herePoint.material.SetColor(PlayerMaterial.BackColor, pc.Data.ShadowColor);
            herePoint.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);

            // 设置图标位置
            var vector = GameStates.IsMeeting && preMeetingPostions.TryGetValue(pc, out var pmp) ? pmp : pc.transform.position;
            vector /= ShipStatus.Instance.MapScale;
            vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
            vector.z = -1f;
            herePoint.transform.localPosition = vector;
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close)), HarmonyPostfix]
    public static void ClosePostfix(MapBehaviour __instance)
    {
        if (!ShouldShowRealTime) return;
        foreach (var kvp in herePoints)
        {
            var herePoint = kvp.Value;
            if (herePoint == null) continue;
            herePoint.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.SetPreMeetingPosition)), HarmonyPrefix]
    public static void SetPreMeetingPositionPrefix()
    {
        preMeetingPostions.Clear();
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (!pc.AmOwner && pc != null)
            {
                // 记录玩家在开会前的位置
                preMeetingPostions.Add(pc, pc.transform.position);
            }
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    [HarmonyPostfix]
    public static void Postfix()
    {
        VentIcons.Clear();
        VentNetworks.Clear();
    }

    public static List<Vent> GetNetworkFor(Vent vent)
    {
        return VentNetworks.FirstOrDefault(x =>
            x.Any(y => y == vent || y == vent.Left || y == vent.Center || y == vent.Right));
    }

    public static bool AllVentsRegistered()
    {
        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (!vent.isActiveAndEnabled)
            {
                continue;
            }

            if (vent.name.StartsWith("MinerVent-", StringComparison.Ordinal))
            {
                continue;
            }

            var network = GetNetworkFor(vent);
            if (network == null || network.All(x => x != vent))
            {
                return false;
            }
        }

        return true;
    }

    public static void HandleMira()
    {
        if (VentNetworks.Count != 0)
        {
            return;
        }

        if (Main.NormalOptions.MapId == 1)
        {
            var vents = ShipStatus.Instance.AllVents.Where(x => !x.name.Contains("MinerVent"));
            VentNetworks.Add(vents.ToList());
            return;
        }
    }
}
