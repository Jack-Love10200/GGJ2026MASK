using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float interactRadius = 2f;
    [SerializeField] private LayerMask enemyLayer = ~0;
    [SerializeField] private bool requireProximityForClick = true;

    [Header("Input")]
    [SerializeField] private KeyCode[] keyInputs = { KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow };
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float maxRaycastDistance = 200f;

    private void Update()
    {
        HandleKeyInput();
        HandlePointerInput();
    }

    private void HandleKeyInput()
    {
        if (keyInputs == null || keyInputs.Length == 0)
            return;

        var target = FindClosestEnemyWithMask();
        if (target == null)
            return;

        for (int i = 0; i < keyInputs.Length; i++)
        {
            var key = keyInputs[i];
            if (!Input.GetKeyDown(key))
                continue;

            var evt = new InteractionEvent
            {
                type = InteractionType.KeyDown,
                key = key
            };

            target.TryHandleInteraction(evt, this);
        }
    }

    private void HandlePointerInput()
    {
        bool down = Input.GetMouseButtonDown(0);
        bool held = Input.GetMouseButton(0);
        bool up = Input.GetMouseButtonUp(0);

        if (!down && !held && !up)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, maxRaycastDistance, raycastMask))
            return;

        var manager = MinigameManager.Instance;
        if (manager != null && manager.HasActiveMinigame)
        {
            if (down)
                manager.HandlePointerDown(hit.point);
            if (held)
                manager.HandlePointerDrag(hit.point);
            if (up)
                manager.HandlePointerUp(hit.point);
            return;
        }

        if (!down)
            return;

        var target = hit.collider.GetComponentInParent<EnemyMaskStackVisual>();
        if (target == null)
            return;

        if (requireProximityForClick)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > interactRadius)
                return;
        }

        var evt = new InteractionEvent
        {
            type = InteractionType.Click,
            worldPoint = hit.point,
            clickedObject = hit.collider.gameObject
        };

        target.TryHandleInteraction(evt, this);
    }

    private EnemyMaskStackVisual FindClosestEnemyWithMask()
    {
        var hits = Physics.OverlapSphere(transform.position, interactRadius, enemyLayer);
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
}
