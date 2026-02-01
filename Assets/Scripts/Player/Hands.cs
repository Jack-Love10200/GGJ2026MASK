using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Hands : MonoBehaviour
{
    [Header("Speeds")]
    public float moveToOptionSpeed = 16f;
    public float returnSpeed = 8f;
    public float attachFollowSpeed = 18f;
    public float cursorFollowSpeed = 22f;

    [Header("Mask Fling")]
    public float maskHoldSeconds = 0.06f;
    public float maskFlingSpeed = 2.5f;
    public float maskFlingUpward = 1.2f;
    public float maskFlingGravity = 4f;
    public float maskFlingDuration = 0.7f;
    public float maskFlingSpinDegPerSec = 360f;

    [Header("References")]
    public Transform leftHand;
    public Transform rightHand;

    public Transform leftReadyPos;
    public Transform rightReadyPos;

    private Vector3 leftOriginPos;
    private Vector3 rightOriginPos;
    private Quaternion leftOriginRot;
    private Quaternion rightOriginRot;
    private Transform leftOriginalParent;
    private Transform rightOriginalParent;

    private Vector3 leftLocalTargetPos;
    private Vector3 rightLocalTargetPos;
    private Quaternion leftLocalTargetRot;
    private Quaternion rightLocalTargetRot;

    private readonly List<Transform> leftOptions = new();
    private readonly List<Transform> rightOptions = new();

    private Transform leftOption;
    private Transform rightOption;

    private bool leftReadied;
    private bool rightReadied;

    private bool leftImpulseActive;
    private bool rightImpulseActive;
    private bool leftExternalControl;
    private bool rightExternalControl;
    private Transform leftAttachTarget;
    private Vector3 rightCursorWorld;
    private bool rightCursorHasTarget;
    private Quaternion leftExternalRotation;

    private Coroutine leftRoutine;
    private Coroutine rightRoutine;

    private void Start()
    {
        leftOriginalParent = leftHand != null ? leftHand.parent : null;
        rightOriginalParent = rightHand != null ? rightHand.parent : null;

        leftOriginPos = leftHand.localPosition;
        rightOriginPos = rightHand.localPosition;

        leftOriginRot = leftHand.localRotation;
        rightOriginRot = rightHand.localRotation;

        leftLocalTargetPos = leftOriginPos;
        rightLocalTargetPos = rightOriginPos;

        leftLocalTargetRot = leftOriginRot;
        rightLocalTargetRot = rightOriginRot;
    }

    private void Update()
    {
        if (leftExternalControl)
        {
            UpdateLeftExternal();
        }
        else if (!leftImpulseActive)
        {
            leftHand.localPosition = Vector3.Lerp(
                leftHand.localPosition,
                leftLocalTargetPos,
                Time.deltaTime * returnSpeed
            );

            leftHand.localRotation = Quaternion.Slerp(
                leftHand.localRotation,
                leftLocalTargetRot,
                Time.deltaTime * returnSpeed
            );
        }

        if (rightExternalControl)
        {
            UpdateRightExternal();
        }
        else if (!rightImpulseActive)
        {
            rightHand.localPosition = Vector3.Lerp(
                rightHand.localPosition,
                rightLocalTargetPos,
                Time.deltaTime * returnSpeed
            );

            rightHand.localRotation = Quaternion.Slerp(
                rightHand.localRotation,
                rightLocalTargetRot,
                Time.deltaTime * returnSpeed
            );
        }
    }

    // ---------- OPTION MANAGEMENT ----------

    public void AddOption(Transform hand, Transform option)
    {
        var list = hand == leftHand ? leftOptions : rightOptions;

        if (!list.Contains(option))
            list.Add(option);

        UpdateClosestOption(hand);
    }

    public void RemoveOption(Transform hand, Transform option)
    {
        var list = hand == leftHand ? leftOptions : rightOptions;

        if (!list.Remove(option))
            return;

        if (list.Count == 0)
            UnreadyHand(hand);
        else
            UpdateClosestOption(hand);
    }

    private void UpdateClosestOption(Transform hand)
    {
        bool isLeft = hand == leftHand;
        var list = isLeft ? leftOptions : rightOptions;

        if (list.Count == 0)
            return;

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var option in list)
        {
            float d = Vector3.Distance(hand.position, option.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = option;
            }
        }

        if (isLeft)
        {
            leftOption = closest;
            leftReadied = true;

            leftLocalTargetPos = leftReadyPos.localPosition;
            leftLocalTargetRot = leftReadyPos.localRotation;
        }
        else
        {
            rightOption = closest;
            rightReadied = true;

            rightLocalTargetPos = rightReadyPos.localPosition;
            rightLocalTargetRot = rightReadyPos.localRotation;
        }
    }

    private void UnreadyHand(Transform hand)
    {
        if (hand == leftHand)
        {
            leftReadied = false;
            leftOption = null;

            leftLocalTargetPos = leftOriginPos;
            leftLocalTargetRot = leftOriginRot;
        }
        else
        {
            rightReadied = false;
            rightOption = null;

            rightLocalTargetPos = rightOriginPos;
            rightLocalTargetRot = rightOriginRot;
        }
    }

    // ---------- INPUT ----------

    public void TryGrabLeft(InputAction.CallbackContext context)
    {
        if (context.started && leftOption)
        {
            if (leftRoutine != null)
                StopCoroutine(leftRoutine);

            leftRoutine = StartCoroutine(
                GrabImpulse(leftHand, leftOption, true)
            );
        }
    }

    public void TryGrabRight(InputAction.CallbackContext context)
    {
        if (context.started && rightOption)
        {
            if (rightRoutine != null)
                StopCoroutine(rightRoutine);

            rightRoutine = StartCoroutine(
                GrabImpulse(rightHand, rightOption, false)
            );
        }
    }

    // ---------- IMPULSE ----------

    private IEnumerator GrabImpulse(
        Transform hand,
        Transform option,
        bool isLeft
    )
    {
        if (isLeft) leftImpulseActive = true;
        else rightImpulseActive = true;

        Transform originalParent = hand.parent;
        Quaternion fixedRotation = hand.rotation;
        hand.SetParent(null, true);

        // World-space impulse
        while (Vector3.Distance(hand.position, option.position) > 0.01f)
        {
            hand.position = Vector3.Lerp(
                hand.position,
                option.position,
                Time.deltaTime * moveToOptionSpeed
            );

            hand.rotation = fixedRotation;

            yield return null;
        }

        hand.rotation = fixedRotation;
        RestoreHandParent(hand, originalParent);
        SetReturnTargets(isLeft);

        if (isLeft) leftImpulseActive = false;
        else rightImpulseActive = false;
    }

    // ---------- MINIGAME CONTROL ----------

    public void EnterMinigameMode(Transform leftSocket)
    {
        leftAttachTarget = leftSocket;
        leftExternalControl = leftAttachTarget != null;
        rightExternalControl = true;
        rightCursorHasTarget = false;

        if (leftExternalControl && leftHand != null)
        {
            leftHand.SetParent(null, true);
            leftExternalRotation = leftHand.rotation;
        }

        if (rightExternalControl && rightHand != null)
            rightHand.SetParent(null, true);
    }

    public void ExitMinigameMode()
    {
        leftExternalControl = false;
        rightExternalControl = false;
        leftAttachTarget = null;
        rightCursorHasTarget = false;

        if (leftHand != null)
            RestoreHandParent(leftHand, leftOriginalParent);
        if (rightHand != null)
            RestoreHandParent(rightHand, rightOriginalParent);

        SetReturnTargets(true);
        SetReturnTargets(false);
    }

    public void SetRightCursorWorld(Vector3 worldPoint)
    {
        rightCursorWorld = worldPoint;
        rightCursorHasTarget = true;
    }

    // ---------- MASK FLING ----------

    public void PlayMaskGrab(EnemyMaskStackVisual.MaskVisualData visual, Transform enemyTransform)
    {
        if (visual.sprite == null)
            return;

        bool canUseLeft = leftHand != null && !leftExternalControl;
        bool canUseRight = rightHand != null && !rightExternalControl;

        if (!canUseLeft && !canUseRight)
            return;

        bool useLeft = canUseLeft;
        if (canUseLeft && canUseRight)
        {
            float leftDist = Vector3.Distance(leftHand.position, visual.position);
            float rightDist = Vector3.Distance(rightHand.position, visual.position);
            useLeft = leftDist <= rightDist;
        }

        if (useLeft)
        {
            if (leftRoutine != null)
                StopCoroutine(leftRoutine);
            leftRoutine = StartCoroutine(GrabAndThrow(leftHand, visual, enemyTransform, true));
        }
        else
        {
            if (rightRoutine != null)
                StopCoroutine(rightRoutine);
            rightRoutine = StartCoroutine(GrabAndThrow(rightHand, visual, enemyTransform, false));
        }
    }

    private IEnumerator GrabAndThrow(
        Transform hand,
        EnemyMaskStackVisual.MaskVisualData visual,
        Transform enemyTransform,
        bool isLeft
    )
    {
        if (isLeft) leftImpulseActive = true;
        else rightImpulseActive = true;

        Transform originalParent = hand.parent;
        Quaternion fixedRotation = hand.rotation;
        hand.SetParent(null, true);

        while (Vector3.Distance(hand.position, visual.position) > 0.01f)
        {
            hand.position = Vector3.Lerp(
                hand.position,
                visual.position,
                Time.deltaTime * moveToOptionSpeed
            );

            hand.rotation = fixedRotation;

            yield return null;
        }

        hand.rotation = fixedRotation;
        GameObject clone = SpawnMaskClone(visual);
        if (clone != null)
            clone.transform.SetParent(hand, true);

        if (maskHoldSeconds > 0f)
            yield return new WaitForSeconds(maskHoldSeconds);

        if (clone != null)
            clone.transform.SetParent(null, true);

        Vector3 throwDir = hand.forward;
        if (enemyTransform != null)
            throwDir = (hand.position - enemyTransform.position);

        if (throwDir.sqrMagnitude < 0.0001f)
            throwDir = hand.forward;
        throwDir.Normalize();

        RestoreHandParent(hand, originalParent);
        SetReturnTargets(isLeft);

        if (isLeft) leftImpulseActive = false;
        else rightImpulseActive = false;

        if (clone != null)
            yield return FlingMask(clone, throwDir);
    }

    private GameObject SpawnMaskClone(EnemyMaskStackVisual.MaskVisualData visual)
    {
        var clone = new GameObject("MaskFlingClone");
        var renderer = clone.AddComponent<SpriteRenderer>();
        renderer.sprite = visual.sprite;
        renderer.sortingLayerID = visual.sortingLayerID;
        renderer.sortingOrder = visual.sortingOrder + 1;

        clone.transform.position = visual.position;
        clone.transform.rotation = visual.rotation;
        Vector3 scale = visual.lossyScale;
        if (scale.sqrMagnitude < 0.0001f)
            scale = Vector3.one;
        clone.transform.localScale = scale;
        return clone;
    }

    private IEnumerator FlingMask(GameObject clone, Vector3 throwDir)
    {
        Vector3 velocity = throwDir * maskFlingSpeed + Vector3.up * maskFlingUpward;
        float time = 0f;

        while (time < maskFlingDuration && clone != null)
        {
            velocity += Vector3.down * maskFlingGravity * Time.deltaTime;
            clone.transform.position += velocity * Time.deltaTime;
            clone.transform.rotation = Quaternion.AngleAxis(maskFlingSpinDegPerSec * Time.deltaTime, Vector3.forward) * clone.transform.rotation;

            time += Time.deltaTime;
            yield return null;
        }

        if (clone != null)
            Destroy(clone);
    }

    private void UpdateLeftExternal()
    {
        if (leftAttachTarget == null || leftHand == null)
            return;

        leftHand.position = Vector3.Lerp(
            leftHand.position,
            leftAttachTarget.position,
            Time.deltaTime * attachFollowSpeed
        );

        leftHand.rotation = leftExternalRotation;
    }

    private void UpdateRightExternal()
    {
        if (!rightCursorHasTarget || rightHand == null)
            return;

        rightHand.position = Vector3.Lerp(
            rightHand.position,
            rightCursorWorld,
            Time.deltaTime * cursorFollowSpeed
        );
    }

    private void RestoreHandParent(Transform hand, Transform originalParent)
    {
        hand.SetParent(originalParent, true);
        if (originalParent == null)
            return;

        hand.localPosition = originalParent.InverseTransformPoint(hand.position);
        hand.localRotation = Quaternion.Inverse(originalParent.rotation) * hand.rotation;
    }

    private void SetReturnTargets(bool isLeft)
    {
        if (isLeft)
        {
            leftLocalTargetPos = leftReadied
                ? leftReadyPos.localPosition
                : leftOriginPos;

            leftLocalTargetRot = leftReadied
                ? leftReadyPos.localRotation
                : leftOriginRot;
        }
        else
        {
            rightLocalTargetPos = rightReadied
                ? rightReadyPos.localPosition
                : rightOriginPos;

            rightLocalTargetRot = rightReadied
                ? rightReadyPos.localRotation
                : rightOriginRot;
        }
    }
}
