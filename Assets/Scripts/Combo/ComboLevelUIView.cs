using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboUIView : MonoBehaviour
{
    [Header("UI Elements")]
    public Image comboLevelSprite;
    public TextMeshProUGUI comboLevelLabel;
    public TextMeshProUGUI comboValueLabel;

    [Header("Animation")]
    public Animator comboAnimator;
    public string levelIntParam = "ComboLevelIndex";

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

            UpdateComboLevelVisuals();
            UpdateComboValueVisuals();
        }
    }

    void Update()
    {

    }

    void UpdateComboLevelVisuals()
    {
        int currentLevel = mComboManagerRef.GetCurrentComboLevel();
        bool showLevel = currentLevel > 0;

        comboLevelSprite.gameObject.SetActive(showLevel);
        comboLevelLabel.gameObject.SetActive(showLevel);

        if (showLevel)
        {
            comboLevelSprite.sprite = mComboManagerRef.GetCurrentComboSprite();
            comboLevelLabel.text = mComboManagerRef.GetCurrentComboLevelName();
            comboAnimator.SetInteger(levelIntParam, currentLevel);
            comboAnimator.SetTrigger("OnLevelChange");
        }
    }

    void UpdateComboValueVisuals()
    {
        float val = mComboManagerRef.GetCurrentComboValue();
        comboValueLabel.gameObject.SetActive(val > 0);

        if (val > 0)
        {
            comboValueLabel.text = val.ToString();
        }
    }
}
