using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyMaskStackVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform faceSocket;
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Header("Initial Masks (Bottom -> Top)")]
    [SerializeField] private List<MaskDef> initialMasksBottomToTop = new List<MaskDef>();

    [Header("Sorting")]
    [SerializeField] private int baseSortingOrder = 5;
    [SerializeField] private string sortingLayerName = "";

    [Header("Jitter")]
    [SerializeField] private Vector2 positionJitter = new Vector2(0.02f, 0.02f);
    [SerializeField] private float rotationJitterDeg = 2f;
    [SerializeField] private float scaleJitter = 0.03f;
    [SerializeField] private float perLayerZOffset = 0f;
    [SerializeField] private bool deterministicJitter = true;
    [Tooltip("0 = use instance ID.")]
    [SerializeField] private int seedOverride = 0;
    [SerializeField] private bool debugInteractions = false;

    private readonly List<MaskLayer> layers = new List<MaskLayer>();
    private ComboManager comboManager;
    private NavMeshAgent cachedAgent;

    private struct MaskLayer
    {
        public MaskDef def;
        public GameObject root;
        public SpriteRenderer renderer;
        public Transform anchor;
        public int layerIndex;
    }

    public bool HasMask => layers.Count > 0;

    public MaskDef PeekTopMask()
    {
        if (layers.Count == 0)
            return null;

        return layers[layers.Count - 1].def;
    }

    public Transform GetTopMinigameAnchor()
    {
        if (layers.Count == 0)
            return null;

        return layers[layers.Count - 1].anchor;
    }

    public bool TryHandleInteraction(InteractionEvent evt, PlayerInteractor caller)
    {
        if (layers.Count == 0)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(EnemyMaskStackVisual)}: No layers to interact.", this);
            return false;
        }

        var top = layers[layers.Count - 1];
        if (top.def == null)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(EnemyMaskStackVisual)}: Top layer def is null.", this);
            return false;
        }

        var unlockAction = top.def.GetUnlockAction();
        if (unlockAction == null)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(EnemyMaskStackVisual)}: Unlock action missing on {top.def.name}.", this);
            return false;
        }

        if (debugInteractions)
            Debug.Log($"{nameof(EnemyMaskStackVisual)}: Interaction {evt.type} -> {unlockAction.name}", this);
        var ctx = new MinigameContext(this, top.anchor, caller, cachedAgent);
        unlockAction.OnInteract(ctx, evt);
        return true;
    }

    public bool PopTopMask()
    {
        if (layers.Count == 0)
            return false;

        RemoveTopLayer();

        // Track killing
        comboManager?.TrackEnemyFinished(1);
        LevelScopeManagers.Instance.GetComponent<ScoreManager>().TrackKill();

        if (layers.Count == 0)
            HandleAllMasksRemoved();
        return true;
    }

    private void HandleAllMasksRemoved()
    {
        // TODO: play VFX/SFX before destroying the enemy.
        Destroy(gameObject);
    }

    public void ResetToInitial()
    {
        ClearRuntimeLayers();
        BuildFromInitial();
    }

    public void AddMask(MaskDef def)
    {
        if (def == null)
            return;

        if (!EnsureFaceSocket())
            return;

        int baseSeed = GetBaseSeed();
        int index = layers.Count == 0 ? 0 : layers[layers.Count - 1].layerIndex + 1;
        TryCreateLayer(def, index, baseSeed);
    }

    private void Awake()
    {
        if (!EnsureFaceSocket())
        {
            Debug.LogError($"{nameof(EnemyMaskStackVisual)}: Face Socket is not set.", this);
            enabled = false;
            return;
        }

        if (LevelScopeManagers.Instance != null)
            comboManager = LevelScopeManagers.Instance.GetComponent<ComboManager>();

        cachedAgent = GetComponentInParent<NavMeshAgent>();

        BuildFromInitial();
    }

    private void OnDisable()
    {
        var manager = MinigameManager.Instance;
        if (manager != null && manager.ActiveEnemy == this)
            manager.CancelActive();
    }

    private void BuildFromInitial()
    {
        if (!EnsureFaceSocket())
            return;

        int baseSeed = GetBaseSeed();
        layers.Clear();

        if (initialMasksBottomToTop == null || initialMasksBottomToTop.Count == 0)
            return;

        for (int i = 0; i < initialMasksBottomToTop.Count; i++)
        {
            var def = initialMasksBottomToTop[i];
            if (def == null)
                continue;

            TryCreateLayer(def, i, baseSeed);
        }
    }

    private bool TryCreateLayer(MaskDef def, int index, int baseSeed)
    {
        if (def.icon == null)
        {
            Debug.LogWarning($"{nameof(EnemyMaskStackVisual)}: Mask icon is null at index {index}.", this);
            return false;
        }

        var root = new GameObject($"MaskLayer_{index}");
        root.transform.SetParent(faceSocket, false);

        var spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = def.icon;

        if (bodyRenderer != null)
        {
            spriteRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        }
        else if (!string.IsNullOrWhiteSpace(sortingLayerName))
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
        }

        spriteRenderer.sortingOrder = baseSortingOrder + index;

        ApplyJitter(root.transform, index, baseSeed);

        var anchor = new GameObject("MinigameAnchor").transform;
        anchor.SetParent(root.transform, false);

        layers.Add(new MaskLayer
        {
            def = def,
            root = root,
            renderer = spriteRenderer,
            anchor = anchor,
            layerIndex = index
        });

        return true;
    }

    private void ApplyJitter(Transform target, int index, int baseSeed)
    {
        float dx;
        float dy;
        float rot;
        float scale;

        if (deterministicJitter)
        {
            int layerSeed = CombineSeed(baseSeed, index);
            var rng = new System.Random(layerSeed);
            dx = Range(rng, -positionJitter.x, positionJitter.x);
            dy = Range(rng, -positionJitter.y, positionJitter.y);
            rot = Range(rng, -rotationJitterDeg, rotationJitterDeg);
            scale = Range(rng, 1f - scaleJitter, 1f + scaleJitter);
        }
        else
        {
            dx = UnityEngine.Random.Range(-positionJitter.x, positionJitter.x);
            dy = UnityEngine.Random.Range(-positionJitter.y, positionJitter.y);
            rot = UnityEngine.Random.Range(-rotationJitterDeg, rotationJitterDeg);
            scale = UnityEngine.Random.Range(1f - scaleJitter, 1f + scaleJitter);
        }

        target.localPosition = new Vector3(dx, dy, index * perLayerZOffset);
        target.localRotation = Quaternion.Euler(0f, 0f, rot);
        target.localScale = new Vector3(scale, scale, 1f);
    }

    private void RemoveTopLayer()
    {
        int index = layers.Count - 1;
        var layer = layers[index];
        layers.RemoveAt(index);

        if (layer.root == null)
            return;

        if (Application.isPlaying)
            Destroy(layer.root);
        else
            DestroyImmediate(layer.root);
    }

    private void ClearRuntimeLayers()
    {
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            if (layer.root == null)
                continue;

            if (Application.isPlaying)
                Destroy(layer.root);
            else
                DestroyImmediate(layer.root);
        }

        layers.Clear();
    }

    private bool EnsureFaceSocket()
    {
        return faceSocket != null;
    }

    private int GetBaseSeed()
    {
        return seedOverride != 0 ? seedOverride : GetInstanceID();
    }

    private static float Range(System.Random rng, float min, float max)
    {
        return (float)(rng.NextDouble() * (max - min) + min);
    }

    private static int CombineSeed(int baseSeed, int index)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + baseSeed;
            hash = hash * 31 + index;
            return hash;
        }
    }

    private void OnValidate()
    {
        if (faceSocket == null)
            Debug.LogWarning($"{nameof(EnemyMaskStackVisual)}: Face Socket is not set.", this);

        if (initialMasksBottomToTop != null)
        {
            for (int i = 0; i < initialMasksBottomToTop.Count; i++)
            {
                if (initialMasksBottomToTop[i] == null)
                {
                    Debug.LogWarning($"{nameof(EnemyMaskStackVisual)}: Initial mask at index {i} is null.", this);
                    break;
                }
            }
        }

        if (bodyRenderer != null && baseSortingOrder <= bodyRenderer.sortingOrder)
        {
            Debug.LogWarning(
                $"{nameof(EnemyMaskStackVisual)}: baseSortingOrder ({baseSortingOrder}) should be higher than Body ({bodyRenderer.sortingOrder}).",
                this);
        }
    }
}
