using UnityEngine;

public class BillboardY : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void LateUpdate()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
            return;

        Vector3 toCam = cam.transform.position - transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(toCam);
    }
}
