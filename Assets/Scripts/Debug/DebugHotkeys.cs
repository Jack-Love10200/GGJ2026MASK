using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugHotkeys : MonoBehaviour
{
    // Public
    public InputActionReference IncreaseScoreDebugHotkey;
    public InputActionReference DecreaseScoreDebugHotkey;
    public InputActionReference DecreaseLevelDebugHotkey;
    public InputActionReference IncreaseLevelDebugHotkey;

    float DebugComboValueChangePerKeyPress = 150;
    // Private
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        IncreaseScoreDebugHotkey.action.performed += DebugIncreaseComboScore;
        DecreaseScoreDebugHotkey.action.performed += DebugDecreaseComboScore;
        DecreaseLevelDebugHotkey.action.performed += DebugDecreaseComboLevel;
        IncreaseLevelDebugHotkey.action.performed += DebugIncreaseComboLevel;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DebugIncreaseComboScore(InputAction.CallbackContext context)
    {
        LevelScopeManagers.Instance.GetComponent<ComboManager>().DebugIncreaseComboValue(DebugComboValueChangePerKeyPress);
    }

    void DebugDecreaseComboScore(InputAction.CallbackContext context)
    {
        LevelScopeManagers.Instance.GetComponent<ComboManager>().DebugDecreaseComboValue(DebugComboValueChangePerKeyPress);
    }

    void DebugIncreaseComboLevel(InputAction.CallbackContext context)
    {
        LevelScopeManagers.Instance.GetComponent<ComboManager>().DebugIncrementComboLevel();
    }

    void DebugDecreaseComboLevel(InputAction.CallbackContext context)
    {
        LevelScopeManagers.Instance.GetComponent<ComboManager>().DebugDecrementComboLevel();
    }
}
