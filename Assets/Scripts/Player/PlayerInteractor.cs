using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask enemyLayer = ~0;
    [SerializeField] private bool requireProximityForClick = true;
    [SerializeField] private BoxCollider targetBox;

    [Header("Input")]
    [SerializeField] private InputActionReference leftAction;
    [SerializeField] private InputActionReference rightAction;
    [SerializeField] private InputActionReference upAction;
    [SerializeField] private InputActionReference downAction;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float maxRaycastDistance = 200f;
    [SerializeField] private bool debugInteractions = false;

    private GameStateManager gsm;
    private bool isInputOwner;

    private void Awake()
    {
        gsm = FindAnyObjectByType<GameStateManager>();
        isInputOwner = GetComponent<Player>() != null;
    }

    private void OnEnable()
    {
        if (!isInputOwner)
            return;

        leftAction.action.Enable();
        rightAction.action.Enable();
        upAction.action.Enable();
        downAction.action.Enable();
        clickAction.action.Enable();
        pointerPositionAction.action.Enable();
    }

    private void OnDisable()
    {
        if (!isInputOwner)
            return;

        leftAction.action.Disable();
        rightAction.action.Disable();
        upAction.action.Disable();
        downAction.action.Disable();
        clickAction.action.Disable();
        pointerPositionAction.action.Disable();
    }

    private void Update()
    {
        if (!isInputOwner)
            return;

        if (gsm != null && gsm.CurrentState != GameState.Playing)
            return;

        HandleKeyInput();
        HandlePointerInput();
    }

    private void HandleKeyInput()
    {
        var minigame = MinigameManager.Instance;
        if (minigame != null && minigame.HasActiveMinigame)
            return;

        var target = FindClosestEnemyWithMask();
        if (target == null)
            return;

        if (leftAction.action.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.LeftArrow);
        if (rightAction.action.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.RightArrow);
        if (upAction.action.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.UpArrow);
        if (downAction.action.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.DownArrow);
    }

    private void HandlePointerInput()
    {
        bool down = clickAction.action.WasPressedThisFrame();
        bool up = clickAction.action.WasReleasedThisFrame();
        bool held = clickAction.action.ReadValue<float>() > 0f;

        if (!down && !held && !up)
            return;

        var manager = MinigameManager.Instance;
        if (manager != null && manager.HasActiveMinigame)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
            {
                if (debugInteractions && down)
                    Debug.Log($"{nameof(PlayerInteractor)}: No main camera for minigame pointer input.", this);
                return;
            }

            Vector2 pointer = pointerPositionAction.action.ReadValue<Vector2>();
            var ray = mainCamera.ScreenPointToRay(new Vector3(pointer.x, pointer.y, 0f));
            if (!manager.TryGetPointerWorldPoint(ray, out var worldPoint))
            {
                if (debugInteractions && down)
                    Debug.Log($"{nameof(PlayerInteractor)}: Minigame pointer ray did not hit plane.", this);
                return;
            }

            if (down)
                manager.HandlePointerDown(worldPoint);
            if (held)
                manager.HandlePointerDrag(worldPoint);
            if (up)
                manager.HandlePointerUp(worldPoint);
            return;
        }

        if (!down)
            return;

        if (requireProximityForClick)
        {
            if (debugInteractions && down)
                Debug.Log($"{nameof(PlayerInteractor)}: Using targetBox overlap for click.", this);

            var overlapTarget = FindClosestEnemyWithMask();
            if (overlapTarget == null)
            {
                if (debugInteractions)
                    Debug.Log($"{nameof(PlayerInteractor)}: No enemy in targetBox for click.", this);
                return;
            }

            var clickedCollider = overlapTarget.GetComponentInChildren<Collider>();
            var overlapEvent = new InteractionEvent
            {
                type = InteractionType.Click,
                worldPoint = overlapTarget.transform.position,
                clickedObject = clickedCollider != null ? clickedCollider.gameObject : overlapTarget.gameObject
            };

            if (debugInteractions)
                Debug.Log($"{nameof(PlayerInteractor)}: Click (overlap) -> {overlapTarget.name}", overlapTarget);
            overlapTarget.TryHandleInteraction(overlapEvent, this);
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            if (debugInteractions && down)
                Debug.Log($"{nameof(PlayerInteractor)}: No main camera for pointer input.", this);
            return;
        }

        Vector2 pointerRay = pointerPositionAction.action.ReadValue<Vector2>();
        var raycastRay = mainCamera.ScreenPointToRay(new Vector3(pointerRay.x, pointerRay.y, 0f));

        if (!Physics.Raycast(raycastRay, out var hit, maxRaycastDistance, raycastMask))
        {
            if (debugInteractions && down)
                Debug.Log($"{nameof(PlayerInteractor)}: Raycast miss. Mask={raycastMask.value} Dist={maxRaycastDistance}", this);
            return;
        }

        var target = hit.collider.GetComponentInParent<EnemyMaskStackVisual>();
        if (target == null)
        {
            if (debugInteractions)
                Debug.Log($"{nameof(PlayerInteractor)}: Hit {hit.collider.name} but no EnemyMaskStackVisual in parents.", hit.collider);
            return;
        }

        var evt = new InteractionEvent
        {
            type = InteractionType.Click,
            worldPoint = hit.point,
            clickedObject = hit.collider.gameObject
        };

        if (debugInteractions)
            Debug.Log($"{nameof(PlayerInteractor)}: Click interaction -> {target.name}", target);
        target.TryHandleInteraction(evt, this);
    }

    private void SendKeyInteraction(EnemyMaskStackVisual target, KeyCode key)
    {
        var evt = new InteractionEvent
        {
            type = InteractionType.KeyDown,
            key = key
        };

        target.TryHandleInteraction(evt, this);
    }

    private EnemyMaskStackVisual FindClosestEnemyWithMask()
    {
        var hits = GetEnemyCandidates();
        if (hits == null || hits.Length == 0)
            return null;

        float bestDist = float.MaxValue;
        EnemyMaskStackVisual best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var enemy = hits[i].GetComponentInParent<EnemyMaskStackVisual>();
            if (enemy == null || !enemy.HasMask)
                continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy;
            }
        }

        return best;
    }

    private Collider[] GetEnemyCandidates()
    {
        if (targetBox == null)
            return null;

        var lossy = targetBox.transform.lossyScale;
        var scaled = new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
        Vector3 halfExtents = Vector3.Scale(targetBox.size, scaled) * 0.5f;
        Vector3 center = targetBox.transform.TransformPoint(targetBox.center);
        return Physics.OverlapBox(center, halfExtents, targetBox.transform.rotation, enemyLayer);
    }

    private bool IsInsideTargetBox(Vector3 worldPosition)
    {
        if (targetBox == null)
            return false;

        Vector3 local = targetBox.transform.InverseTransformPoint(worldPosition);
        Vector3 centered = local - targetBox.center;
        Vector3 half = targetBox.size * 0.5f;

        if (Mathf.Abs(centered.x) > half.x)
            return false;
        if (Mathf.Abs(centered.y) > half.y)
            return false;
        if (Mathf.Abs(centered.z) > half.z)
            return false;

        return true;
    }

}
