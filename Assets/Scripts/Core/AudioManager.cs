using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton de audio. Se autoarranca antes de la primera escena (no hay que montarlo
// en ninguna escena) y persiste con DontDestroyOnLoad. Carga los clips por clave desde
// Resources/Audio y los cachea. API estatica: AudioManager.PlayMusic / PlaySFX.
// La musica de World la pone su propio AudioSource ambiental en la escena; aqui solo
// gestionamos menu/historia y los SFX, y paramos la musica al entrar a World para no
// solaparla con la ambiental.
public class AudioManager : MonoBehaviour
{
    private const string RutaClips = "Audio/";
    private const string EscenaMenu = "MainMenu";
    private const string EscenaJuego = "World";

    private static AudioManager instancia;

    private AudioSource fuenteMusica;
    private AudioSource fuenteSfx;
    private AudioSource fuentePasos;
    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private string musicaActual;
    private string pasosActual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Inicializar()
    {
        if (instancia != null) return;
        var go = new GameObject("AudioManager");
        instancia = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.loop = true;
        fuenteMusica.playOnAwake = false;
        fuenteMusica.volume = 0.5f;
        fuenteSfx = gameObject.AddComponent<AudioSource>();
        fuenteSfx.playOnAwake = false;
        fuenteSfx.volume = 0.85f;
        fuentePasos = gameObject.AddComponent<AudioSource>();
        fuentePasos.loop = true;
        fuentePasos.playOnAwake = false;
        fuentePasos.volume = 0.5f;
        SceneManager.sceneLoaded += AlCargarEscena;
        MusicaDeEscena(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= AlCargarEscena;

    private void AlCargarEscena(Scene escena, LoadSceneMode modo) => MusicaDeEscena(escena.name);

    private void MusicaDeEscena(string nombre)
    {
        if (nombre == EscenaMenu) ReproducirMusica(Sonidos.Musica.Menu);
        else if (nombre == EscenaJuego) DetenerMusica(); // World usa su AudioSource ambiental
    }

    // --- API estatica ---

    public static void PlayMusic(string clave) { if (instancia != null) instancia.ReproducirMusica(clave); }
    public static void PlaySFX(string clave) { if (instancia != null) instancia.ReproducirSfx(clave); }

    // Pasos en bucle: suena mientras el jugador camina (PlayPasos cada frame) y para al detenerse.
    public static void PlayPasos(string clave) { if (instancia != null) instancia.ReproducirPasos(clave); }
    public static void StopPasos() { if (instancia != null) instancia.DetenerPasos(); }

    // Voz del enemigo al actuar (atacar), segun su prefab (mapa nombre -> clip). Silencioso si no hay match.
    public static void PlayVozEnemigo(string nombreObjeto)
    {
        if (instancia != null) instancia.ReproducirSfx(ClaveEnemigo(nombreObjeto));
    }

    private void ReproducirMusica(string clave)
    {
        if (clave == musicaActual && fuenteMusica.isPlaying) return;
        AudioClip clip = Cargar(clave);
        if (clip == null) return;
        musicaActual = clave;
        fuenteMusica.clip = clip;
        fuenteMusica.Play();
    }

    private void DetenerMusica()
    {
        fuenteMusica.Stop();
        musicaActual = null;
    }

    private void ReproducirSfx(string clave)
    {
        AudioClip clip = Cargar(clave);
        if (clip != null) fuenteSfx.PlayOneShot(clip);
    }

    private void ReproducirPasos(string clave)
    {
        if (clave == pasosActual && fuentePasos.isPlaying) return;
        AudioClip clip = Cargar(clave);
        if (clip == null) return;
        pasosActual = clave;
        fuentePasos.clip = clip;
        fuentePasos.Play();
    }

    private void DetenerPasos()
    {
        if (!fuentePasos.isPlaying) return;
        fuentePasos.Stop();
        pasosActual = null;
    }

    private AudioClip Cargar(string clave)
    {
        if (string.IsNullOrEmpty(clave)) return null;
        if (cache.TryGetValue(clave, out AudioClip clip)) return clip;
        clip = Resources.Load<AudioClip>(RutaClips + clave);
#if UNITY_EDITOR
        if (clip == null) Debug.LogWarning($"AudioManager: no encontre el clip '{clave}' en Resources/Audio");
#endif
        cache[clave] = clip; // cachear incluso null para no reintentar cada vez
        return clip;
    }

    // Mapa de prefab de enemigo -> clip de voz. Diccionario, no strings sueltos (regla del proyecto).
    private static readonly Dictionary<string, string> sfxEnemigos = new Dictionary<string, string>
    {
        { "Enemy_Skull", "skull" }, { "Enemy_Skull_Nuevo", "skull" },
        { "Enemy_Snake", "snake" }, { "Enemy_Spider", "spider attack 1" },
        { "Bear", "bear" }, { "Enemy_Panda", "panda" }, { "Enemy_Gnome", "gnomo" },
        { "Enemy_Gnoll", "hiena" }, { "Enemy_Lizard", "crocodile" },
        { "Enemy_Thief", "assassin" }, { "Enemy_Troll", "troll attack" },
        { "Enemy_Shaman", "wizard" }, { "Enemy_Purple", "goblin gruñe" },
        { "Enemy_Archer", "goblin gruñe" },
    };

    private static string ClaveEnemigo(string nombre)
    {
        nombre = nombre.Replace("(Clone)", "").Trim();
        int espacio = nombre.LastIndexOf(' '); // "Enemy_Purple 1" -> "Enemy_Purple"
        if (espacio > 0 && int.TryParse(nombre.Substring(espacio + 1), out _))
            nombre = nombre.Substring(0, espacio);
        return sfxEnemigos.TryGetValue(nombre, out string clave) ? clave : null;
    }
}
