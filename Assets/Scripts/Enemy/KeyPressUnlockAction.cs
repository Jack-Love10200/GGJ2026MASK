using UnityEngine;

[CreateAssetMenu(menuName = "Game/Mask Unlock/Key Press")]
public class KeyPressUnlockAction : MaskUnlockAction
{
    public KeyCode requiredKey = KeyCode.LeftArrow;

    public override string GetHint()
    {
        return requiredKey.ToString();
    }

    public override void OnInteract(MinigameContext ctx, InteractionEvent evt)
    {
        if (ctx == null)
            return;

        if (evt.type != InteractionType.KeyDown)
            return;

        if (evt.key != requiredKey)
            return;

        ctx.PopTopMask();
    }
}
