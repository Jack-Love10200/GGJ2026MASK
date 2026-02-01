using System;
using System.Collections.Generic;
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
    [SerializeField] private KeyCode interactKey = KeyCode.Space;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float maxRaycastDistance = 200f;
    [SerializeField] private bool debugInteractions = false;

    private GameStateManager gsm;
    private bool isInputOwner;
    private InputAction leftActionRuntime;
    private InputAction rightActionRuntime;
    private InputAction upActionRuntime;
    private InputAction downActionRuntime;
    private InputAction clickActionRuntime;
    private InputAction pointerPositionActionRuntime;
    private readonly HashSet<EnemyMaskStackVisual> minigameBlocked = new HashSet<EnemyMaskStackVisual>();
    private readonly HashSet<EnemyMaskStackVisual> minigameInside = new HashSet<EnemyMaskStackVisual>();
    private readonly List<EnemyMaskStackVisual> minigameRemove = new List<EnemyMaskStackVisual>();

    private void Awake()
    {
        gsm = FindAnyObjectByType<GameStateManager>();
        isInputOwner = GetComponent<Player>() != null;
    }

    private void OnEnable()
    {
        if (!isInputOwner)
            return;

        ResolveInputActions();
        EnableAction(leftActionRuntime);
        EnableAction(rightActionRuntime);
        EnableAction(upActionRuntime);
        EnableAction(downActionRuntime);
        EnableAction(clickActionRuntime);
        EnableAction(pointerPositionActionRuntime);
    }

    private void OnDisable()
    {
        if (!isInputOwner)
            return;

        DisableAction(leftActionRuntime);
        DisableAction(rightActionRuntime);
        DisableAction(upActionRuntime);
        DisableAction(downActionRuntime);
        DisableAction(clickActionRuntime);
        DisableAction(pointerPositionActionRuntime);
    }

    private void Update()
    {
        if (!isInputOwner)
            return;

        if (gsm != null && gsm.CurrentState != GameState.Playing)
            return;

        UpdateMinigameReentry();
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

        if (leftActionRuntime != null && leftActionRuntime.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.LeftArrow);
        if (rightActionRuntime != null && rightActionRuntime.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.RightArrow);
        if (upActionRuntime != null && upActionRuntime.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.UpArrow);
        if (downActionRuntime != null && downActionRuntime.WasPressedThisFrame())
            SendKeyInteraction(target, KeyCode.DownArrow);

        if (interactKey != KeyCode.None && !IsArrowKey(interactKey) && WasKeyPressedThisFrame(interactKey))
            SendKeyInteraction(target, interactKey);
    }

    private void HandlePointerInput()
    {
        if (clickActionRuntime == null || pointerPositionActionRuntime == null)
            return;

        bool down = clickActionRuntime.WasPressedThisFrame();
        bool up = clickActionRuntime.WasReleasedThisFrame();
        bool held = clickActionRuntime.ReadValue<float>() > 0f;

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

            Vector2 pointer = pointerPositionActionRuntime.ReadValue<Vector2>();
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

        Vector2 pointerRay = pointerPositionActionRuntime.ReadValue<Vector2>();
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

    private void UpdateMinigameReentry()
    {
        if (minigameBlocked.Count == 0)
            return;

        minigameInside.Clear();
        var hits = GetEnemyCandidates();
        if (hits != null)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var enemy = hits[i].GetComponentInParent<EnemyMaskStackVisual>();
                if (enemy != null)
                    minigameInside.Add(enemy);
            }
        }

        minigameRemove.Clear();
        foreach (var enemy in minigameBlocked)
        {
            if (enemy == null || !minigameInside.Contains(enemy))
                minigameRemove.Add(enemy);
        }

        for (int i = 0; i < minigameRemove.Count; i++)
            minigameBlocked.Remove(minigameRemove[i]);
    }

    public void BlockMinigameUntilExit(EnemyMaskStackVisual enemy)
    {
        if (enemy == null)
            return;

        minigameBlocked.Add(enemy);
    }

    public bool IsMinigameBlocked(EnemyMaskStackVisual enemy)
    {
        if (enemy == null)
            return false;

        return minigameBlocked.Contains(enemy);
    }

    private static bool IsArrowKey(KeyCode key)
    {
        return key == KeyCode.LeftArrow ||
               key == KeyCode.RightArrow ||
               key == KeyCode.UpArrow ||
               key == KeyCode.DownArrow;
    }

    private static bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
        if (Keyboard.current == null || keyCode == KeyCode.None)
            return false;

        if (!TryMapKeyCode(keyCode, out var key))
            return false;

        var control = Keyboard.current[key];
        return control != null && control.wasPressedThisFrame;
    }

    private void ResolveInputActions()
    {
        var asset = GetInputActionAsset();
        leftActionRuntime = ResolveAction(asset, leftAction, "LeftArrow");
        rightActionRuntime = ResolveAction(asset, rightAction, "RightArrow");
        upActionRuntime = ResolveAction(asset, upAction, "UpArrow");
        downActionRuntime = ResolveAction(asset, downAction, "DownArrow");
        clickActionRuntime = ResolveAction(asset, clickAction, "LeftClick");
        pointerPositionActionRuntime = ResolveAction(asset, pointerPositionAction, "PointerPosition");
    }

    private InputActionAsset GetInputActionAsset()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
            return playerInput.actions;

        if (leftAction != null && leftAction.action != null)
            return leftAction.action.actionMap != null ? leftAction.action.actionMap.asset : null;

        return null;
    }

    private static InputAction ResolveAction(InputActionAsset asset, InputActionReference reference, string actionName)
    {
        if (asset != null)
        {
            var action = asset.FindAction(actionName, false);
            if (action != null)
                return action;

            action = asset.FindAction($"Player/{actionName}", false);
            if (action != null)
                return action;
        }

        if (reference != null)
            return reference.action;

        return null;
    }

    private static void EnableAction(InputAction action)
    {
        if (action != null && !action.enabled)
            action.Enable();
    }

    private static void DisableAction(InputAction action)
    {
        if (action != null && action.enabled)
            action.Disable();
    }

    private static bool TryMapKeyCode(KeyCode keyCode, out Key key)
    {
        key = Key.None;

        string name = keyCode.ToString();
        if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
        {
            name = $"Digit{keyCode - KeyCode.Alpha0}";
        }
        else if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
        {
            name = $"Numpad{keyCode - KeyCode.Keypad0}";
        }
        else if (keyCode == KeyCode.Return)
        {
            name = "Enter";
        }
        else if (keyCode == KeyCode.KeypadEnter)
        {
            name = "NumpadEnter";
        }

        return Enum.TryParse(name, out key);
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
