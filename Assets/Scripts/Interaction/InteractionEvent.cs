using UnityEngine;

public struct InteractionEvent
{
    public InteractionType type;
    public KeyCode key;
    public Vector3 worldPoint;
    public GameObject clickedObject;
}
