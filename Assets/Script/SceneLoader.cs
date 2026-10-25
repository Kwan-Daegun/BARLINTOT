using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public GameObject loadingScreen;

    public void PlayGame(string sceneName)
    {
        StartCoroutine(LoadScene(sceneName));
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadScene(SceneManager.GetActiveScene().name));
    }

    public void BackToMenu(string sceneName)
    {
        StartCoroutine(LoadScene(sceneName));
    }

    public void Quit()
    {
        Application.Quit();
    }

    private IEnumerator LoadScene(string sceneName)
    {
        loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
            yield return null;

        yield return new WaitForSeconds(0.5f);
        operation.allowSceneActivation = true;
    }
}