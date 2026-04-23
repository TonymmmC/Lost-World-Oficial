using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    [Header("Zoom")]
    [SerializeField] private float normalSize = 5f;
    [SerializeField] private float zoomedOutSize = 12f;
    [SerializeField] private float zoomSpeed = 5f;

    private Camera cam;
    private bool isZoomedOut;

    private void Awake() => cam = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.selectButton.wasPressedThisFrame)
            isZoomedOut = !isZoomedOut;

        float targetSize = isZoomedOut ? zoomedOutSize : normalSize;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
    }
}
