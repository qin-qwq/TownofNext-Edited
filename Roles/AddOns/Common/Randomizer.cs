using System;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Randomizer : IAddon
{
    //===========================SETUP================================\\
    public CustomRoles Role => CustomRoles.Randomizer;
    private const int Id = 7500;
    public AddonTypes Type => AddonTypes.Helpful;
    //==================================================================\\

    public static OptionItem BecomeBaitDelayNotify;
    public static OptionItem BecomeBaitDelayMin;
    public static OptionItem BecomeBaitDelayMax;
    public static OptionItem BecomeTrapperBlockMoveTime;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Randomizer, canSetNum: true, teamSpawnOptions: true);
        BecomeBaitDelayNotify = BooleanOptionItem.Create(Id + 10, "BaitDelayNotify", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer]);
        BecomeBaitDelayMin = FloatOptionItem.Create(Id + 11, "BaitDelayMin", new(0f, 5f, 1f), 0f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeBaitDelayMax = FloatOptionItem.Create(Id + 12, "BaitDelayMax", new(0f, 10f, 1f), 0f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeTrapperBlockMoveTime = FloatOptionItem.Create(Id + 13, "FreezeTime", new(1f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static void RandomizerKilled(PlayerControl killer, PlayerControl target)
    {
        var Fg = IRandom.Instance;
        int Randomizer = Fg.Next(1, 5);

        if (Randomizer == 1)
        {
            if (killer.PlayerId == target.PlayerId)
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

            if (killer.PlayerId != target.PlayerId || (target.GetRealKiller()?.GetCustomRole() is CustomRoles.Swooper) || !killer.Is(CustomRoles.KillingMachine) || !killer.Is(CustomRoles.Oblivious) || (killer.Is(CustomRoles.Oblivious) && !Oblivious.ObliviousBaitImmune.GetBool()))
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

                Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发自动报告 => {target.GetNameWithRole()}", "Randomizer");

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer1")));

                _ = new LateTask(() =>
                {
                    if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data);
                }, delay, "Bait Self Report");
            }
        }
        else if (Randomizer == 2)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发暂时无法移动 => {target.GetNameWithRole()}", "Randomizer");

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer2")));
            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
            killer.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                killer.MarkDirtySettings();
                RPC.PlaySoundRPC(Sounds.TaskComplete, killer.PlayerId);
            }, BecomeTrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
        else if (Randomizer == 3)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发凶手CD变成600 => {target.GetNameWithRole()}", "Randomizer");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer3")));
            killer.SetKillCooldown(600f, forceAnime: true);
        }
        else if (Randomizer == 4)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发随机复仇 => {target.GetNameWithRole()}", "Randomizer");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer4")));
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
    }
}
