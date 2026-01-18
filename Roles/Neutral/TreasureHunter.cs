using System.Text;
using TONE.Modules;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.Options;
using static TONE.Translator;
using static TONE.Utils;

namespace TONE.Roles.Neutral;

internal class TreasureHunter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TreasureHunter;
    private const int Id = 33900;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    public static OptionItem TreasureNum;

    public static Dictionary<Vector2, Treasure> TreasureLocation = [];
    public Vector2 TreasurePlace = Vector2.zero;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.TreasureHunter);
        TreasureNum = FloatOptionItem.Create(Id + 10, "TreasureNum", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TreasureHunter])
            .SetValueFormat(OptionFormat.Pieces);
    }

    public override void Init()
    {
        TreasureLocation.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        GetTreasure(_Player, false);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        Vector2 position = player.GetCustomPosition();
        if (Vector2.Distance(position, TreasurePlace) <= 1f && TreasurePlace != Vector2.zero)
        {
            GetTreasure(player);
        }
    }

    public override void AfterMeetingTasks()
    {
        CreateTreasure(_Player);
    }

    public static void CreateTreasure(PlayerControl pc)
    {
        var location = GetAllRandomSpawnLocation();
        var pcRoleClass = pc.GetRoleClass();
        TreasureHunter pcRole = pcRoleClass as TreasureHunter;
        pcRole.TreasurePlace = location;
        TreasureLocation.Add(location, new(location, [pc.PlayerId], pc.PlayerId));
    }

    public static void GetTreasure(PlayerControl pc, bool get = true)
    {
        var pcRoleClass = pc.GetRoleClass();
        TreasureHunter pcRole = pcRoleClass as TreasureHunter;
        pcRole.TreasurePlace = Vector2.zero;
        TreasureLocation.Values.Do(x => x.Despawn());
        TreasureLocation.Clear();
        if (get)
        {
            pc.RpcIncreaseAbilityUseLimitBy(1);
            pc.RPCPlayCustomSound("MarioCoin");
            pc.Notify(ColorString(GetRoleColor(CustomRoles.TreasureHunter), GetString("TreasureHunterGetTreasure")));
            if (pc.GetAbilityUseLimit() >= TreasureNum.GetFloat())
            {
                if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TreasureHunter);
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }            
            }
        }
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        Color TextColor = GetRoleColor(CustomRoles.TreasureHunter).ShadeColor(0.25f);

        //ProgressText.Append(GetTaskCount(playerId, comms));
        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({playerId.GetAbilityUseLimit()}/{TreasureNum.GetInt()})"));
        return ProgressText.ToString();
    }
}
