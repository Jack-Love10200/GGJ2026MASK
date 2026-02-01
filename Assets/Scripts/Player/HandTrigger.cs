using UnityEngine;

public class HandTrigger : MonoBehaviour
{
    public enum HandSide
    {
        Left,
        Right
    }

    [Header("Setup")]
    public Transform myHand;
    public HandSide handSide;

    private Hands hands;

    private void Start()
    {
        hands = GetComponentInParent<Hands>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Mask")) return;

        TestMask mask = other.GetComponent<TestMask>();
        if (!mask) return;

        Transform grabPoint = GetGrabPoint(mask);
        if (!grabPoint) return;

        hands.AddOption(myHand, grabPoint);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Mask")) return;

        TestMask mask = other.GetComponent<TestMask>();
        if (!mask) return;

        Transform grabPoint = GetGrabPoint(mask);
        if (!grabPoint) return;

        hands.RemoveOption(myHand, grabPoint);
    }

    private Transform GetGrabPoint(TestMask mask)
    {
        return handSide == HandSide.Left
            ? mask.leftGrabPoint
            : mask.rightGrabPoint;
    }
}
