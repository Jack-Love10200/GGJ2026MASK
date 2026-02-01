using UnityEngine;

[CreateAssetMenu(menuName = "Game/Mask Unlock/Click Start Minigame")]
public class ClickStartMinigameUnlockAction : MaskUnlockAction
{
    public string minigameId = "tictactoe";
    public bool debugInteractions = false;

    public override string GetHint()
    {
        return $"Click: {minigameId}";
    }

    public override void OnInteract(MinigameContext ctx, InteractionEvent evt)
    {
        if (ctx == null)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: Context is null.");
            return;
        }

        if (evt.type != InteractionType.Click)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: Ignored evt {evt.type}.");
            return;
        }

        var manager = MinigameManager.Instance;
        if (manager == null)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: MinigameManager.Instance is null.");
            return;
        }

        if (debugInteractions)
            Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: Start minigame {minigameId}.");
        manager.StartMinigame(minigameId, ctx);
    }
}
