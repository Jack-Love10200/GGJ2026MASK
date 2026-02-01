using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Hands : MonoBehaviour
{
    [Header("Speeds")]
    public float moveToOptionSpeed = 16f;
    public float returnSpeed = 8f;

    [Header("References")]
    public Transform leftHand;
    public Transform rightHand;

    public Transform leftReadyPos;
    public Transform rightReadyPos;

    private Vector3 leftOriginPos;
    private Vector3 rightOriginPos;
    private Quaternion leftOriginRot;
    private Quaternion rightOriginRot;

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

    private Coroutine leftRoutine;
    private Coroutine rightRoutine;

    private void Start()
    {
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
        if (!leftImpulseActive)
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

        if (!rightImpulseActive)
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

        Transform originalParent = transform;
        hand.SetParent(null, true);

        // World-space impulse
        while (
            Vector3.Distance(hand.position, option.position) > 0.01f ||
            Quaternion.Angle(hand.rotation, option.rotation) > 1f
        )
        {
            hand.position = Vector3.Lerp(
                hand.position,
                option.position,
                Time.deltaTime * moveToOptionSpeed
            );

            hand.rotation = Quaternion.Slerp(
                hand.rotation,
                option.rotation,
                Time.deltaTime * moveToOptionSpeed
            );

            yield return null;
        }

        // Re-parent and convert space correctly
        hand.SetParent(originalParent, true);
        hand.localPosition = originalParent.InverseTransformPoint(hand.position);
        hand.localRotation = Quaternion.Inverse(originalParent.rotation) * hand.rotation;

        // Set return targets
        if (isLeft)
        {
            leftLocalTargetPos = leftReadied
                ? leftReadyPos.localPosition
                : leftOriginPos;

            leftLocalTargetRot = leftReadied
                ? leftReadyPos.localRotation
                : leftOriginRot;

            leftImpulseActive = false;
        }
        else
        {
            rightLocalTargetPos = rightReadied
                ? rightReadyPos.localPosition
                : rightOriginPos;

            rightLocalTargetRot = rightReadied
                ? rightReadyPos.localRotation
                : rightOriginRot;

            rightImpulseActive = false;
        }
    }
}
