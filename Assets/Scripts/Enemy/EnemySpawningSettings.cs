using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawningSettings", menuName = "Scriptable Objects/EnemySpawningSettings")]
public class EnemySpawningSettings : ScriptableObject
{
    public enum EnemyClass
    {
        NonEvent,
        Sequence
    }

    [System.Serializable]
    public class EnemyTypeSettings
    {
        public EnemyClass EnemyClass = EnemyClass.NonEvent;

        [Header("Initial Masks (Bottom -> Top)")]
        public List<MaskDef> initialMasksBottomToTop = new List<MaskDef>();

        [Header("Spawning Counts")]
        public int desiredNumberMin = 0;

        public int desiredNumberMax = 3;
    }

    public List<EnemyTypeSettings> SpecialEnemyTypes = new List<EnemyTypeSettings>();

}
