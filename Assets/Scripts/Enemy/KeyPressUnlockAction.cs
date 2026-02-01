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

        if (ctx.enemy != null && ctx.player != null)
        {
            var hands = ctx.player.GetComponentInChildren<Hands>();
            if (hands != null && ctx.enemy.TryGetTopMaskVisual(out var visual))
                hands.PlayMaskGrab(visual, ctx.enemy.transform);
        }

        ctx.PopTopMask();
    }
}
