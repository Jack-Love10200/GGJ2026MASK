using UnityEngine;
using UnityEngine.UI;

public class QuitConfirmation : MonoBehaviour
{
    [Header("Buttons")]
    public Button yesButton;
    public Button noButton;

    void Start()
    {
        yesButton.onClick.AddListener(YesButton);
        noButton.onClick.AddListener(NoButton);
    }

    void Update()
    {
        
    }

    public void YesButton()
    {
        Application.Quit();
    }

    public void NoButton()
    {
        GetComponent<Animator>().SetTrigger("Close");
    }

    public void DestroyMe()
    {
        Destroy(this.gameObject);
    }
}
