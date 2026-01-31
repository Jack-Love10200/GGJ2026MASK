using UnityEngine;

public interface IMinigame
{
    void Begin(MinigameContext ctx);
    void HandlePointerDown(Vector3 localPos);
    void HandlePointerDrag(Vector3 localPos);
    void HandlePointerUp(Vector3 localPos);
    void Cancel();
}
