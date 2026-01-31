using System.Collections.Generic;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    [System.Serializable]
    private struct MinigameEntry
    {
        public string id;
        public GameObject prefab;
    }

    [SerializeField] private List<MinigameEntry> minigames = new List<MinigameEntry>();

    public static MinigameManager Instance { get; private set; }

    public bool HasActiveMinigame => activeMinigame != null;
    public EnemyMaskStackVisual ActiveEnemy => activeContext != null ? activeContext.enemy : null;

    private readonly Dictionary<string, GameObject> minigameLookup = new Dictionary<string, GameObject>();
    private IMinigame activeMinigame;
    private MinigameContext activeContext;
    private GameObject activeInstance;
    private bool stoppedAgent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildLookup();
    }

    public void StartMinigame(string id, MinigameContext ctx)
    {
        if (activeMinigame != null)
            return;

        if (ctx == null || ctx.anchor == null)
            return;

        if (!minigameLookup.TryGetValue(id, out var prefab) || prefab == null)
        {
            Debug.LogWarning($"{nameof(MinigameManager)}: Minigame id not found: {id}", this);
            return;
        }

        activeInstance = Instantiate(prefab, ctx.anchor, false);
        activeMinigame = activeInstance.GetComponent<IMinigame>();
        if (activeMinigame == null)
            activeMinigame = activeInstance.GetComponentInChildren<IMinigame>();

        if (activeMinigame == null)
        {
            Debug.LogWarning($"{nameof(MinigameManager)}: Minigame prefab missing IMinigame: {id}", this);
            Destroy(activeInstance);
            activeInstance = null;
            return;
        }

        activeContext = ctx;

        if (ctx.agentToStop != null)
        {
            stoppedAgent = true;
            ctx.agentToStop.isStopped = true;
        }

        activeMinigame.Begin(ctx);
    }

    public void EndMinigame(bool success)
    {
        if (activeMinigame == null)
            return;

        if (success)
            activeContext?.onSuccess?.Invoke();
        else
            activeContext?.onFail?.Invoke();

        CleanupActive();
    }

    public void CancelActive()
    {
        if (activeMinigame == null)
            return;

        activeMinigame.Cancel();
        CleanupActive();
    }

    public void HandlePointerDown(Vector3 worldPoint)
    {
        if (activeMinigame == null || activeContext == null || activeContext.anchor == null)
            return;

        var localPos = activeContext.anchor.InverseTransformPoint(worldPoint);
        activeMinigame.HandlePointerDown(localPos);
    }

    public void HandlePointerDrag(Vector3 worldPoint)
    {
        if (activeMinigame == null || activeContext == null || activeContext.anchor == null)
            return;

        var localPos = activeContext.anchor.InverseTransformPoint(worldPoint);
        activeMinigame.HandlePointerDrag(localPos);
    }

    public void HandlePointerUp(Vector3 worldPoint)
    {
        if (activeMinigame == null || activeContext == null || activeContext.anchor == null)
            return;

        var localPos = activeContext.anchor.InverseTransformPoint(worldPoint);
        activeMinigame.HandlePointerUp(localPos);
    }

    private void CleanupActive()
    {
        if (stoppedAgent && activeContext != null && activeContext.agentToStop != null)
            activeContext.agentToStop.isStopped = false;

        stoppedAgent = false;
        activeMinigame = null;
        activeContext = null;

        if (activeInstance != null)
            Destroy(activeInstance);

        activeInstance = null;
    }

    private void BuildLookup()
    {
        minigameLookup.Clear();

        if (minigames == null)
            return;

        for (int i = 0; i < minigames.Count; i++)
        {
            var entry = minigames[i];
            if (string.IsNullOrWhiteSpace(entry.id) || entry.prefab == null)
                continue;

            minigameLookup[entry.id] = entry.prefab;
        }
    }

    private void OnValidate()
    {
        BuildLookup();
    }
}
