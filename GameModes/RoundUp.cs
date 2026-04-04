using System;
using System.Text.RegularExpressions;
using TONE.Roles.Coven;
using TONE.Roles.Crewmate;
using TONE.Roles.Double;
using UnityEngine;
using static TONE.CheckForEndVotingPatch;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE;

internal static class RoundUp
{
    private const int Id = 67_228_001;

    public static OptionItem ImpostorCanBecomeDeputy;
    public static OptionItem NeutralCanBecomeDeputy;
    public static OptionItem CovenCanBecomeDeputy;

    public static byte Deputy;
    public static readonly Dictionary<byte, string> PlayerHat = [];

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(10000038, "MenuTitle.RoundUp", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.RoundUp)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue));

        ImpostorCanBecomeDeputy = BooleanOptionItem.Create(Id + 2, "RoundUp_ImpostorCanBecomeDeputy", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.RoundUp)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue))
            .SetHeader(true);
        NeutralCanBecomeDeputy = BooleanOptionItem.Create(Id + 3, "RoundUp_NeutralCanBecomeDeputy", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.RoundUp)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue));
        CovenCanBecomeDeputy = BooleanOptionItem.Create(Id + 4, "RoundUp_CovenCanBecomeDeputy", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.RoundUp)
            .SetColor(new Color32(248, 216, 110, byte.MaxValue));
    }

    public static void Init()
    {
        Deputy = byte.MaxValue;
        PlayerHat.Clear();
    }

    public static void OnReportDeadBody()
    {
        var pcList = Main.EnumerateAlivePlayerControls().Where(x => x.IsPlayerCrewmateTeam() || (x.IsPlayerImpostorTeam() && ImpostorCanBecomeDeputy.GetBool()) ||
        (x.IsPlayerNeutralTeam() && NeutralCanBecomeDeputy.GetBool() && !x.Is(CustomRoles.Jester) && !x.Is(CustomRoles.Executioner) && !x.Is(CustomRoles.Pixie) && !x.Is(CustomRoles.Solsticer)) ||
        (x.IsPlayerCovenTeam() && CovenCanBecomeDeputy.GetBool())).ToList();

        if (pcList.Any())
        {
            var player = pcList.RandomElement();
            var hat = player.Data.Outfits[PlayerOutfitType.Default].HatId;
            Deputy = player.PlayerId;
            PlayerHat.Remove(player.PlayerId);
            PlayerHat.Add(player.PlayerId, hat);
            if (AmongUsClient.Instance.AmHost) player.RpcSetHat("hat_pk02_TenGallonHat");
        }
    }

    public static void OnMeetingHudStart()
    {
        if (Deputy != byte.MaxValue)
        {
            MeetingHudStartPatch.AddMsg(string.Format(GetString("RoundUp.SendDeputy"), ColorString(Deputy.GetPlayerColor(), Deputy.GetPlayer().GetRealName())),
            255, ColorString(new Color32(248, 216, 110, byte.MaxValue), GetString("RoundUp").ToUpper()));
            MeetingHudStartPatch.AddMsg(GetString("RoundUp.YouBecomeDeputy"), Deputy, ColorString(new Color32(248, 216, 110, byte.MaxValue), GetString("RoundUp").ToUpper()));
        }
    }

    public static void AfterMeetingTasks()
    {
        if (Deputy != byte.MaxValue)
        {
            if (PlayerHat.ContainsKey(Deputy) && AmongUsClient.Instance.AmHost)
            {
                var player = Deputy.GetPlayer();
                player.RpcSetHat(PlayerHat[player.PlayerId]);
                PlayerHat.Clear();
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
        if (Options.CurrentGameMode != CustomGameMode.RoundUp) return false;
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
                if (MeetingHud.Instance && MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.Animating)
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
                if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
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
                MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), target.Data, false);
                ConfirmEjections(target.Data);
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
            if (!target.IsDisconnected() && PlayerHat.ContainsKey(target.PlayerId) && AmongUsClient.Instance.AmHost)
            {
                target.RpcSetHat(PlayerHat[target.PlayerId]);
                PlayerHat.Clear();
            }
            Deputy = byte.MaxValue;
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
        }
    }
}