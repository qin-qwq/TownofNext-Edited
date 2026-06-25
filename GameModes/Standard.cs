namespace TONE;

internal class Standard : GameModeBase
{
    public override CustomGameMode GameMode => CustomGameMode.Standard;
    public override bool NormalSelectRoles => true;
    public override bool NormalSelectAddons => true;
    public override bool CanCloseDoors => true;
    public override bool CanReport => true;
    public override bool NormalTaskText => true;
}