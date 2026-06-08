using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] private Image fillBar;
    [SerializeField] private Stamina playerStamina;

    private void Start()
    {
        if (playerStamina == null) { Debug.LogError($"{name}: falta referencia a Stamina"); return; }
        playerStamina.OnChanged += UpdateBar;
        UpdateBar(playerStamina.Current, playerStamina.Max);
    }

    private void OnDestroy()
    {
        if (playerStamina != null)
            playerStamina.OnChanged -= UpdateBar;
    }

    public void Connect(Stamina stamina)
    {
        if (playerStamina != null)
            playerStamina.OnChanged -= UpdateBar;
        playerStamina = stamina;
        if (playerStamina != null)
        {
            playerStamina.OnChanged += UpdateBar;
            UpdateBar(playerStamina.Current, playerStamina.Max);
        }
    }

    private void UpdateBar(float current, float max)
    {
        fillBar.fillAmount = current / max;
    }
}
