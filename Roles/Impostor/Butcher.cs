using Hazel;
using TONE.Modules;
using TONE.Roles.Core;
using UnityEngine;
using static TONE.Options;

namespace TONE.Roles.Impostor;

internal class Butcher : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Butcher;
    private const int Id = 24300;

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Butcher);
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(Translator.GetString("ButcherButtonText"));

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;
        if (target == null) return;

        target.SetRealKiller(killer);
        target.SetDeathReason(PlayerState.DeathReason.Dismembered);
        Main.PlayerStates[target.PlayerId].SetDead();

        Main.OverDeadPlayerList.Add(target.PlayerId);
        var rd = IRandom.Instance;

        if (target.Is(CustomRoles.Avanger))
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);

            var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId); //No need to do extra check cause nobody is winning
            pcList.Do(x =>
            {
                x.SetDeathReason(PlayerState.DeathReason.Revenge);
                target.RpcSpecificMurderPlayer(x, x);
                x.SetRealKiller(target);
                Main.PlayerStates[x.PlayerId].SetDead();
            });
            return;
        }

        _ = new LateTask(() =>
        {
            for (var i = 0; i < 30; i++)
            {
                if (!GameStates.IsInTask) return;
                var ops = target.GetCustomPosition();

                Vector2 location = new(ops.x + ((float)(rd.Next(1, 200) - 100) / 100), ops.y + ((float)(rd.Next(1, 200) - 100) / 100));
                Utils.RpcCreateDeadBody(location, (byte)target.CurrentOutfit.ColorId, target, SendOption.None);
            }
        }, 0.2f, "Butcher Show Bodies");
    }
}
