using UnityEngine;

public class ReactionSpotMinigame : MonoBehaviour, IMinigame
{
    [Header("Gameplay")]
    [SerializeField] private float totalDuration = 4f;
    [SerializeField] private float spotLifetime = 1.1f;
    [SerializeField] private int requiredHits = 2;
    [SerializeField] private float delayBetweenSpots = 0.05f;
    [SerializeField] private bool failOnMiss = true;
    [SerializeField] private bool failOnTimeout = true;

    [Header("Spawn Area")]
    [SerializeField] private Transform inputAreaTransform;
    [SerializeField] private Vector2 fallbackAreaSize = new Vector2(0.45f, 0.45f);
    [SerializeField] private Vector2 fallbackAreaCenter = Vector2.zero;
    [SerializeField] private float areaPadding = 0.02f;

    [Header("Visuals")]
    [SerializeField] private float spotRadius = 0.05f;
    [SerializeField] private float ringRadiusMultiplier = 2f;
    [SerializeField] private float ringStartScale = 1.2f;
    [SerializeField] private float ringEndScale = 0.2f;
    [Range(0.05f, 0.95f)]
    [SerializeField] private float ringInnerRatio = 0.72f;
    [SerializeField] private Color spotColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private int sortingOrderOffset = 6;
    [SerializeField] private float visualZOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugDrawArea = false;

    private MinigameContext context;
    private bool isRunning;
    private int hits;
    private float remainingDuration;
    private float spotElapsed;
    private float spawnDelayTimer;
    private bool hasSpot;
    private Vector3 currentSpotLocal;
    private GameObject spotRoot;
    private SpriteRenderer spotRenderer;
    private SpriteRenderer ringRenderer;
    private Sprite spotSprite;
    private Sprite ringSprite;
    private Texture2D spotTexture;
    private Texture2D ringTexture;
    private bool hasSorting;
    private int sortingLayerId;
    private int sortingOrder;

    public void Begin(MinigameContext ctx)
    {
        context = ctx;
        hits = 0;
        remainingDuration = totalDuration;
        spotElapsed = 0f;
        spawnDelayTimer = 0f;
        isRunning = true;
        hasSpot = false;

        ResolveSorting();
        EnsureSprites();

        if (requiredHits <= 0)
        {
            EndMinigame(true);
            return;
        }

        SpawnSpot();
    }

    public void HandlePointerDown(Vector3 localPos)
    {
        if (!isRunning || !hasSpot)
            return;

        Vector3 minigameLocal = ToMinigameLocal(localPos);
        Vector2 delta = new Vector2(minigameLocal.x - currentSpotLocal.x, minigameLocal.y - currentSpotLocal.y);
        if (delta.sqrMagnitude <= spotRadius * spotRadius)
        {
            RegisterHit();
            return;
        }

        if (failOnMiss)
            EndMinigame(false);
    }

    public void HandlePointerDrag(Vector3 localPos)
    {
    }

    public void HandlePointerUp(Vector3 localPos)
    {
    }

    public void Cancel()
    {
        isRunning = false;
        CleanupSpot();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        if (totalDuration > 0f)
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
            {
                EndMinigame(false);
                return;
            }
        }

        if (!hasSpot)
        {
            if (spawnDelayTimer > 0f)
            {
                spawnDelayTimer -= Time.deltaTime;
                if (spawnDelayTimer <= 0f)
                    SpawnSpot();
            }
            return;
        }

        spotElapsed += Time.deltaTime;
        UpdateRingVisual();

        if (spotLifetime > 0f && spotElapsed >= spotLifetime)
        {
            if (failOnTimeout)
            {
                EndMinigame(false);
                return;
            }

            CleanupSpot();
            QueueNextSpot();
        }
    }

    private void RegisterHit()
    {
        hits++;
        CleanupSpot();

        if (hits >= requiredHits)
        {
            EndMinigame(true);
            return;
        }

        QueueNextSpot();
    }

    private void QueueNextSpot()
    {
        if (delayBetweenSpots <= 0f)
        {
            SpawnSpot();
            return;
        }

        spawnDelayTimer = delayBetweenSpots;
        hasSpot = false;
    }

    private void SpawnSpot()
    {
        if (!TryGetInputArea(out float width, out float height, out Vector2 center))
        {
            EndMinigame(false);
            return;
        }

        float halfW = Mathf.Max(0f, width * 0.5f - areaPadding - spotRadius);
        float halfH = Mathf.Max(0f, height * 0.5f - areaPadding - spotRadius);

        float x = center.x + Random.Range(-halfW, halfW);
        float y = center.y + Random.Range(-halfH, halfH);

        currentSpotLocal = new Vector3(x, y, visualZOffset);
        BuildSpotVisual(currentSpotLocal);
        spotElapsed = 0f;
        hasSpot = true;
        UpdateRingVisual();
    }

    private void BuildSpotVisual(Vector3 localPos)
    {
        CleanupSpot();

        spotRoot = new GameObject("ReactionSpot");
        spotRoot.transform.SetParent(transform, false);
        spotRoot.transform.localPosition = localPos;
        spotRoot.transform.localRotation = Quaternion.identity;
        spotRoot.transform.localScale = Vector3.one;

        var core = new GameObject("Core");
        core.transform.SetParent(spotRoot.transform, false);
        spotRenderer = core.AddComponent<SpriteRenderer>();
        spotRenderer.sprite = spotSprite;
        spotRenderer.color = spotColor;
        ApplySorting(spotRenderer, 1);
        float coreScale = Mathf.Max(0.0001f, spotRadius * 2f);
        core.transform.localScale = new Vector3(coreScale, coreScale, 1f);

        var ring = new GameObject("Ring");
        ring.transform.SetParent(spotRoot.transform, false);
        ringRenderer = ring.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = ringSprite;
        ringRenderer.color = ringColor;
        ApplySorting(ringRenderer, 0);
    }

    private void UpdateRingVisual()
    {
        if (ringRenderer == null)
            return;

        float t = spotLifetime > 0f ? Mathf.Clamp01(spotElapsed / spotLifetime) : 0f;
        float scale = Mathf.Lerp(ringStartScale, ringEndScale, t);
        float baseScale = Mathf.Max(0.0001f, spotRadius * 2f * ringRadiusMultiplier);
        ringRenderer.transform.localScale = new Vector3(baseScale * scale, baseScale * scale, 1f);

        var color = ringColor;
        color.a = Mathf.Lerp(ringColor.a, 0f, t);
        ringRenderer.color = color;
    }

    private void CleanupSpot()
    {
        if (spotRoot != null)
            Destroy(spotRoot);

        spotRoot = null;
        spotRenderer = null;
        ringRenderer = null;
        hasSpot = false;
    }

    private void EndMinigame(bool success)
    {
        if (!isRunning)
            return;

        isRunning = false;
        CleanupSpot();
        MinigameManager.Instance?.EndMinigame(success);
    }

    private Vector3 ToMinigameLocal(Vector3 anchorLocal)
    {
        if (transform.parent == null)
            return anchorLocal;

        Vector3 world = transform.parent.TransformPoint(anchorLocal);
        return transform.InverseTransformPoint(world);
    }

    private void ResolveSorting()
    {
        hasSorting = false;

        var renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            sortingLayerId = renderer.sortingLayerID;
            sortingOrder = renderer.sortingOrder + sortingOrderOffset;
            hasSorting = true;
            return;
        }

        if (context != null && context.enemy != null && context.enemy.TryGetTopMaskVisual(out var data))
        {
            sortingLayerId = data.sortingLayerID;
            sortingOrder = data.sortingOrder + sortingOrderOffset;
            hasSorting = true;
        }
    }

    private void ApplySorting(Renderer renderer, int extraOrder)
    {
        if (!hasSorting || renderer == null)
            return;

        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder + extraOrder;
    }

    private void EnsureSprites()
    {
        if (spotSprite == null)
        {
            spotTexture = CreateCircleTexture(64, 0f);
            spotSprite = Sprite.Create(spotTexture, new Rect(0, 0, spotTexture.width, spotTexture.height), new Vector2(0.5f, 0.5f), spotTexture.width);
        }

        if (ringSprite == null)
        {
            ringTexture = CreateCircleTexture(64, ringInnerRatio);
            ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, ringTexture.width, ringTexture.height), new Vector2(0.5f, 0.5f), ringTexture.width);
        }
    }

    private static Texture2D CreateCircleTexture(int size, float innerRatio)
    {
        int clampedSize = Mathf.Max(8, size);
        var tex = new Texture2D(clampedSize, clampedSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        float radius = clampedSize * 0.5f;
        float innerRadius = Mathf.Clamp01(innerRatio) * radius;
        float outerSq = radius * radius;
        float innerSq = innerRadius * innerRadius;
        var clear = new Color32(0, 0, 0, 0);
        var white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < clampedSize; y++)
        {
            float dy = y - radius + 0.5f;
            for (int x = 0; x < clampedSize; x++)
            {
                float dx = x - radius + 0.5f;
                float distSq = dx * dx + dy * dy;
                tex.SetPixel(x, y, distSq <= outerSq && distSq >= innerSq ? white : clear);
            }
        }

        tex.Apply();
        return tex;
    }

    private bool TryGetInputArea(out float width, out float height, out Vector2 center)
    {
        if (inputAreaTransform != null && TryGetAreaFromTransform(inputAreaTransform, out width, out height, out center))
            return true;

        if (context != null && context.anchor != null)
        {
            var root = context.anchor.parent;
            var renderer = root != null ? root.GetComponent<SpriteRenderer>() : null;
            if (renderer != null && TryGetAreaFromRenderer(renderer, out width, out height, out center))
                return true;
        }

        width = Mathf.Max(0f, fallbackAreaSize.x);
        height = Mathf.Max(0f, fallbackAreaSize.y);
        center = fallbackAreaCenter;
        return width > 0f && height > 0f;
    }

    private bool TryGetAreaFromTransform(Transform areaTransform, out float width, out float height, out Vector2 center)
    {
        center = Vector2.zero;
        if (areaTransform == null)
        {
            width = 0f;
            height = 0f;
            return false;
        }

        Vector3 centerLocal = transform.InverseTransformPoint(areaTransform.position);
        center = new Vector2(centerLocal.x, centerLocal.y);

        var box = areaTransform.GetComponent<BoxCollider>();
        if (box != null)
        {
            Vector3 half = box.size * 0.5f;
            Vector3 boxCenter = box.center;
            Matrix4x4 localToWorld = box.transform.localToWorldMatrix;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 localCorner = boxCenter + new Vector3(half.x * sx, half.y * sy, half.z * sz);
                        Vector3 worldCorner = localToWorld.MultiplyPoint3x4(localCorner);
                        Vector3 rootLocal = worldToLocal.MultiplyPoint3x4(worldCorner);
                        min = Vector3.Min(min, rootLocal);
                        max = Vector3.Max(max, rootLocal);
                    }
                }
            }

            width = Mathf.Abs(max.x - min.x);
            height = Mathf.Abs(max.y - min.y);
            center = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            return width > 0f && height > 0f;
        }

        var renderer = areaTransform.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            return TryGetAreaFromRenderer(renderer, out width, out height, out center);
        }

        Vector3 localScale = areaTransform.localScale;
        width = Mathf.Abs(localScale.x);
        height = Mathf.Abs(localScale.y);
        return width > 0f && height > 0f;
    }

    private bool TryGetAreaFromRenderer(SpriteRenderer renderer, out float width, out float height, out Vector2 center)
    {
        center = Vector2.zero;
        if (renderer == null)
        {
            width = 0f;
            height = 0f;
            return false;
        }

        Vector3 centerLocal = transform.InverseTransformPoint(renderer.bounds.center);
        center = new Vector2(centerLocal.x, centerLocal.y);
        Vector3 localSize = transform.InverseTransformVector(renderer.bounds.size);
        width = Mathf.Abs(localSize.x);
        height = Mathf.Abs(localSize.y);
        return width > 0f && height > 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawArea)
            return;

        if (!TryGetInputArea(out float width, out float height, out Vector2 center))
            return;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 half = new Vector3(width * 0.5f, height * 0.5f, 0f);
        Vector3 c = new Vector3(center.x, center.y, 0f);
        Gizmos.DrawLine(c + new Vector3(-half.x, -half.y, 0f), c + new Vector3(half.x, -half.y, 0f));
        Gizmos.DrawLine(c + new Vector3(half.x, -half.y, 0f), c + new Vector3(half.x, half.y, 0f));
        Gizmos.DrawLine(c + new Vector3(half.x, half.y, 0f), c + new Vector3(-half.x, half.y, 0f));
        Gizmos.DrawLine(c + new Vector3(-half.x, half.y, 0f), c + new Vector3(-half.x, -half.y, 0f));
    }

    private void OnDestroy()
    {
        if (spotTexture != null)
        {
            if (Application.isPlaying)
                Destroy(spotTexture);
            else
                DestroyImmediate(spotTexture);
        }

        if (ringTexture != null)
        {
            if (Application.isPlaying)
                Destroy(ringTexture);
            else
                DestroyImmediate(ringTexture);
        }
    }
}
