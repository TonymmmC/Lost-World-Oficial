using UnityEngine;
using System;

// Cuenta los enemigos guardianes fijos de la zona. Cuando todos mueren,
// muestra el panel de victoria y pausa el juego.
public class QuestManager : MonoBehaviour
{
    [SerializeField] private Health[] guardianes;
    [SerializeField] private GameObject victoryPanel;

    private int restantes;
    private int total;

    public event Action<int, int> OnProgreso;

    private void Start()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        foreach (Health h in guardianes)
        {
            if (h == null) continue;
            h.OnDeath += ManejarMuerteGuardian;
            restantes++;
        }
        total = restantes;
        OnProgreso?.Invoke(restantes, total);
    }

    private void OnDestroy()
    {
        foreach (Health h in guardianes)
            if (h != null) h.OnDeath -= ManejarMuerteGuardian;
    }

    private void ManejarMuerteGuardian()
    {
        restantes = Mathf.Max(0, restantes - 1);
        OnProgreso?.Invoke(restantes, total);
        if (restantes <= 0)
            MostrarVictoria();
    }

    private void MostrarVictoria()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}
