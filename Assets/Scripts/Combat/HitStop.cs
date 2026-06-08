using UnityEngine;

// Congela brevemente el juego al conectar un golpe para dar peso al impacto.
// Colocar este componente en un unico objeto persistente de la escena.
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private float timer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Aplicar(float duracion)
    {
        timer = duracion;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (timer <= 0f) return;
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
            Time.timeScale = 1f;
    }
}
