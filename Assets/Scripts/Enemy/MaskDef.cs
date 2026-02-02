using System.Collections.Generic;
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
    [Header("Pop SFX")]
    [SerializeField] private List<AudioClip> popSfx = new List<AudioClip>();
    [Range(0f, 1f)]
    [SerializeField] private float popSfxVolume = 1f;

    public virtual MaskUnlockAction GetUnlockAction()
    {
        return unlockAction;
    }

    public bool TryGetRandomPopSfx(out AudioClip clip, out float volume)
    {
        clip = null;
        volume = popSfxVolume;

        if (popSfx == null || popSfx.Count == 0)
            return false;

        int start = Random.Range(0, popSfx.Count);
        for (int i = 0; i < popSfx.Count; i++)
        {
            var candidate = popSfx[(start + i) % popSfx.Count];
            if (candidate == null)
                continue;

            clip = candidate;
            return true;
        }

        return false;
    }
}
