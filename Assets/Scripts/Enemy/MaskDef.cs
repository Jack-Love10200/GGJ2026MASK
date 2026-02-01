using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/Mask Def")]
public class MaskDef : ScriptableObject
{
    public string debugName;
    [FormerlySerializedAs("icon")]//I already made all the defs but wanted to change the name so...
    public Sprite maskSprite;
    public Sprite indicatorSprite;
    public MaskUnlockAction unlockAction;

    public virtual MaskUnlockAction GetUnlockAction()
    {
        return unlockAction;
    }
}
