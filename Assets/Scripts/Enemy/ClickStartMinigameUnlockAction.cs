using UnityEngine;

[CreateAssetMenu(menuName = "Game/Mask Unlock/Click Start Minigame")]
public class ClickStartMinigameUnlockAction : MaskUnlockAction
{
    public string minigameId = "tictactoe";

    public override string GetHint()
    {
        return $"Click: {minigameId}";
    }

    public override void OnInteract(MinigameContext ctx, InteractionEvent evt)
    {
        if (ctx == null)
            return;

        if (evt.type != InteractionType.Click)
            return;

        var manager = MinigameManager.Instance;
        if (manager == null)
            return;

        manager.StartMinigame(minigameId, ctx);
    }
}
