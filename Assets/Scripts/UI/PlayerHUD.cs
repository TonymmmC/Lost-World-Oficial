using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image fillBar;
    [SerializeField] private Health playerHealth;

    private void Start()
    {
        if (playerHealth == null) { Debug.LogError($"{name}: falta referencia a Health"); return; }
        playerHealth.OnChanged += UpdateBar;
        UpdateBar(playerHealth.Current, playerHealth.Max);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnChanged -= UpdateBar;
    }

    private void UpdateBar(int current, int max)
    {
        fillBar.fillAmount = (float)current / max;
    }
}
