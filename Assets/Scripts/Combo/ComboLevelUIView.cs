using TMPro;
using UnityEngine;

public class ComboUIView : MonoBehaviour
{
    ComboManager mComboManagerRef;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mComboManagerRef = LevelScopeManagers.Instance.GetComponent<ComboManager>();

        if (mComboManagerRef == null )
        {
            print("ComboUIView:Start: Could not find object of name ComboManagerHolder to get the combo manager from. Make sure it exists in the scene");
        }
        else
        {
            // Subscribe to the changing of the combo level
            mComboManagerRef.mOnComboLevelChanged += UpdateComboLevelVisuals;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void UpdateComboLevelVisuals()
    {
        GetComponent<TextMeshProUGUI>().SetText(mComboManagerRef.GetCurrentComboLevelName());
    }
}
