using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Distance")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 10f;

    [Header("Rotation")]
    public float rotationSpeed = 180f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Smooth")]
    public float smoothSpeed = 12f;

    private float yaw;
    private float pitch = 20f;

    private InputAction lookAction;
    private InputAction scrollAction;

    private void Awake()
    {
        // Mouse delta (movimiento cámara)
        lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        scrollAction = new InputAction("Scroll", binding: "<Mouse>/scroll");
    }

    private void OnEnable()
    {
        lookAction.Enable();
        scrollAction.Enable();
    }

    private void OnDisable()
    {
        lookAction.Disable();
        scrollAction.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (!player) return;

        Vector2 look = lookAction.ReadValue<Vector2>();
        Vector2 scroll = scrollAction.ReadValue<Vector2>();

        // 🎯 Rotación cámara
        yaw += look.x * rotationSpeed * Time.deltaTime;
        pitch -= look.y * rotationSpeed * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 🔭 Zoom
        distance -= scroll.y * 0.01f;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 desiredPosition =
            player.position - (rotation * Vector3.forward * distance);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt(player.position);
    }
}