using System;
using TONE.Roles.AddOns.Common;
using TONE.Roles.AddOns.Impostor;
using TONE.Roles.Neutral;

namespace TONE.Roles.Core.AssignManager;

public static class AddonAssign
{
    private static readonly HashSet<CustomRoles> AddonRolesList = [];
    public static Dictionary<byte, HashSet<CustomRoles>> SetAddOns = [];

    private static bool NotAssignAddOnInGameStarted(CustomRoles role)
    {
        switch (role)
        {
            case CustomRoles.Lovers:
            case CustomRoles.Workhorse:
            case CustomRoles.LastImpostor:
            case CustomRoles.Narc:
                return true;
            case CustomRoles.Autopsy when Options.EveryoneCanSeeDeathReason.GetBool():
            case CustomRoles.Madmate when Madmate.MadmateSpawnMode.GetInt() != 0:
            case CustomRoles.Glow or CustomRoles.Mare or CustomRoles.Torch when GameStates.FungleIsActive:
            case CustomRoles.Guesser when Guesser.AdvancedSettings.GetBool():
                return true;
        }

        return false;
    }

    public static void StartSelect()
    {
        if (Options.CurrentGameMode != CustomGameMode.Standard) return;

        AddonRolesList.Clear();
        foreach (var cr in CustomRolesHelper.AllRoles)
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (!role.IsAdditionRole()) continue;

            if (NotAssignAddOnInGameStarted(role)) continue;

            AddonRolesList.Add(role);
        }
    }
    public static void StartSortAndAssign()
    {
        if (Options.CurrentGameMode != CustomGameMode.Standard) return;

        var rd = IRandom.Instance;
        List<CustomRoles> addonsList = [];
        List<CustomRoles> addonsIsEnableList = [];

        // Sort Add-ons by spawn rate
        var sortAddOns = Options.CustomAdtRoleSpawnRate.OrderByDescending(role => role.Value.GetFloat());
        var dictionarSortAddOns = sortAddOns.ToDictionary(x => x.Key, x => x.Value);

        // Add only enabled add-ons
        foreach (var addonKVP in dictionarSortAddOns.Where(a => a.Key.IsEnable()).ToArray())
        {
            if (!NotAssignAddOnInGameStarted(addonKVP.Key))
            {
                addonsIsEnableList.Add(addonKVP.Key);
            }
        }

        Logger.Info($"Number enabled of add-ons (before priority): {addonsIsEnableList.Count}", "Check Add-ons Count");

        // Add addons which have a percentage greater than 90
        foreach (var addonKVP in dictionarSortAddOns.Where(a => a.Key.IsEnable() && a.Value.GetFloat() >= 90).ToArray())
        {
            var addon = addonKVP.Key;

            if (AddonRolesList.Contains(addon))
            {
                addonsList.Add(addon);
                addonsIsEnableList.Remove(addon);
            }
        }

        if (addonsList.Count > 2)
            addonsList = addonsList.Shuffle(rd).ToList();

        Logger.Info($"Number enabled of add-ons (after priority): {addonsIsEnableList.Count}", "Check Add-ons Count");

        // Add addons randomly
        while (addonsIsEnableList.Any())
        {
            var randomAddOn = addonsIsEnableList.RandomElement();

            if (!addonsList.Contains(randomAddOn) && AddonRolesList.Contains(randomAddOn))
            {
                addonsList.Add(randomAddOn);
            }

            // Even if an add-on cannot be added, it must be removed from the "addonsIsEnableList"
            // To prevent the game from freezing
            addonsIsEnableList.Remove(randomAddOn);
        }

        Logger.Info($" Is Started", "Assign Add-ons");

        // Assign Set Add-Ons
        foreach ((byte id, HashSet<CustomRoles> addons) in SetAddOns)
        {
            var player = id.GetPlayer();

            foreach (CustomRoles addon in addons)
            {
                if (!CustomRolesHelper.CheckAddonConfilct(addon, player)) continue;

                // Set Add-on
                Main.PlayerStates[player.PlayerId].SetSubRole(addon);
                Logger.Info($"Registered Add-on: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {addon}", $"Assign {addon}");

                addonsList.Remove(addon);
            }

        }

        // Assign add-ons
        foreach (var addOn in addonsList.ToArray())
        {
            if (rd.Next(1, 101) <= (Options.CustomAdtRoleSpawnRate.TryGetValue(addOn, out var sc) ? sc.GetFloat() : 0))
            {
                AssignSubRoles(addOn);
            }
        }
    }
    public static void AssignSubRoles(CustomRoles role, int RawCount = -1)
    {
        try
        {
            var allAlivePlayers = Main.EnumerateAlivePlayerControls().ToList();
            if (!allAlivePlayers.Any()) return;

            var eligiblePlayers = new List<PlayerControl>();

            foreach (var pc in allAlivePlayers)
            {
                if (CustomRolesHelper.CheckAddonConfilct(role, pc))
                {
                    eligiblePlayers.Add(pc);
                }
            }

            if (!eligiblePlayers.Any()) return;

            var count = Math.Clamp(RawCount, 0, eligiblePlayers.Count);

            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, eligiblePlayers.Count);

            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var player = eligiblePlayers.RandomElement();
                eligiblePlayers.Remove(player);

                Main.PlayerStates[player.PlayerId].SetSubRole(role);

                Logger.Info($"Registered Add-on: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {role}", $"Assign {role}");
            }
        }
        catch (Exception error)
        {
            Logger.Warn($"Add-On {role} get error after check addon confilct for: {error}", "AssignSubRoles");
        }
    }

    public static void InitAndStartAssignLovers()
    {
        var rd = IRandom.Instance;
        if (CustomRoles.Lovers.IsEnable() && (CustomRoles.Hater.IsEnable() ? -1 : rd.Next(1, 100)) <= CustomRoles.Lovers.GetMode())
        {
            // Initialize Lovers
            Lovers.LoversPlayers.Clear();
            Lovers.isLoversDead = false;

            //Two randomly selected
            AssignLovers();
        }
    }
    private static void AssignLovers(int RawCount = -1)
    {
        if (RoleAssign.RoleResult.ContainsValue(CustomRoles.Cupid)) return;
        var allPlayers = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoles.GM)
                || (pc.HasSubRole() && pc.GetCustomSubRoles().Count >= Options.NoLimitAddonsNumMax.GetInt())
                || pc.Is(CustomRoles.Dictator)
                || pc.Is(CustomRoles.God)
                || pc.Is(CustomRoles.Hater)
                || pc.Is(CustomRoles.Sunnyboy)
                || pc.Is(CustomRoles.Bomber)
                || pc.Is(CustomRoles.Provocateur)
                || pc.Is(CustomRoles.RuthlessRomantic)
                || pc.Is(CustomRoles.Romantic)
                || pc.Is(CustomRoles.VengefulRomantic)
                || pc.Is(CustomRoles.Workaholic)
                || pc.Is(CustomRoles.Solsticer)
                || pc.Is(CustomRoles.Mini)
                || pc.Is(CustomRoles.Wraith)
                || (pc.GetCustomRole().IsCrewmate() && !Lovers.CrewCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsNeutral() && !Lovers.NeutralCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsImpostor() && !Lovers.ImpCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsCoven() && !Lovers.CovenCanBeInLove.GetBool()))
                continue;

            allPlayers.Add(pc);
        }
        var role = CustomRoles.Lovers;
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0 || allPlayers.Count <= 1) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers.RandomElement();
            Lovers.LoversPlayers.Add(player);
            allPlayers.Remove(player);
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info($"Registered Lovers: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {role}", "Assign Lovers");
        }
        if (Lovers.LoversPlayers.Any())
            RPC.SyncLoversPlayers();
    }

    public static void StartAssigningNarc()
    {
        var ps = Main.PlayerStates.Values.FirstOrDefault(x => x.MainRole == NarcManager.RoleForNarcToSpawnAs) ?? null;

        if (ps == null)
        {
            NarcManager.RoleForNarcToSpawnAs = CustomRoles.NotAssigned;
            return;
        }
        ps.SetSubRole(CustomRoles.Narc);

        // logs the assigning
        var pc = ps.PlayerId.GetPlayer();
        if (pc != null)
        {
            Logger.Info($"将警局特工分配给 {pc?.Data?.PlayerName}({pc.PlayerId})。 {pc?.Data?.PlayerName}的职业是： {pc.GetCustomRole()} + 警局特工", "Assign Narc");
        }
    }

    public static void StartAssigningGuesser()
    {
        if (!Guesser.AdvancedSettings.GetBool() || !CustomRoles.Guesser.IsEnable()) return;
        var random = IRandom.Instance;
        List<PlayerControl> AllPlayers = Main.EnumeratePlayerControls().Shuffle(random).ToList();
        var ImpNum = Guesser.GImpMax.GetInt();
        var CrewNum = Guesser.GCrewMax.GetInt();
        var NeuNum = Guesser.GNeuMax.GetInt();
        var CovenNum = Guesser.GCovenMax.GetInt();
        var role = CustomRoles.Guesser;
        foreach (PlayerControl pc in AllPlayers)
        {
            if (pc == null) continue;
            if (Options.GuesserMode.GetBool() && ((pc.GetCustomRole().IsCrewmate() && Options.CrewmatesCanGuess.GetBool())
            || (pc.GetCustomRole().IsNK() && Options.NeutralKillersCanGuess.GetBool())
            || (pc.GetCustomRole().IsImpostor() && Options.ImpostorsCanGuess.GetBool())
            || (pc.GetCustomRole().IsCoven() && Options.CovenCanGuess.GetBool())
            || (pc.GetCustomRole().IsNA() && Options.NeutralApocalypseCanGuess.GetBool())
            || (pc.GetCustomRole().IsNonNK() && Options.PassiveNeutralsCanGuess.GetBool())))
                continue;
            if (pc.Is(CustomRoles.EvilGuesser)
                    || pc.Is(CustomRoles.NiceGuesser)
                    || pc.Is(CustomRoles.Judge)
                    || pc.Is(CustomRoles.CopyCat)
                    || pc.Is(CustomRoles.Doomsayer)
                    || pc.Is(CustomRoles.Nemesis)
                    || pc.Is(CustomRoles.Councillor)
                    || pc.Is(CustomRoles.GuardianAngelTONE)
                    || pc.Is(CustomRoles.GM)
                    || pc.Is(CustomRoles.Retributionist)
                    || (pc.HasSubRole() && pc.GetCustomSubRoles().Count >= Options.NoLimitAddonsNumMax.GetInt())
                    || pc.Is(CustomRoles.PunchingBag))
                continue;
            if ((pc.Is(CustomRoles.Specter) && !Specter.CanGuess.GetBool())
                    || (pc.Is(CustomRoles.Terrorist) && (!Terrorist.TerroristCanGuess.GetBool() || Terrorist.CanTerroristSuicideWin.GetBool()))
                    || (pc.Is(CustomRoles.Workaholic) && !Workaholic.WorkaholicCanGuess.GetBool())
                    || (pc.Is(CustomRoles.Solsticer) && !Solsticer.SolsticerCanGuess.GetBool())
                    || (pc.Is(CustomRoles.God) && !God.CanGuess.GetBool()))
                continue;
            if ((pc.GetCustomRole().IsCrewmate() && !Guesser.CrewCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Guesser.NeutralCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Guesser.ImpCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsCoven() && !Guesser.CovenCanBeGuesser.GetBool()))
                continue;
            if (pc.GetCustomRole().IsInvestigativeRole() && Options.InvestigativeRoleCantGuess.GetBool())
                continue;
            if (ImpNum > 0 && pc.IsPlayerImpostorTeam() && Guesser.ImpCanBeGuesser.GetBool())
            {
                Main.PlayerStates[pc.PlayerId].SetSubRole(role);
                ImpNum--;
            }
            else if (CrewNum > 0 && pc.IsPlayerCrewmateTeam() && Guesser.CrewCanBeGuesser.GetBool())
            {
                Main.PlayerStates[pc.PlayerId].SetSubRole(role);
                CrewNum--;
            }
            else if (NeuNum > 0 && pc.IsPlayerNeutralTeam() && Guesser.NeutralCanBeGuesser.GetBool())
            {
                Main.PlayerStates[pc.PlayerId].SetSubRole(role);
                NeuNum--;
            }
            else if (CovenNum > 0 && pc.IsPlayerCovenTeam() && Guesser.CovenCanBeGuesser.GetBool())
            {
                Main.PlayerStates[pc.PlayerId].SetSubRole(role);
                CovenNum--;
            }
        }
    }
}
