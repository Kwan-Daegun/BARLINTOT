using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void PlayGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}