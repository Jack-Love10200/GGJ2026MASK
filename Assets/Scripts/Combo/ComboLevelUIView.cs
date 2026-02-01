using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboUIView : MonoBehaviour
{
    [Header("UI Elements")]
    public Image comboLevelSprite;
    public TextMeshProUGUI comboLevelLabel;
    public TextMeshProUGUI comboValueLabel;

    ComboManager mComboManagerRef;

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
            mComboManagerRef.mOnComboValueChanged += UpdateComboValueVisuals;
        }
    }

    void Update()
    {

    }

    void UpdateComboLevelVisuals()
    {
        comboLevelSprite.sprite = mComboManagerRef.GetCurrentComboSprite();
        comboLevelLabel.text = mComboManagerRef.GetCurrentComboLevelName();


    }

    void UpdateComboValueVisuals()
    {
        comboValueLabel.text = mComboManagerRef.GetCurrentComboValue().ToString();
    }
}
