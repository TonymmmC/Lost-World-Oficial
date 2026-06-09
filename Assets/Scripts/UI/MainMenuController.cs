using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame() => StorySequence.Iniciar();

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
