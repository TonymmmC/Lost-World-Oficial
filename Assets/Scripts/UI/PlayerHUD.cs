using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private RectTransform fillBar;
    [SerializeField] private Health playerHealth;

    private void Start()
    {
        playerHealth.OnChanged += UpdateBar;
        UpdateBar(playerHealth.Current, playerHealth.Max);
    }

    private void UpdateBar(int current, int max)
    {
        float pct = (float)current / max;
        fillBar.localScale = new Vector3(pct, 1f, 1f);
    }
}
