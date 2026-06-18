using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTargetFollow : MonoBehaviour
{
    [Header("Referencia")]
    public Transform playerChest;

    [Header("Configuración de Distancia")]
    [Tooltip("La distancia fija que mantendrá la cámara respecto al jugador")]
    public float distance = 6f;

    [Tooltip("Desfase vertical para que la cámara no apunte directamente a los pies")]
    public float heightOffset = 1.5f;

    [Header("Control del Ratón")]
    public float sensitivityX = 0.1f;
    public float sensitivityY = 0.1f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch;

    private InputAction lookAction;

    private void Awake()
    {
        lookAction = new InputAction(
            name: "Look",
            type: InputActionType.Value,
            binding: "<Mouse>/delta"
        );
    }

    private void OnEnable()
    {
        lookAction.Enable();
    }

    private void OnDisable()
    {
        lookAction.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void LateUpdate()
    {
        if (playerChest == null)
            return;

        // Leer movimiento del ratón
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();

        yaw += mouseDelta.x * sensitivityX;
        pitch -= mouseDelta.y * sensitivityY;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 targetPosition =
            playerChest.position + Vector3.up * heightOffset;

        Vector3 finalPosition =
            targetPosition + rotation * new Vector3(0, 0, -distance);

        transform.position = finalPosition;
        transform.LookAt(targetPosition);
    }
}