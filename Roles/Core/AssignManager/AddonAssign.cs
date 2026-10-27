using System;
using TONE.Roles.AddOns.Common;
using TONE.Roles.AddOns.Impostor;
using TONE.Roles.Neutral;

namespace TONE.Roles.Core.AssignManager;

public static class AddonAssign
{
    private static readonly HashSet<CustomRoles> AddonRolesList = [];
    public static Dictionary<byte, HashSet<CustomRoles>> SetAddOns = [];
    private static readonly HashSet<PlayerControl> LoversList = [];

    private static bool NotAssignAddOnInGameStarted(CustomRoles role)
    {
        if (role.NotAssignInVanillaServer() && Main.CurrentServerIsVanilla) return true;

        switch (role)
        {
            //case CustomRoles.Lovers:
            case CustomRoles.Workhorse:
            case CustomRoles.LastImpostor:
            case CustomRoles.Narc:
            case CustomRoles.RDeputy:
                return true;
            case CustomRoles.Autopsy when Options.EveryoneCanSeeDeathReason.GetBool():
            case CustomRoles.Madmate when Madmate.MadmateSpawnMode.GetInt() != 0:
            case CustomRoles.Glow or CustomRoles.Mare or CustomRoles.Torch when GameStates.FungleIsActive:
                return true;
        }

        return false;
    }

    public static void StartSelect()
    {
        if (!GameModeBase.GetGameMode().GetGameModeClass().NormalSelectAddons) return;

        AddonRolesList.Clear();
        LoversList.Clear();
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
        if (!GameModeBase.GetGameMode().GetGameModeClass().NormalSelectAddons) return;

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

                if (addon is CustomRoles.Lovers)
                {
                    LoversList.Add(player);
                    continue;
                }

                // Set Add-on
                Main.PlayerStates[player.PlayerId].SetSubRole(addon);
                Logger.Info($"Registered Add-on: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {addon}", $"Assign {addon}");

                addonsList.Remove(addon);
            }

        }

        if (LoversList.Count > CustomRoles.Lovers.GetCount())
        {
            for (var i = 0; i < LoversList.Count - CustomRoles.Lovers.GetCount(); i++)
            {
                var player = LoversList.RandomElement();
                LoversList.Remove(player);
            }
        }

        if (LoversList.Count % 2 != 0)
        {
            var addon = CustomRoles.Lovers;
            var player = LoversList.RandomElement();
            var allAlivePlayers = Main.EnumerateAlivePlayerControls().Where(x => CustomRolesHelper.CheckAddonConfilct(addon, x) && x != player).ToList();
            if (!allAlivePlayers.Any()) return;
            var pc = allAlivePlayers.RandomElement();
            Main.PlayerStates[pc.PlayerId].SetSubRole(addon);
            Main.PlayerStates[player.PlayerId].SetSubRole(addon);
            Logger.Info($"Registered Add-on: {pc?.Data?.PlayerName} = {pc.GetCustomRole()} + {addon}", $"Assign {addon}");
            Logger.Info($"Registered Add-on: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {addon}", $"Assign {addon}");
            addonsList.Remove(addon);
        }
        else if (LoversList.Count > 0)
        {
            var addon = CustomRoles.Lovers;
            foreach (var player in LoversList)
            {
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
            var eligiblePlayers = AssignTargetList(role);

            if (!eligiblePlayers.Any()) return;

            var count = Math.Clamp(RawCount, 0, eligiblePlayers.Count);

            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, eligiblePlayers.Count);

            if (role == CustomRoles.Lovers)
            {
                if (count % 2 != 0)
                {
                    count = Math.Max(0, count - 1);
                }

                var maxEven = eligiblePlayers.Count;
                if (maxEven % 2 != 0) maxEven -= 1;
                count = Math.Min(count, maxEven);
            }

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

    ///<summary>
    ///要分配的玩家列表
    ///</summary>
    private static List<PlayerControl> AssignTargetList(CustomRoles role)
    {
        var rnd = IRandom.Instance;
        var candidates = new List<PlayerControl>();
        var validPlayers = Main.EnumerateAlivePlayerControls().Where(pc => CustomRolesHelper.CheckAddonConfilct(role, pc));

        if (!Options.AddonTeamMaximumSettings.TryGetValue(role, out var data))
        {
            return validPlayers.ToList();
        }
        else
        {
            var crewmateMaximum = data.Crew.GetInt();
            if (crewmateMaximum > 0)
            {
                var crewmates = validPlayers.Where(pc
                    => pc.GetCustomRole().IsCrewmate()).ToList();
                for (var i = 0; i < crewmateMaximum; i++)
                {
                    if (crewmates.Count == 0) break;
                    var selectedCrewmate = crewmates[rnd.Next(crewmates.Count)];
                    candidates.Add(selectedCrewmate);
                    crewmates.Remove(selectedCrewmate);
                }
            }

            var impostorMaximum = data.Imp.GetInt();
            if (impostorMaximum > 0)
            {
                var impostors = validPlayers.Where(pc
                    => pc.GetCustomRole().IsImpostor()).ToList();
                for (var i = 0; i < impostorMaximum; i++)
                {
                    if (impostors.Count == 0) break;
                    var selectedImpostor = impostors[rnd.Next(impostors.Count)];
                    candidates.Add(selectedImpostor);
                    impostors.Remove(selectedImpostor);
                }
            }

            var neutralMaximum = data.Neutral.GetInt();
            if (neutralMaximum > 0)
            {
                var neutrals = validPlayers.Where(pc
                    => pc.GetCustomRole().IsNeutral()).ToList();
                for (var i = 0; i < neutralMaximum; i++)
                {
                    if (neutrals.Count == 0) break;
                    var selectedNeutral = neutrals[rnd.Next(neutrals.Count)];
                    candidates.Add(selectedNeutral);
                    neutrals.Remove(selectedNeutral);
                }
            }

            var covenMaximum = data.Coven.GetInt();
            if (covenMaximum > 0)
            {
                var covens = validPlayers.Where(pc
                    => pc.GetCustomRole().IsCoven()).ToList();
                for (var i = 0; i < covenMaximum; i++)
                {
                    if (covens.Count == 0) break;
                    var selectedCoven = covens[rnd.Next(covens.Count)];
                    candidates.Add(selectedCoven);
                    covens.Remove(selectedCoven);
                }
            }
        }

        return candidates;
    }

    /*public static void InitAndStartAssignLovers()
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
        if ((Options.CustomAdtRoleSpawnRate.TryGetValue(CustomRoles.Lovers, out var sc) ? sc.GetFloat() : 0) == 0) return;
        if (Options.NoLimitAddonsNumMax.GetInt() == 0) return;
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
                || pc.Is(CustomRoles.Cupid)
                || pc.Is(CustomRoles.Wraith)
                || pc.Is(CustomRoles.CopyCat)
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
    }*/

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
        if (pc)
        {
            Logger.Info($"将警局特工分配给 {pc?.Data?.PlayerName}({pc.PlayerId})。 {pc?.Data?.PlayerName}的职业是： {pc.GetCustomRole()} + 警局特工", "Assign Narc");
        }
    }
}
