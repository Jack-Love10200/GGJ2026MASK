using UnityEngine;

[CreateAssetMenu(menuName = "Game/Mask Unlock/Click Start Minigame")]
public class ClickStartMinigameUnlockAction : MaskUnlockAction
{
    public string minigameId = "tictactoe";
    [Header("Input")]
    public bool allowClick = true;
    public bool allowKey = false;
    public KeyCode requiredKey = KeyCode.None;
    public bool debugInteractions = false;

    public override string GetHint()
    {
        if (allowKey && !allowClick)
            return requiredKey == KeyCode.None ? "Any Key" : requiredKey.ToString();

        if (allowKey && allowClick)
            return requiredKey == KeyCode.None ? "Click or Any Key" : $"Click or {requiredKey}";

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

        bool isClick = evt.type == InteractionType.Click;
        bool isKey = evt.type == InteractionType.KeyDown;

        if (!isClick && !isKey)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: Ignored evt {evt.type}.");
            return;
        }

        if (isClick && !allowClick)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: Click input disabled.");
            return;
        }

        if (isKey)
        {
            if (!allowKey)
            {
                if (debugInteractions)
                    Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: Key input disabled.");
                return;
            }

            if (requiredKey != KeyCode.None && evt.key != requiredKey)
            {
                if (debugInteractions)
                    Debug.Log($"{nameof(ClickStartMinigameUnlockAction)}: Key {evt.key} does not match {requiredKey}.");
                return;
            }
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
