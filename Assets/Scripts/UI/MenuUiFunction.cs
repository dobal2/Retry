using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUiFunction : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Main");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Retry();
            PlayerStats.Instance.ResetAllStats();
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}
