using UnityEngine;

[CreateAssetMenu(menuName = "Game/Mask Def")]
public class MaskDef : ScriptableObject
{
    public string debugName;
    public Sprite icon;
    public MaskUnlockAction unlockAction;

    public virtual MaskUnlockAction GetUnlockAction()
    {
        return unlockAction;
    }
}
