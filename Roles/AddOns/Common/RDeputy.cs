using System;
using System.Text.RegularExpressions;
using AmongUs.InnerNet.GameDataMessages;
using TONE.Modules;
using TONE.Modules.Rpc;
using TONE.Roles.Coven;
using TONE.Roles.Crewmate;
using UnityEngine;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.AddOns.Common;

public class RDeputy : IAddon
{
    public CustomRoles Role => CustomRoles.RDeputy;
    private const int Id = 34900;
    public AddonTypes Type => AddonTypes.Experimental;

    public static OptionItem ImpostorCanBecomeDeputy;
    public static OptionItem CrewmateCanBecomeDeputy;
    public static OptionItem NeutralCanBecomeDeputy;
    public static OptionItem CovenCanBecomeDeputy;
    enum OptionName
    {
        RoundUp_ImpostorCanBecomeDeputy,
        RoundUp_CrewmateCanBecomeDeputy,
        RoundUp_NeutralCanBecomeDeputy,
        RoundUp_CovenCanBecomeDeputy
    }

    public static byte Deputy;
    public static NetworkedPlayerInfo.PlayerOutfit PlayerOutfit = null;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, Role, tab: TabGroup.Addons);
        ImpostorCanBecomeDeputy = BooleanOptionItem.Create(Role, Id + 20, OptionName.RoundUp_ImpostorCanBecomeDeputy, true, false)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue));
        CrewmateCanBecomeDeputy = BooleanOptionItem.Create(Role, Id + 21, OptionName.RoundUp_CrewmateCanBecomeDeputy, true, false)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue));
        NeutralCanBecomeDeputy = BooleanOptionItem.Create(Role, Id + 22, OptionName.RoundUp_NeutralCanBecomeDeputy, true, false)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue));
        CovenCanBecomeDeputy = BooleanOptionItem.Create(Role, Id + 23, OptionName.RoundUp_CovenCanBecomeDeputy, true, false)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue));
    }
    public void Init()
    {
        Deputy = byte.MaxValue;
        PlayerOutfit = null;
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    public static void OnReportDeadBody()
    {
        if (IRandom.Instance.Next(1, 101) <= (Options.CustomAdtRoleSpawnRate.TryGetValue(CustomRoles.RDeputy, out var sc) ? sc.GetFloat() : 0))
        {
            var pcList = Main.EnumerateAlivePlayerControls().Where(x => (x.IsPlayerCrewmateTeam() && CrewmateCanBecomeDeputy.GetBool()) || (x.IsPlayerImpostorTeam() && ImpostorCanBecomeDeputy.GetBool()) ||
            (x.IsPlayerNeutralTeam() && NeutralCanBecomeDeputy.GetBool() && !x.Is(CustomRoles.Jester) && !x.Is(CustomRoles.Executioner) && !x.Is(CustomRoles.Pixie) && !x.Is(CustomRoles.Solsticer)) ||
            (x.IsPlayerCovenTeam() && CovenCanBecomeDeputy.GetBool())).ToList();

            if (pcList.Any())
            {
                var player = pcList.RandomElement();
                Deputy = player.PlayerId;
                PlayerOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                .Set(player.GetRealName(), player.CurrentOutfit.ColorId, player.CurrentOutfit.HatId, player.CurrentOutfit.SkinId, player.CurrentOutfit.VisorId, player.CurrentOutfit.PetId, player.CurrentOutfit.NamePlateId);
                var hatId = "hat_pk02_TenGallonHat";
                if (AmongUsClient.Instance.AmClient)
                {
                    int valueOrDefault = player.CurrentOutfit.ColorId;
                    player.SetHat(hatId, valueOrDefault);
                }
                var rpcMessage = new RpcSetHatStrMessage(player.NetId, hatId, player.GetNextRpcSequenceId(RpcCalls.SetHatStr));
                RpcUtils.SendMessageImmediately(rpcMessage);
                Logger.Info($"{player.GetNameWithRole()} Become RDeputy", "Round Up");
            }
        }
    }

    public static void OnMeetingHudStart()
    {
        if (Deputy != byte.MaxValue)
        {
            MeetingHudStartPatch.AddMsg(string.Format(GetString("RoundUp.SendDeputy"), ColorString(Main.PlayerColors[Deputy], Deputy.GetPlayer().GetRealName())),
            255, ColorString(new Color32(248, 216, 110, byte.MaxValue), GetString("RoundUp").ToUpper()));
            MeetingHudStartPatch.AddMsg(GetString("RoundUp.YouBecomeDeputy"), Deputy, ColorString(new Color32(248, 216, 110, byte.MaxValue), GetString("RoundUp").ToUpper()));
        }
    }

    public static void AfterMeetingTasks()
    {
        if (Deputy != byte.MaxValue)
        {
            var player = Deputy.GetPlayer();
            if (player)
            {
                player.SetNewOutfit(PlayerOutfit, false, false);
            }
            Deputy = byte.MaxValue;
        }
    }

    public static bool DeputyCommand(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (Deputy == byte.MaxValue) return false;
        if (!GameStates.IsMeeting || !pc || GameStates.IsExilling) return false;
        if (pc.PlayerId != Deputy) return false;
        if (!pc.IsAlive()) return false;
        msg = msg.ToLower().Trim();

        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (GuessManager.CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id||編號|玩家編號")) operate = 1;
        else if (GuessManager.CheckCommond(ref msg, "ru|roundup|ls|lassoes|围捕", false)) operate = 2;
        else return false;

        if (operate == 1)
        {
            SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error) && targetId != 253)
            {
                SendMessage(error, pc.PlayerId);
                return true;
            }

            var target = GetPlayerById(targetId);
            if (target)
            {
                if (target.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target.PlayerId].Count > 0)
                {
                    target = GetPlayerById(VoodooMaster.Dolls[target.PlayerId].Where(x => GetPlayerById(x).IsAlive()).ToList().RandomElement());
                    SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
                }
                if (MeetingHud.Instance && MeetingHud.Instance.state is MeetingHud.MeetingStates.Discussion or MeetingHud.MeetingStates.Animating)
                {
                    pc.ShowInfoMessage(isUI, GetString("UseAbilityDuringDiscussion"));
                    return true;
                }
                if (Balancer.Choose && !(targetId == Balancer.Target1 || targetId == Balancer.Target2))
                {
                    pc.ShowInfoMessage(isUI, GetString("SpecialMeeting2"));
                    return true;
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    pc.ShowInfoMessage(isUI, GetString("RoundUp_LasseosSelf"));
                    return true;
                }
                if (target.Is(CustomRoles.Mini) && Mini.Age < 18)
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessMini"));
                    return true;
                }
                if (target.Is(CustomRoles.Solsticer))
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessSolsticer"));
                    return true;
                }
                if (Keeper.IsTargetExiled(target.PlayerId))
                {
                    pc.ShowInfoMessage(isUI, GetString("KeeperProtectTarget"));
                    return true;
                }

                List<MeetingHud.VoterState> statesList = [];
                MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), target.Data, false, true, target.PlayerId);
                CheckForEndVotingPatch.ConfirmEjections(target.Data);
            }
        }

        return true;
    }

    private static bool MsgToPlayerAndRole(string msg, out byte id, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num) && num <= byte.MaxValue)
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MinValue;
            error = GetString("RoundUp_Help");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = id.GetPlayer();
        if (target == null || !target.IsAlive())
        {
            error = GetString("RoundUp_Null");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting && Deputy != byte.MaxValue && target.PlayerId == Deputy)
        {
            if (!target.IsDisconnected())
            {
                target.SetNewOutfit(PlayerOutfit, false, false);
            }
            Deputy = byte.MaxValue;
            RpcVotingCompleteV2();
        }
    }
}