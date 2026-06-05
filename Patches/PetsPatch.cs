namespace TONE;

public static class PetsPatch
{
    public static void RpcRemovePet(this PlayerControl pc)
    {
        if (!pc || !pc.Data.IsDead) return;
        if (!GameStates.IsInGame) return;
        if (!Options.RemovePetsAtDeadPlayers.GetBool()) return;
        if (pc.CurrentOutfit.PetId == "") return;
        if (Main.CurrentServerIsVanilla) return;

        pc.RpcSetPet("");
    }

    public static string GetPetId()
    {
        var random = IRandom.Instance;
        string[] pets = Options.PetToAssign;
        string pet = pets[Options.PetToAssignToEveryone.GetValue()];
        string petId = pet == "pet_RANDOM_FOR_EVERYONE" ? HatManager.Instance.allPets[random.Next(1, HatManager.Instance.allPets.Length)].ProdId : pet;
        return string.IsNullOrEmpty(petId.Trim()) ? "pet_test" : petId;
    }

    public static void SetPet(PlayerControl pc, string petId)
    {
        var sender = CustomRpcSender.Create("PetsHelper.SetPet", Hazel.SendOption.Reliable);

        try { pc.SetPet(petId); }
        catch { }

        try { pc.Data.DefaultOutfit.PetSequenceId += 10; }
        catch { }

        sender.AutoStartRpc(pc.NetId, RpcCalls.SetPetStr)
            .Write(petId)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr))
            .EndRpc();

        sender.SendMessage();
    }
}
