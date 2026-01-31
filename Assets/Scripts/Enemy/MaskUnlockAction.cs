using UnityEngine;

public abstract class MaskUnlockAction : ScriptableObject
{
    public virtual string GetHint()
    {
        return string.Empty;
    }

    public abstract void OnInteract(MinigameContext ctx, InteractionEvent evt);
}
