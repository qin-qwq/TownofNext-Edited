using System.Collections.Generic;
using HarmonyLib;
using TOHE.Roles.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TOHE;

// Thanks to https://github.com/TownOfNext/TownOfNext/blob/TONX-unofficial/TONX/Patches/MapBehaviourPatch.cs
[HarmonyPatch]
public class MapBehaviourPatch
{
    private static Dictionary<PlayerControl, SpriteRenderer> herePoints = new Dictionary<PlayerControl, SpriteRenderer>();
    private static Dictionary<PlayerControl, Vector3> preMeetingPostions = new Dictionary<PlayerControl, Vector3>();
    private static bool ShouldShowRealTime => !PlayerControl.LocalPlayer.IsAlive() || Main.GodMode.Value
    || Options.CurrentGameMode == CustomGameMode.TagMode && PlayerControl.LocalPlayer.GetRoleClass() is TCrewmate tc && tc.DetectState.Item1;
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap)), HarmonyPostfix]
    public static void ShowNormalMapPostfix(MapBehaviour __instance)
    {
        InitializeCustomHerePoints(__instance);
        if (Options.CurrentGameMode == CustomGameMode.Standard)
        {
            var player = PlayerControl.LocalPlayer;
            var role = player.GetCustomRole();
            var color = Utils.GetRoleColor(role);
            __instance.ColorControl.SetColor(color);
        }
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap)), HarmonyPostfix]
    public static void ShowSabotageMapPostfix(MapBehaviour __instance)
    {
        InitializeCustomHerePoints(__instance);
        if (Options.CurrentGameMode == CustomGameMode.Standard)
        {
            var player = PlayerControl.LocalPlayer;
            var role = player.GetCustomRole();
            var color = Utils.GetRoleColor(role);
            __instance.ColorControl.SetColor(color);
        }
    }

    public static void InitializeCustomHerePoints(MapBehaviour __instance)
    {
        if (!PlayerControl.LocalPlayer.IsAlive()) __instance.DisableTrackerOverlays();
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
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.TZombie)))
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
        foreach (var pc in Main.AllAlivePlayerControls)
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
}
