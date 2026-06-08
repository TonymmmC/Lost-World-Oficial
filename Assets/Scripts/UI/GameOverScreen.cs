using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;

    public void Show() => StartCoroutine(ShowDelayed());

    private IEnumerator ShowDelayed()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        gameOverPanel.SetActive(true);
    }

    public void Respawn()
    {
        SceneManager.LoadScene("World");
    }
}
