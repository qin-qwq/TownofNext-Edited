using System;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Box : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Box;
    private const int Id = 31600;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    public static OptionItem BecomeBaitDelayNotify;
    public static OptionItem BecomeBaitDelayMin;
    public static OptionItem BecomeBaitDelayMax;
    public static OptionItem BecomeTrapperBlockMoveTime;
    public static OptionItem SpeedMax;
    public static OptionItem SpeedMaxTime;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Box);
        BecomeBaitDelayNotify = BooleanOptionItem.Create(Id + 10, "BaitDelayNotify", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Box]);
        BecomeBaitDelayMin = FloatOptionItem.Create(Id + 11, "BaitDelayMin", new(0f, 5f, 1f), 1f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Box])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeBaitDelayMax = FloatOptionItem.Create(Id + 12, "BaitDelayMax", new(0f, 10f, 1f), 1f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Box])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeTrapperBlockMoveTime = FloatOptionItem.Create(Id + 13, "FreezeTime", new(1f, 180f, 1f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Box])
            .SetValueFormat(OptionFormat.Seconds);
        SpeedMax = FloatOptionItem.Create(Id + 14, "SpeedMax", new(0f, 3f, 0.25f), 2.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Box])
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedMaxTime = FloatOptionItem.Create(Id + 15, "SpeedMaxTime", new(2.5f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Box])
            .SetValueFormat(OptionFormat.Seconds);
    }
        public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;

        var Fg = IRandom.Instance;
        int Box = Fg.Next(1, 8);

        if (Box == 1)
        {
            if (isSuicide)
            {
                if (target.GetRealKiller() != null)
                {
                    if (!target.GetRealKiller().IsAlive()) return;
                    killer = target.GetRealKiller();
                }
            }

            if (killer.PlayerId == target.PlayerId) return;

            if (killer.Is(CustomRoles.KillingMachine)
                || (killer.Is(CustomRoles.Oblivious) && Oblivious.ObliviousBaitImmune.GetBool()))
                return;

            if (!isSuicide || (target.GetRealKiller()?.GetCustomRole() is CustomRoles.Swooper or CustomRoles.Wraith) || !killer.Is(CustomRoles.KillingMachine) || !killer.Is(CustomRoles.Oblivious) || (killer.Is(CustomRoles.Oblivious) && !Oblivious.ObliviousBaitImmune.GetBool()))
            {
                killer.RPCPlayCustomSound("Congrats");
                target.RPCPlayCustomSound("Congrats");

                float delay;
                if (BecomeBaitDelayMax.GetFloat() < BecomeBaitDelayMin.GetFloat())
                {
                    delay = 0f;
                }
                else
                {
                    delay = IRandom.Instance.Next((int)BecomeBaitDelayMin.GetFloat(), (int)BecomeBaitDelayMax.GetFloat() + 1);
                }
                delay = Math.Max(delay, 0.15f);
                if (delay > 0.15f && BecomeBaitDelayNotify.GetBool())
                {
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
                }

                Logger.Info($"{killer.GetNameWithRole()} 击杀了礼盒触发自动报告 => {target.GetNameWithRole()}", "Box");

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Box), GetString("YouKillBox1")));

                _ = new LateTask(() =>
                {
                    if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data);
                }, delay, "Bait Self Report");
            }
        }
        else if (Box == 2)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了礼盒触发暂时无法移动 => {target.GetNameWithRole()}", "Box");

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Box), GetString("YouKillBox2")));
            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
            killer.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                killer.MarkDirtySettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, BecomeTrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
        else if (Box == 3)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了礼盒触发凶手CD变成600 => {target.GetNameWithRole()}", "Box");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Box), GetString("YouKillBox3")));
            Main.AllPlayerKillCooldown[killer.PlayerId] = 600f;
            killer.SyncSettings();
        }
        else if (Box == 4)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了礼盒触发随机复仇 => {target.GetNameWithRole()}", "Box");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Box), GetString("YouKillBox4")));
            {
                var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId && target.RpcCheckAndMurder(x, true)).ToList();
                var pc = pcList[IRandom.Instance.Next(0, pcList.Count)];
                if (!pc.IsTransformedNeutralApocalypse())
                {
                    pc.SetDeathReason(PlayerState.DeathReason.Revenge);
                    pc.RpcMurderPlayer(pc);
                    pc.SetRealKiller(target);
                }
            }
        }
        else if (Box == 5)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了礼盒触发凶手CD变成1 => {target.GetNameWithRole()}", "Box");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Box), GetString("YouKillBox5")));
            Main.AllPlayerKillCooldown[killer.PlayerId] = 1f;
            killer.SyncSettings();
        }
        else if (Box == 6)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了礼盒触发暂时移速加快 => {target.GetNameWithRole()}", "Box");

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Box), GetString("YouKillBox6")));
            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = SpeedMax.GetFloat();
            killer.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - SpeedMax.GetFloat() + tmpSpeed;
                killer.MarkDirtySettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, SpeedMaxTime.GetFloat());
        }
        else if (Box == 7)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了礼盒触发同归于尽 => {target.GetNameWithRole()}", "Box");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Box), GetString("YouKillBox7")));
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
        }
    }
}
