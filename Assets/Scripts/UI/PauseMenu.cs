using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject firstSelected;
    private bool isPaused;
    private PlayerMovement playerMovement;

    private void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
    }

    void Update()
    {
        bool pausePressed = Input.GetKeyDown(KeyCode.Escape);
        var gamepad = Gamepad.current;
        if (gamepad != null)
            pausePressed |= gamepad.startButton.wasPressedThisFrame;

        if (pausePressed)
            Toggle();
    }

    public void Toggle()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        if (playerMovement != null)
            playerMovement.enabled = !isPaused;

        if (isPaused && firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void Resume() => Toggle();

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
