using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Hands : MonoBehaviour
{
    public enum HandPose
    {
        Idle,
        Reach,
        Grab,
        Flip,
        Rock,
        ThumbDown,
        ThumbUp
    }

    [System.Serializable]
    public struct HandSpriteSet
    {
        public Sprite idle;
        public Sprite reach;
        public Sprite grab;
        public Sprite flip;
        public Sprite rock;
        public Sprite thumbDown;
        public Sprite thumbUp;
    }
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
    [Header("Mask Fling Chaos")]
    public float maskFlingRandomSpreadDeg = 12f;
    public float maskFlingWobbleAmplitude = 0.12f;
    public float maskFlingWobbleFrequency = 16f;
    public float maskFlingWobbleDamping = 1.5f;
    public float maskFlingSpinJitterDegPerSec = 240f;
    [Header("Mask Fling Screen Space")]
    public bool useScreenSpaceFling = true;
    public float screenSpaceDepth = 1.6f;
    public float screenSpaceScale = 1.1f;
    [Range(0f, 1f)]
    public float screenSpaceOutwardBias = 0.75f;
    [Range(0f, 1f)]
    public float screenSpaceForwardBias = 0.25f;
    public Camera maskFlingCamera;

    [Header("References")]
    public Transform leftHand;
    public Transform rightHand;

    public Transform leftReadyPos;
    public Transform rightReadyPos;

    [Header("Hand Sprites")]
    public SpriteRenderer leftRenderer;
    public SpriteRenderer rightRenderer;
    public HandSpriteSet leftSprites;
    public HandSpriteSet rightSprites;

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
    private HandPose leftBasePose = HandPose.Idle;
    private HandPose rightBasePose = HandPose.Idle;
    private bool gestureActive;
    private HandPose gesturePose = HandPose.Idle;

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

        if (leftRenderer == null && leftHand != null)
            leftRenderer = leftHand.GetComponentInChildren<SpriteRenderer>();
        if (rightRenderer == null && rightHand != null)
            rightRenderer = rightHand.GetComponentInChildren<SpriteRenderer>();

        SetBasePose(true, HandPose.Idle);
        SetBasePose(false, HandPose.Idle);
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

        SetBasePose(isLeft, HandPose.Reach);

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
        SetBasePose(isLeft, HandPose.Grab);
        RestoreHandParent(hand, originalParent);
        SetReturnTargets(isLeft);
        SetBasePose(isLeft, HandPose.Idle);

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

        bool canUseLeft = leftHand != null && !leftExternalControl && !leftImpulseActive;
        bool canUseRight = rightHand != null && !rightExternalControl && !rightImpulseActive;

        if (!canUseLeft && !canUseRight)
            return;

        bool useLeft = canUseLeft;
        if (canUseLeft && canUseRight)
        {
            float leftDist = Vector3.Distance(leftHand.position, visual.position);
            float rightDist = Vector3.Distance(rightHand.position, visual.position);
            useLeft = leftDist <= rightDist;
        }
        else if (!canUseLeft)
        {
            useLeft = false;
        }

        if (useLeft)
        {
            leftRoutine = StartCoroutine(GrabAndThrow(leftHand, visual, enemyTransform, true));
        }
        else
        {
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

        SetBasePose(isLeft, HandPose.Reach);

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
        SetBasePose(isLeft, HandPose.Grab);
        Camera flingCam = ResolveFlingCamera();
        bool screenSpaceFling = useScreenSpaceFling && flingCam != null;
        GameObject clone = SpawnMaskClone(
            visual,
            enemyTransform,
            hand,
            flingCam,
            screenSpaceFling,
            out Vector3 throwDir,
            out Vector3 upDir,
            out float spinDegPerSec,
            out float wobblePhase);
        if (clone != null && !screenSpaceFling)
            clone.transform.SetParent(hand, true);

        if (maskHoldSeconds > 0f)
            yield return new WaitForSeconds(maskHoldSeconds);

        if (clone != null && !screenSpaceFling)
            clone.transform.SetParent(null, true);

        RestoreHandParent(hand, originalParent);
        SetReturnTargets(isLeft);
        SetBasePose(isLeft, HandPose.Idle);

        if (isLeft) leftImpulseActive = false;
        else rightImpulseActive = false;

        if (clone != null)
            yield return FlingMask(clone, throwDir, upDir, spinDegPerSec, wobblePhase);
    }

    private GameObject SpawnMaskClone(
        EnemyMaskStackVisual.MaskVisualData visual,
        Transform enemyTransform,
        Transform hand,
        Camera flingCam,
        bool screenSpaceFling,
        out Vector3 throwDir,
        out Vector3 upDir,
        out float spinDegPerSec,
        out float wobblePhase)
    {
        throwDir = Vector3.forward;
        if (hand != null)
            throwDir = hand.forward;
        if (enemyTransform != null && hand != null)
            throwDir = (hand.position - enemyTransform.position);

        if (throwDir.sqrMagnitude < 0.0001f)
            throwDir = hand != null ? hand.forward : Vector3.forward;
        throwDir.Normalize();

        upDir = Vector3.up;
        spinDegPerSec = maskFlingSpinDegPerSec + Random.Range(-maskFlingSpinJitterDegPerSec, maskFlingSpinJitterDegPerSec);
        wobblePhase = Random.Range(0f, Mathf.PI * 2f);

        var clone = new GameObject("MaskFlingClone");
        var renderer = clone.AddComponent<SpriteRenderer>();
        renderer.sprite = visual.sprite;
        renderer.sortingLayerID = visual.sortingLayerID;
        renderer.sortingOrder = visual.sortingOrder + 1;

        Vector3 position = visual.position;
        Quaternion rotation = visual.rotation;
        Vector3 scale = visual.lossyScale;
        if (scale.sqrMagnitude < 0.0001f)
            scale = Vector3.one;

        if (screenSpaceFling && flingCam != null)
        {
            Vector3 screen = flingCam.WorldToScreenPoint(visual.position);
            float depth = Mathf.Max(0.01f, screenSpaceDepth);
            position = flingCam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            if (screen.z < 0f)
                position = flingCam.transform.position + flingCam.transform.forward * depth;

            if (Mathf.Abs(screenSpaceScale - 1f) > 0.0001f)
                scale *= screenSpaceScale;

            Vector3 planeDir = Vector3.ProjectOnPlane(throwDir, flingCam.transform.forward);
            if (planeDir.sqrMagnitude < 0.0001f)
                planeDir = flingCam.transform.right;
            planeDir.Normalize();

            Vector3 outwardDir = GetScreenOutwardDir(flingCam, screen);
            if (outwardDir.sqrMagnitude < 0.0001f)
                outwardDir = planeDir;
            outwardDir.Normalize();

            Vector3 blended = Vector3.Slerp(planeDir, outwardDir, screenSpaceOutwardBias);
            if (screenSpaceForwardBias > 0f)
                blended = (blended + flingCam.transform.forward * screenSpaceForwardBias).normalized;

            throwDir = blended;
            upDir = flingCam.transform.up;

            float z = visual.rotation.eulerAngles.z;
            rotation = Quaternion.AngleAxis(z, flingCam.transform.forward) * flingCam.transform.rotation;
        }

        Vector3 right = Vector3.Cross(upDir, throwDir);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(Vector3.up, throwDir);
        right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

        if (maskFlingRandomSpreadDeg > 0f)
        {
            float yaw = Random.Range(-maskFlingRandomSpreadDeg, maskFlingRandomSpreadDeg);
            float pitch = Random.Range(-maskFlingRandomSpreadDeg, maskFlingRandomSpreadDeg) * 0.5f;
            Quaternion spread = Quaternion.AngleAxis(yaw, upDir) * Quaternion.AngleAxis(pitch, right);
            throwDir = (spread * throwDir).normalized;
        }

        clone.transform.position = position;
        clone.transform.rotation = rotation;
        clone.transform.localScale = scale;
        return clone;
    }

    private IEnumerator FlingMask(GameObject clone, Vector3 throwDir, Vector3 upDir, float spinDegPerSec, float wobblePhase)
    {
        Vector3 up = upDir.sqrMagnitude > 0.0001f ? upDir.normalized : Vector3.up;
        Vector3 velocity = throwDir * maskFlingSpeed + up * maskFlingUpward;
        float time = 0f;
        Vector3 basePos = clone.transform.position;
        Vector3 right = Vector3.Cross(up, throwDir);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(Vector3.up, throwDir);
        right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

        while (time < maskFlingDuration && clone != null)
        {
            velocity += -up * maskFlingGravity * Time.deltaTime;
            basePos += velocity * Time.deltaTime;

            float wobble = maskFlingWobbleAmplitude;
            if (maskFlingWobbleDamping > 0f)
                wobble *= Mathf.Exp(-maskFlingWobbleDamping * time);

            float t = time * maskFlingWobbleFrequency + wobblePhase;
            float t2 = time * (maskFlingWobbleFrequency * 1.37f) + wobblePhase * 0.7f;
            Vector3 wobbleOffset = (Mathf.Sin(t) * right + Mathf.Cos(t2) * up) * wobble;

            clone.transform.position = basePos + wobbleOffset;
            clone.transform.rotation = Quaternion.AngleAxis(spinDegPerSec * Time.deltaTime, Vector3.forward) * clone.transform.rotation;

            time += Time.deltaTime;
            yield return null;
        }

        if (clone != null)
            Destroy(clone);
    }

    private Camera ResolveFlingCamera()
    {
        if (maskFlingCamera != null)
            return maskFlingCamera;

        return Camera.main;
    }

    private static Vector3 GetScreenOutwardDir(Camera cam, Vector3 screenPos)
    {
        if (cam == null)
            return Vector3.zero;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 delta = new Vector2(screenPos.x, screenPos.y) - center;
        if (delta.sqrMagnitude < 0.0001f)
            return cam.transform.right;

        Vector3 world = cam.transform.right * delta.x + cam.transform.up * delta.y;
        return Vector3.ProjectOnPlane(world, cam.transform.forward);
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

    // ---------- HAND POSE ----------

    public void OnGestureFlip(InputAction.CallbackContext context)
    {
        HandleGestureInput(context, HandPose.Flip);
    }

    public void OnGestureRock(InputAction.CallbackContext context)
    {
        HandleGestureInput(context, HandPose.Rock);
    }

    public void OnGestureThumbDown(InputAction.CallbackContext context)
    {
        HandleGestureInput(context, HandPose.ThumbDown);
    }

    public void OnGestureThumbUp(InputAction.CallbackContext context)
    {
        HandleGestureInput(context, HandPose.ThumbUp);
    }

    private void HandleGestureInput(InputAction.CallbackContext context, HandPose pose)
    {
        if (context.started || context.performed)
        {
            SetGesturePose(pose);
            return;
        }

        if (context.canceled && gesturePose == pose)
            ClearGesturePose();
    }

    private void SetGesturePose(HandPose pose)
    {
        gestureActive = true;
        gesturePose = pose;
        ApplyPose(true, pose);
        ApplyPose(false, pose);
    }

    private void ClearGesturePose()
    {
        gestureActive = false;
        ApplyPose(true, leftBasePose);
        ApplyPose(false, rightBasePose);
    }

    private void SetBasePose(bool isLeft, HandPose pose)
    {
        if (isLeft)
            leftBasePose = pose;
        else
            rightBasePose = pose;

        if (!gestureActive)
            ApplyPose(isLeft, pose);
    }

    private void ApplyPose(bool isLeft, HandPose pose)
    {
        var renderer = isLeft ? leftRenderer : rightRenderer;
        if (renderer == null)
            return;

        var set = isLeft ? leftSprites : rightSprites;
        Sprite sprite = pose switch
        {
            HandPose.Reach => set.reach,
            HandPose.Grab => set.grab,
            HandPose.Flip => set.flip,
            HandPose.Rock => set.rock,
            HandPose.ThumbDown => set.thumbDown,
            HandPose.ThumbUp => set.thumbUp,
            _ => set.idle
        };

        if (sprite != null)
      { 
        renderer.material.SetTexture("_MainTex", sprite.texture);
        renderer.material.SetTexture("_PersonTex", sprite.texture);
        renderer.material.SetTexture("_ShirtTex", sprite.texture);
            renderer.sprite = sprite;
      }
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
