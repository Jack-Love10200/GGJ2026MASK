using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum MinigameResult
{
    Win,
    Lose,
    Draw
}

public class MinigameManager : MonoBehaviour
{
    [System.Serializable]
    private struct MinigameEntry
    {
        public string id;
        public GameObject prefab;
    }

    [SerializeField] private List<MinigameEntry> minigames = new List<MinigameEntry>();

    [Header("Camera Focus")]
    [SerializeField] private bool focusCameraOnMinigame = true;
    [SerializeField] private Camera focusCamera;
    [FormerlySerializedAs("focusWorldOffset")]
    [FormerlySerializedAs("focusLocalOffset")]
    [SerializeField] private Vector3 focusLocalOffset = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private float focusFov = 25f;
    [SerializeField] private bool smoothCameraFocus = true;
    [Tooltip("Seconds to smooth camera focus. 0 = snap.")]
    [SerializeField] private float focusSmoothTime = 0.25f;
    [Tooltip("Freeze anchor rotation at minigame start to avoid camera/anchor feedback loops.")]
    [SerializeField] private bool lockFocusRotationOnStart = true;
    [SerializeField] private bool debugMinigameFlow = false;
    [SerializeField] private bool showCursorDuringMinigame = true;
    [SerializeField] private bool debugDrawMinigameInput = false;
    [SerializeField] private float debugDrawRayLength = 5f;
    [SerializeField] private float debugDrawPlaneSize = 0.5f;
    [SerializeField] private float debugDrawPointRadius = 0.03f;

    [Header("SFX")]
    [SerializeField] private bool playResultSfx = true;
    [SerializeField] private AudioClip winResultSfx;
    [SerializeField] private AudioClip loseResultSfx;
    [SerializeField] private AudioClip drawResultSfx;

    public static MinigameManager Instance { get; private set; }

    public bool HasActiveMinigame => activeInstance != null;
    public EnemyMaskStackVisual ActiveEnemy => activeContext != null ? activeContext.enemy : null;

    private readonly Dictionary<string, GameObject> minigameLookup = new Dictionary<string, GameObject>();
    private IMinigame activeMinigame;
    private MinigameContext activeContext;
    private GameObject activeInstance;
    private bool stoppedAgent;
    private bool hasCameraFocus;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private float originalCameraFov;
    private Vector3 focusPositionVelocity;
    private float focusFovVelocity;
    private bool hasFocusRotation;
    private Quaternion focusRotation;
    private bool hasCursorOverride;
    private bool originalCursorVisible;
    private CursorLockMode originalCursorLockMode;
    private bool hasDebugPoint;
    private Vector3 debugRayOrigin;
    private Vector3 debugRayDir;
    private Vector3 debugPlaneOrigin;
    private Vector3 debugPlaneRight;
    private Vector3 debugPlaneUp;
    private Vector3 debugPoint;

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
        {
            if (debugMinigameFlow)
                Debug.Log($"{nameof(MinigameManager)}: StartMinigame ignored (already active).", this);
            return;
        }

        if (ctx == null || ctx.anchor == null)
        {
            if (debugMinigameFlow)
                Debug.Log($"{nameof(MinigameManager)}: StartMinigame missing ctx/anchor.", this);
            return;
        }

        if (!minigameLookup.TryGetValue(id, out var prefab) || prefab == null)
        {
            if (debugMinigameFlow)
                Debug.Log($"{nameof(MinigameManager)}: StartMinigame id not found: {id}", this);
            Debug.LogWarning($"{nameof(MinigameManager)}: Minigame id not found: {id}", this);
            return;
        }

        if (debugMinigameFlow)
            Debug.Log($"{nameof(MinigameManager)}: StartMinigame instantiate {id}.", this);
        Transform anchorToUse = ctx.anchor;
        activeInstance = Instantiate(prefab, anchorToUse, false);
        activeMinigame = activeInstance.GetComponent<IMinigame>();
        if (activeMinigame == null)
            activeMinigame = activeInstance.GetComponentInChildren<IMinigame>();

        if (activeMinigame == null)
        {
            if (debugMinigameFlow)
                Debug.Log($"{nameof(MinigameManager)}: Minigame prefab missing IMinigame: {id}", this);
            Debug.LogWarning($"{nameof(MinigameManager)}: Minigame prefab missing IMinigame: {id}", this);
            Destroy(activeInstance);
            activeInstance = null;
            return;
        }

        activeContext = ctx;
        activeContext.anchor = anchorToUse;

        if (lockFocusRotationOnStart && anchorToUse != null)
        {
            hasFocusRotation = true;
            focusRotation = anchorToUse.rotation;
        }
        else
        {
            hasFocusRotation = false;
        }

        if (ctx.agentToStop != null)
        {
            stoppedAgent = true;
            ctx.agentToStop.isStopped = true;
        }

        SetPlayerMinigamePaused(activeContext, true);
        SetHandsMinigameMode(activeContext, true);
        BeginCursorOverride();
        BeginCameraFocus(anchorToUse);
        activeMinigame.Begin(activeContext);
    }

    public void EndMinigame(bool success)
    {
        EndMinigame(success ? MinigameResult.Win : MinigameResult.Lose);
    }

    public void EndMinigame(MinigameResult result)
    {
        if (activeMinigame == null)
            return;

        bool success = result == MinigameResult.Win;

        if (playResultSfx)
            PlayResultSfx(result);

        if (!success && activeContext != null && activeContext.player != null && activeContext.enemy != null)
            activeContext.player.BlockMinigameUntilExit(activeContext.enemy);

        if (success)
            activeContext?.onSuccess?.Invoke();
        else
            activeContext?.onFail?.Invoke();

        CleanupActive();
    }

    private void PlayResultSfx(MinigameResult result)
    {
        AudioClip clip = result switch
        {
            MinigameResult.Win => winResultSfx,
            MinigameResult.Draw => drawResultSfx,
            _ => loseResultSfx
        };

        if (clip == null)
            return;

        var audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlaySfx(clip, 1f);
            return;
        }
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

    public bool TryGetPointerWorldPoint(Ray ray, out Vector3 worldPoint)
    {
        worldPoint = default;
        if (activeContext == null || activeContext.anchor == null)
            return false;

        var plane = new Plane(activeContext.anchor.forward, activeContext.anchor.position);
        if (!plane.Raycast(ray, out float distance))
        {
            if (debugDrawMinigameInput)
            {
                debugRayOrigin = ray.origin;
                debugRayDir = ray.direction;
                hasDebugPoint = false;
            }
            return false;
        }

        worldPoint = ray.GetPoint(distance);
        if (debugDrawMinigameInput)
        {
            debugRayOrigin = ray.origin;
            debugRayDir = ray.direction;
            debugPlaneOrigin = activeContext.anchor.position;
            debugPlaneRight = activeContext.anchor.right;
            debugPlaneUp = activeContext.anchor.up;
            debugPoint = worldPoint;
            hasDebugPoint = true;
        }
        return true;
    }

    private void CleanupActive()
    {
        if (stoppedAgent && activeContext != null && activeContext.agentToStop != null)
            activeContext.agentToStop.isStopped = false;

        SetHandsMinigameMode(activeContext, false);
        SetPlayerMinigamePaused(activeContext, false);
        RestoreCursorOverride();
        RestoreCameraFocus();

        stoppedAgent = false;
        activeMinigame = null;
        activeContext = null;
        hasFocusRotation = false;

        if (activeInstance != null)
            Destroy(activeInstance);

        activeInstance = null;
    }

    private void SetHandsMinigameMode(MinigameContext ctx, bool active)
    {
        if (ctx == null || ctx.player == null)
            return;

        var hands = ctx.player.GetComponentInChildren<Hands>(true);
        if (hands == null)
            return;

        if (active)
        {
            Transform socket = ctx.enemy != null ? ctx.enemy.GetLeftShoulderSocket() : null;
            hands.EnterMinigameMode(socket);
        }
        else
        {
            hands.ExitMinigameMode();
        }
    }

    private void SetPlayerMinigamePaused(MinigameContext ctx, bool paused)
    {
        if (ctx == null || ctx.player == null)
            return;

        var playerMover = ctx.player.GetComponent<Player>();
        if (playerMover == null)
            return;

        playerMover.SetMinigamePaused(paused);
    }

    private void BeginCursorOverride()
    {
        if (!showCursorDuringMinigame)
            return;

        if (!hasCursorOverride)
        {
            originalCursorVisible = Cursor.visible;
            originalCursorLockMode = Cursor.lockState;
            hasCursorOverride = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreCursorOverride()
    {
        if (!hasCursorOverride)
            return;

        Cursor.lockState = originalCursorLockMode;
        Cursor.visible = originalCursorVisible;
        hasCursorOverride = false;
    }

    private void BeginCameraFocus(Transform anchor)
    {
        if (!focusCameraOnMinigame || anchor == null)
            return;

        if (focusCamera == null)
            focusCamera = Camera.main;

        if (focusCamera == null)
            return;

        if (!hasCameraFocus)
        {
            originalCameraPosition = focusCamera.transform.position;
            originalCameraRotation = focusCamera.transform.rotation;
            originalCameraFov = focusCamera.fieldOfView;
            focusPositionVelocity = Vector3.zero;
            focusFovVelocity = 0f;
            hasCameraFocus = true;
        }

        ApplyCameraFocus(anchor);
    }

    private void RestoreCameraFocus()
    {
        if (!hasCameraFocus)
            return;

        if (focusCamera == null)
            focusCamera = Camera.main;

        if (focusCamera == null)
        {
            hasCameraFocus = false;
            return;
        }

        focusCamera.transform.position = originalCameraPosition;
        focusCamera.transform.rotation = originalCameraRotation;
        focusCamera.fieldOfView = originalCameraFov;
        hasCameraFocus = false;
    }

    private void LateUpdate()
    {
        if (activeInstance == null && (activeMinigame != null || activeContext != null || hasCameraFocus || hasCursorOverride))
        {
            if (debugMinigameFlow)
                Debug.Log($"{nameof(MinigameManager)}: Active instance missing, cleaning up.", this);
            CleanupActive();
            return;
        }

        if (!hasCameraFocus || !focusCameraOnMinigame || activeContext == null || activeContext.anchor == null)
            return;

        ApplyCameraFocus(activeContext.anchor);
    }

    private void ApplyCameraFocus(Transform anchor)
    {
        Quaternion basisRotation = hasFocusRotation ? focusRotation : anchor.rotation;
        Vector3 targetPos = anchor.position + (basisRotation * focusLocalOffset);
        Quaternion targetRot = Quaternion.LookRotation(anchor.position - targetPos, Vector3.up);

        if (smoothCameraFocus && focusSmoothTime > 0f)
        {
            focusCamera.transform.position = Vector3.SmoothDamp(
                focusCamera.transform.position,
                targetPos,
                ref focusPositionVelocity,
                focusSmoothTime);

            float t = 1f - Mathf.Exp(-Time.deltaTime / focusSmoothTime);
            focusCamera.transform.rotation = Quaternion.Slerp(focusCamera.transform.rotation, targetRot, t);

            if (focusFov > 0f)
            {
                focusCamera.fieldOfView = Mathf.SmoothDamp(
                    focusCamera.fieldOfView,
                    focusFov,
                    ref focusFovVelocity,
                    focusSmoothTime);
            }

            return;
        }

        focusCamera.transform.position = targetPos;
        focusCamera.transform.rotation = targetRot;

        if (focusFov > 0f)
            focusCamera.fieldOfView = focusFov;
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

    private void OnDrawGizmos()
    {
        if (!debugDrawMinigameInput)
            return;

        if (debugRayDir.sqrMagnitude > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(debugRayOrigin, debugRayDir * debugDrawRayLength);
        }

        Gizmos.color = Color.yellow;
        if (debugPlaneRight.sqrMagnitude > 0f && debugPlaneUp.sqrMagnitude > 0f)
        {
            Gizmos.DrawLine(debugPlaneOrigin - debugPlaneRight * debugDrawPlaneSize, debugPlaneOrigin + debugPlaneRight * debugDrawPlaneSize);
            Gizmos.DrawLine(debugPlaneOrigin - debugPlaneUp * debugDrawPlaneSize, debugPlaneOrigin + debugPlaneUp * debugDrawPlaneSize);
        }

        if (hasDebugPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(debugPoint, debugDrawPointRadius);
        }
    }
}
