using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    [Header("Zoom")]
    [SerializeField] private float normalSize = 5f;
    [SerializeField] private float zoomedOutSize = 12f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private KeyCode zoomKey = KeyCode.Tab;

    private Camera cam;
    private bool isZoomedOut;
    private float shakeIntensidad;
    private float shakeTimer;
    private float shakeDuracionTotal;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Vibra la camara brevemente (impactos fuertes, muerte de jefe). Usa tiempo real
    // para seguir funcionando durante el hit stop (Time.timeScale = 0).
    public void Sacudir(float intensidad, float duracion)
    {
        if (shakeTimer > 0f && intensidad <= shakeIntensidad) return;
        shakeIntensidad = intensidad;
        shakeTimer = duracion;
        shakeDuracionTotal = duracion;
    }

    private Vector3 CalcularSacudida()
    {
        if (shakeTimer <= 0f) return Vector3.zero;
        shakeTimer -= Time.unscaledDeltaTime;
        float fuerza = shakeIntensidad * (shakeTimer / shakeDuracionTotal);
        return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * fuerza;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.position += CalcularSacudida();

        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.selectButton.wasPressedThisFrame)
            isZoomedOut = !isZoomedOut;

        if (Input.GetKeyDown(zoomKey))
            isZoomedOut = !isZoomedOut;

        float targetSize = isZoomedOut ? zoomedOutSize : normalSize;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
    }
}
