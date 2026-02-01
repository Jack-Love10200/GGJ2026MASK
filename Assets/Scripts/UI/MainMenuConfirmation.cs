using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuConfirmation : MonoBehaviour
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
        SceneManager.LoadScene("MainMenu");
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
