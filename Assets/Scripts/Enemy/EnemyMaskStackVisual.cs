using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyMaskStackVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform faceSocket;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Transform indicatorSocket;
    [SerializeField] private Transform leftShoulderSocket; 
    [SerializeField] private GameObject MaskPrefab;
    [SerializeField] private Material arrowMaterial;


    [Header("Initial Masks (Bottom -> Top)")]
    [SerializeField] private List<MaskDef> initialMasksBottomToTop = new List<MaskDef>();

    [Header("Sorting")]
    [SerializeField] private int baseSortingOrder = 5;
    [SerializeField] private string sortingLayerName = "";
    [SerializeField] private int indicatorSortingOffset = 10;

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
    private SpriteRenderer indicatorRenderer;

    static bool useNonPrefabIndicator = false;

    private struct MaskLayer
    {
        public MaskDef def;
        public GameObject root;
        public SpriteRenderer renderer;
        public Transform anchor;
        public int layerIndex;
    }

    public struct MaskVisualData
    {
        public Sprite sprite;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 lossyScale;
        public int sortingLayerID;
        public int sortingOrder;
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

    public Transform GetLeftShoulderSocket()
    {
        if (leftShoulderSocket == null)
            leftShoulderSocket = FindSocketByName("LeftShoulderSocket");

        return leftShoulderSocket;
    }

    public bool TryGetTopMaskVisual(out MaskVisualData data)
    {
        data = default;
        if (layers.Count == 0)
            return false;

        var top = layers[layers.Count - 1];
        var renderer = top.renderer;
        var sprite = renderer != null ? renderer.sprite : top.def != null ? top.def.maskSprite : null;
        if (sprite == null)
            return false;

        var root = top.root != null ? top.root.transform : faceSocket;
        if (root == null)
            root = transform;

        data.sprite = sprite;
        data.position = root.position;
        data.rotation = root.rotation;
        data.lossyScale = root.lossyScale;

        if (renderer != null)
        {
            data.sortingLayerID = renderer.sortingLayerID;
            data.sortingOrder = renderer.sortingOrder;
        }
        else if (bodyRenderer != null)
        {
            data.sortingLayerID = bodyRenderer.sortingLayerID;
            data.sortingOrder = bodyRenderer.sortingOrder + 1;
        }

        return true;
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
        RefreshIndicator();

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
        RefreshIndicator();
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
        RefreshIndicator();
    }

    private void Awake()
    {
        if (!EnsureFaceSocket())
        {
            Debug.LogError($"{nameof(EnemyMaskStackVisual)}: Face Socket is not set.", this);
            enabled = false;
            return;
        }

        EnsureIndicator();

        if (LevelScopeManagers.Instance != null)
            comboManager = LevelScopeManagers.Instance.GetComponent<ComboManager>();

        cachedAgent = GetComponentInParent<NavMeshAgent>();

        BuildFromInitial();
        RefreshIndicator();
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

        RefreshIndicator();
    }

    private bool TryCreateLayer(MaskDef def, int index, int baseSeed)
    {
        if (def.maskSprite == null)
        {
            Debug.LogWarning($"{nameof(EnemyMaskStackVisual)}: Mask icon is null at index {index}.", this);
            return false;
        }


        var root = GameObject.Instantiate(MaskPrefab);
        root.name = $"MaskLayer_{index}";
        root.transform.SetParent(faceSocket, false);

        SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();

        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(root.transform);
        arrow.transform.localPosition = new Vector3(0, 0, 0.1f);

        SpriteRenderer arrowRenderer = arrow.AddComponent<SpriteRenderer>();
        arrowRenderer.sprite = def.indicatorSprite;
        arrowRenderer.material = arrowMaterial;

        arrowRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        //if (bodyRenderer != null)
        //{
        //    spriteRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        //}
        //else if (!string.IsNullOrWhiteSpace(sortingLayerName))
        //{
        //    spriteRenderer.sortingLayerName = sortingLayerName;
        //}

        spriteRenderer.sortingOrder = baseSortingOrder + index;

        ApplyJitter(root.transform, index, baseSeed);

        var anchor = new GameObject("MinigameAnchor").transform;
        anchor.SetParent(root.transform, false);
        var parentScale = root.transform.lossyScale;
        anchor.localScale = new Vector3(
            parentScale.x < 0f ? -1f : 1f,
            parentScale.y < 0f ? -1f : 1f,
            parentScale.z < 0f ? -1f : 1f);

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
        RefreshIndicator();
    }

    public void ClearRuntimeLayersNoDestroy()
    {
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            if (layer.root == null)
                continue;

            //if (Application.isPlaying)
            //    Destroy(layer.root);
            //else
            //    DestroyImmediate(layer.root);
        }

        layers.Clear();
        RefreshIndicator();
    }

    private bool EnsureFaceSocket()
    {
        return faceSocket != null;
    }

    private void RefreshIndicator()
    {
        if (!EnsureIndicator())
            return;

        // Use only indicators on prefab normally, but allow for old sprite renderer indicator
        if (useNonPrefabIndicator == false)
        {
            indicatorRenderer.enabled = false;
            return;
        }
        //else
        //{
        //    indicatorRenderer.enabled = true;
        //}

        if (layers.Count == 0)
        {
            indicatorRenderer.enabled = false;
            return;
        }

        var top = layers[layers.Count - 1];
        if (top.def == null)
        {
            indicatorRenderer.enabled = false;
            return;
        }

        var sprite = top.def.indicatorSprite;
        if (sprite == null)
        {
            indicatorRenderer.enabled = false;
            return;
        }

        indicatorRenderer.sprite = sprite;
        // Use only indicators on prefab normally, but allow for old sprite renderer indicator
        if (useNonPrefabIndicator == true)
        {
            indicatorRenderer.enabled = false;

        }
        else
        {
            indicatorRenderer.enabled = true;
        }
        ApplyIndicatorSorting();
    }

    private bool EnsureIndicator()
    {
        if (indicatorSocket == null)
            indicatorSocket = FindSocketByName("ArrowIndicatorSocket");

        if (indicatorSocket == null)
            return false;

        if (indicatorRenderer == null)
        {
            indicatorRenderer = indicatorSocket.GetComponent<SpriteRenderer>();
            if (indicatorRenderer == null)
                indicatorRenderer = indicatorSocket.gameObject.AddComponent<SpriteRenderer>();
        }

        return indicatorRenderer != null;
    }

    private Transform FindSocketByName(string socketName)
    {
        if (string.IsNullOrWhiteSpace(socketName))
            return null;

        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == socketName)
                return transforms[i];
        }

        return null;
    }

    private void ApplyIndicatorSorting()
    {
        if (indicatorRenderer == null)
            return;

        if (bodyRenderer != null)
        {
            indicatorRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        }
        else if (!string.IsNullOrWhiteSpace(sortingLayerName))
        {
            indicatorRenderer.sortingLayerName = sortingLayerName;
        }

        int order = baseSortingOrder + layers.Count + indicatorSortingOffset;
        indicatorRenderer.sortingOrder = order;
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
