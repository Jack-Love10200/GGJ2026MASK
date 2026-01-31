using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboLevelSettings", menuName = "Scriptable Objects/ComboLevelSettings")]
public class ComboLevelSettings : ScriptableObject
{
    public List<string> mComboLevelNames;

    public List<float> mComboLevelThresholds;

    public float mComboValueVisualScalar = 1.0f; // This is a multiplier on what the combo value is, which can let the actually seen combo value seem greater

}
