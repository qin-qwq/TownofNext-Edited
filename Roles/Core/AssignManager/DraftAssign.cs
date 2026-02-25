using System.Text;
using AmongUs.GameOptions;
using TONE.Roles.Core.AssignManager;
using TONE.Roles.Crewmate;
using TONE.Roles.Impostor;
using TONE.Roles.Neutral;
using static TONE.Translator;

namespace TONE.Roles.Core.DraftAssign;

public static class DraftAssign
{
    public static List<CustomRoles> AllRoles = [];
    public static Dictionary<byte, List<CustomRoles>> DraftPools = [];
    public static Dictionary<byte, CustomRoles> DraftRoles = [];

    public static void GetNeutralCounts(int NKmaxOpt, int NKminOpt, int NNKmaxOpt, int NNKminOpt, int NAmaxOpt, int NAminOpt, ref int ResultNKnum, ref int ResultNNKnum, ref int ResultNAnum)
    {
        var rd = IRandom.Instance;

        if (NNKmaxOpt > 0 && NNKmaxOpt >= NNKminOpt)
        {
            ResultNNKnum = rd.Next(NNKminOpt, NNKmaxOpt + 1);
            ResultNNKnum -= RoleAssign.SetRoles.Values.Count(x => x.IsNNK());
            if (ResultNNKnum < 0) ResultNNKnum = 0;
        }

        if (NKmaxOpt > 0 && NKmaxOpt >= NKminOpt)
        {
            ResultNKnum = rd.Next(NKminOpt, NKmaxOpt + 1);
            ResultNKnum -= RoleAssign.SetRoles.Values.Count(x => x.IsNK());
            if (ResultNKnum < 0) ResultNKnum = 0;
        }

        if (NAmaxOpt > 0 && NAmaxOpt >= NAminOpt)
        {
            ResultNAnum = rd.Next(NAminOpt, NAmaxOpt + 1);
            ResultNAnum -= RoleAssign.SetRoles.Values.Count(x => x.IsNA());
            if (ResultNAnum < 0) ResultNAnum = 0;
        }
    }

    public static void GetCovenCounts(int CVmaxOpt, int CVminOpt, ref int ResultCVnum)
    {
        var rd = IRandom.Instance;

        if (CVmaxOpt > 0 && CVmaxOpt >= CVminOpt)
        {
            ResultCVnum = rd.Next(CVminOpt, CVmaxOpt + 1);
            ResultCVnum -= RoleAssign.SetRoles.Values.Count(x => x.IsCoven());
            if (ResultCVnum < 0) ResultCVnum = 0;
        }
    }

    public static void GetImpCounts(int ImpmaxOpt, int ImpminOpt, ref int ResultImpnum)
    {
        if (!Options.UseVariableImp.GetBool())
        {
            ResultImpnum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
            ResultImpnum -= RoleAssign.SetRoles.Values.Count(x => x.IsImpostor());
            if (ResultImpnum < 0) ResultImpnum = 0;
            return;
        }
        var rd = IRandom.Instance;

        if (ImpmaxOpt > 0 && ImpmaxOpt >= ImpminOpt)
        {
            ResultImpnum = rd.Next(ImpminOpt, ImpmaxOpt + 1);
            ResultImpnum -= RoleAssign.SetRoles.Values.Count(x => x.IsImpostor());
            if (ResultImpnum < 0) ResultImpnum = 0;
        }
    }

    public static void Reset()
    {
        AllRoles = [];
        DraftPools.Clear();
        DraftRoles.Clear();
        foreach (var pc in Main.EnumerateAlivePlayerControls())
        {
            DraftPools[pc.PlayerId] = [];
            DraftRoles[pc.PlayerId] = CustomRoles.NotAssigned;
        }
    }

    public static void StartSelect()
    {
        Reset();

        var rd = IRandom.Instance;
        int draftCount = Options.DraftableCount.GetInt();
        int playerCount = Main.AllAlivePlayerControls.Count;
        int optImpNum = 0;
        int optNonNeutralKillingNum = 0;
        int optNeutralKillingNum = 0;
        int optNeutralApocalypseNum = 0;
        int optCovenNum = 0;

        GetNeutralCounts(Options.NeutralKillingRolesMaxPlayer.GetInt(), Options.NeutralKillingRolesMinPlayer.GetInt(), Options.NonNeutralKillingRolesMaxPlayer.GetInt(), Options.NonNeutralKillingRolesMinPlayer.GetInt(), Options.NeutralApocalypseRolesMaxPlayer.GetInt(), Options.NeutralApocalypseRolesMinPlayer.GetInt(), ref optNeutralKillingNum, ref optNonNeutralKillingNum, ref optNeutralApocalypseNum);
        GetCovenCounts(Options.CovenRolesMaxPlayer.GetInt(), Options.CovenRolesMinPlayer.GetInt(), ref optCovenNum);
        GetImpCounts(Options.ImpRolesMaxPlayer.GetInt(), Options.ImpRolesMinPlayer.GetInt(), ref optImpNum);

        List<CustomRoles> allRoles = EnumHelper.GetAllValues<CustomRoles>().Where(x => !NoAssignRoles(x) && (!Options.DraftAffectedByRoleSpawnChances.GetBool() || IRandom.Instance.Next(100) < x.GetMode())).Shuffle(rd).ToList();

        if (allRoles.Count < playerCount * draftCount)
        {
            Logger.SendInGame(GetString("DraftNotEnoughRoles"));
            return;
        }

        var ImpRoles = allRoles.Where(x => x.IsImpostor()).Shuffle(rd).Take(optImpNum * draftCount);
        var CovenRoles = allRoles.Where(x => x.IsCoven()).Shuffle(rd).Take(optCovenNum * draftCount);
        var NARoles = allRoles.Where(x => x.IsNA()).Shuffle(rd).Take(optNeutralApocalypseNum * draftCount);
        var NKRoles = allRoles.Where(x => x.IsNK()).Shuffle(rd).Take(optNeutralKillingNum * draftCount);
        var NNKRoles = allRoles.Where(x => x.IsNNK()).Shuffle(rd).Take(optNonNeutralKillingNum * draftCount);

        if (ImpRoles.Count() < optImpNum * Options.DraftableCount.GetInt() || CovenRoles.Count() < optCovenNum * Options.DraftableCount.GetInt()
        || NARoles.Count() < optNeutralApocalypseNum * Options.DraftableCount.GetInt() || NKRoles.Count() < optNeutralKillingNum * Options.DraftableCount.GetInt()
        || NNKRoles.Count() < optNonNeutralKillingNum * Options.DraftableCount.GetInt())
        {
            Logger.SendInGame(GetString("DraftNotEnoughRoles"));
            return;
        }

        allRoles.RemoveAll(x => x.IsImpostor());
        allRoles.RemoveAll(x => x.IsCoven());
        allRoles.RemoveAll(x => x.IsNA());
        allRoles.RemoveAll(x => x.IsNK());
        allRoles.RemoveAll(x => x.IsNonNK());

        var num = playerCount - optImpNum - optNonNeutralKillingNum - optNeutralKillingNum - optNeutralApocalypseNum - optCovenNum - RoleAssign.SetRoles.Values.Count > 0 ? 
            (playerCount - optImpNum - optNonNeutralKillingNum - optNeutralKillingNum - optNeutralApocalypseNum - optCovenNum - RoleAssign.SetRoles.Values.Count) * Options.DraftableCount.GetInt() :
            0;

        AllRoles = allRoles
            .Take(num)
            .CombineWith(ImpRoles, CovenRoles, NARoles, NKRoles, NNKRoles)
            .Shuffle()
            .ToList();

        if (AllRoles.Count < (playerCount - RoleAssign.SetRoles.Values.Count) * draftCount)
        {
            for (var i = 0; i < (playerCount - RoleAssign.SetRoles.Values.Count) * draftCount - AllRoles.Count; i++)
            {
                AllRoles.Add(CustomRoles.CrewmateTONE);
            }
        }

        if (Sunnyboy.CheckSpawn() && AllRoles.Remove(CustomRoles.Jester)) AllRoles.Add(CustomRoles.Sunnyboy);
        if (Bard.CheckSpawn() && AllRoles.Remove(CustomRoles.Arrogance)) AllRoles.Add(CustomRoles.Bard);
        if (Requiter.CheckSpawn() && AllRoles.Remove(CustomRoles.Knight)) AllRoles.Add(CustomRoles.Requiter);

        List<PlayerControl> AllPlayers = Main.EnumeratePlayerControls().Shuffle(rd).ToList();

        foreach (var pc in AllPlayers)
        {
            if (pc == null) continue;

            if (RoleAssign.SetRoles.ContainsKey(pc.PlayerId))
            {
                for (var i = 0; i < draftCount; i++)
                {
                    DraftPools[pc.PlayerId].Add(RoleAssign.SetRoles[pc.PlayerId]);
                }
            }
            else
            {
                var team = AllRoles.RandomElement();
                var Roles = AllRoles.Where(x => IsSameTeamRoles(team, x)).Shuffle(rd).Take(draftCount);
                foreach (var role in Roles)
                {
                    DraftPools[pc.PlayerId].Add(role);
                    AllRoles.Remove(role);
                }
            }
        }

        foreach (var player in AllPlayers)
        {
            SendDraftPoolMsg(player);
        }
    }

    public static List<CustomRoles> GetDraftPool(this PlayerControl player) => DraftPools[player.PlayerId];

    public static string GetFormattedDraftPool(this PlayerControl player)
    {
        StringBuilder sb = new();

        int i = 1;
        foreach (var role in player.GetDraftPool())
        {
            sb.Append('\n');
            sb.Append(i++).Append(". ");
            sb.Append(role.ToColoredString());
        }

        return sb.ToString();
    }

    public static void SendDraftDescription(this PlayerControl player, int index)
    {
        index--;
        byte playerId = player.PlayerId;
        var result = CustomRoles.NotAssigned;
        var pool = DraftPools[player.PlayerId];
        if (index < pool.Count)
            result = pool[index];

        if (result == CustomRoles.NotAssigned)
            return;

        var Des = result.GetInfoLong();
        var title = "▲" + $"<color=#ffffff>" + result.GetRoleTitle() + "</color>\n";
        var Conf = new StringBuilder();
        string rlHex = Utils.GetRoleColorCode(result);
        if (Options.CustomRoleSpawnChances.ContainsKey(result))
        {
            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[result], ref Conf);
            var cleared = Conf.ToString();
            var Setting = $"<color={rlHex}>{GetString(result.ToString())} {GetString("Settings:")}</color>\n";
            Conf.Clear().Append($"<color=#ffffff>" + $"<size={ChatCommands.Csize}>" + Setting + cleared + "</size>" + "</color>");

        }
        // Show role info
        Utils.SendMessage(Des, playerId, title, noReplay: true);

        // Show role settings
        Utils.SendMessage("", playerId, Conf.ToString(), noReplay: true);
    }

    public static void DraftedRoles(PlayerControl player, int index, bool send = true)
    {
        if (!Options.DraftMode.GetBool())
        {
            Utils.SendMessage(GetString("Message.DraftModeDisabled"), player.PlayerId);
            return;
        }
        if (!DraftPools.ContainsKey(player.PlayerId)) return;
        if (index == 11)
        {
            Utils.SendMessage(GetString("DraftSelectionCleared"), player.PlayerId, noReplay: true);
            DraftRoles[player.PlayerId] = CustomRoles.NotAssigned;
            return;
        }
        else if (index < 1 || index > DraftPools[player.PlayerId].Count)
        {
            Utils.SendMessage(GetString("InvalidDraftSelection"), player.PlayerId, noReplay: true);
            return;
        }
        else
        {
            var role = DraftPools[player.PlayerId][index - 1];
            DraftRoles[player.PlayerId] = role;
            if (send)
            {
                Utils.SendMessage(string.Format(GetString("DraftSelection"), role.ToColoredString()), player.PlayerId, noReplay: true);
                SendDraftDescription(player, index);
            }
        }
    }

    public static void DraftDescriptionRoles(PlayerControl player, int index)
    {
        if (!Options.DraftMode.GetBool())
        {
            Utils.SendMessage(GetString("Message.DraftModeDisabled"), player.PlayerId);
            return;
        }
        if (!DraftPools.ContainsKey(player.PlayerId)) return;
        if (index < 1 || index > DraftPools[player.PlayerId].Count)
        {
            Utils.SendMessage(GetString("InvalidDraftSelection"), player.PlayerId, noReplay: true);
            return;
        }
        else
        {
            SendDraftDescription(player, index);
        }
    }

    public static void SendDraftPoolMsg(PlayerControl player)
    {
        Utils.SendMessage(string.Format(GetString("DraftPoolMessage"), player.GetFormattedDraftPool()), player.PlayerId, noReplay: true);
    }

    public static bool IsSameTeamRoles(CustomRoles role, CustomRoles role2)
    {
        if ((role.IsCrewmate() && role2.IsCrewmate()) || (role.IsImpostor() && role2.IsImpostor()) || (role.IsCoven() && role2.IsCoven())
            || (role.IsNonNK() && role2.IsNonNK()) || (role.IsNK() && role2.IsNK()) || (role.IsNA() && role2.IsNA()))
        {
            return true;
        }
        return false;
    }

    public static bool NoAssignRoles(CustomRoles role)
    {
        int chance = role.GetMode();
        if (role.IsVanilla() || chance == 0 || role.IsAdditionRole() || role.IsGhostRole() || (role.OnlySpawnsWithPetsRole() && !Options.UsePets.GetBool())) return true;
        if (RoleAssign.SetRoles.ContainsValue(role)) return true;
        switch (role)
        {
            case CustomRoles.Stalker when GameStates.FungleIsActive:
            case CustomRoles.Lighter when GameStates.FungleIsActive:
            case CustomRoles.Camouflager when GameStates.FungleIsActive:
            case CustomRoles.Doctor when Options.EveryoneCanSeeDeathReason.GetBool():
            case CustomRoles.VengefulRomantic:
            case CustomRoles.RuthlessRomantic:
            case CustomRoles.GM:
            case CustomRoles.NotAssigned:
            case CustomRoles.NiceMini:
            case CustomRoles.EvilMini:
            case CustomRoles.Runner:
            case CustomRoles.PhantomTONE when NarcManager.IsNarcAssigned():
            case CustomRoles.Mini:
            case CustomRoles.NiceGuesser when Options.GuesserMode.GetBool() && Options.CrewmatesCanGuess.GetBool():
            case CustomRoles.EvilGuesser when Options.GuesserMode.GetBool() && Options.ImpostorsCanGuess.GetBool():
                return true;
        }
        return false;
    }
}