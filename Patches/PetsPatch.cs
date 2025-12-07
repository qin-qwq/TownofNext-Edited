using AmongUs.Data;

namespace TOHE;

public static class PetsPatch
{
    public static void RpcRemovePet(this PlayerControl pc)
    {
        if (pc == null || !pc.Data.IsDead) return;
        if (!GameStates.IsInGame) return;
        if (!Options.RemovePetsAtDeadPlayers.GetBool()) return;
        if (pc.CurrentOutfit.PetId == "") return;

        pc.RpcSetPet("");
    }

    public static string GetPetId()
    {
        var random = IRandom.Instance;
        string[] pets = Options.PetToAssign;
        string pet = pets[Options.PetToAssignToEveryone.GetValue()];
        string petId = pet == "pet_RANDOM_FOR_EVERYONE" ? HatManager.Instance.allPets[random.Next(0, HatManager.Instance.allPets.Length)].ProdId : pet;
        return string.IsNullOrEmpty(petId.Trim()) ? "pet_test" : petId;
    }
}
