using UnityEngine;

[CreateAssetMenu(fileName = "GameQuotaSettings", menuName = "Scriptable Objects/GameQuotaSettings")]
public class GameQuotaSettings : ScriptableObject
{
    public int KillTarget = 80; // Number of kills (ripping off all an enemy's masks) to win. Will likely be broken up by enemy type if we get time
}
