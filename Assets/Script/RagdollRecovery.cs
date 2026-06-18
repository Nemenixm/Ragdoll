using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RagdollRecovery : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;

    [SerializeField]
    private StarterAssets.ThirdPersonController playerController;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody hips;

    [Header("Animator")]
    [SerializeField] private string getUpTriggerName = "GetUp";
    [SerializeField] private string getUpStateName = "Kip Up";

    [Header("Detección de reposo")]
    [SerializeField] private float minimumRagdollTime = 1.5f;
    [SerializeField] private float velocityThreshold = 0.15f;
    [SerializeField] private float angularVelocityThreshold = 0.5f;
    [SerializeField] private float timeStillRequired = 1f;

    [Header("Suelo")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundRayHeight = 2f;
    [SerializeField] private float groundRayDistance = 6f;
    [SerializeField] private float groundOffset = 0.03f;

    private StarterAssets.StarterAssetsInputs starterInputs;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput playerInput;
#endif

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    private bool ragdollActive;
    private bool recovering;

    private float ragdollStartTime;
    private float stillTimer;

    public bool IsRagdollActive => ragdollActive;
    public bool IsRecovering => recovering;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerController == null)
            playerController =
                GetComponent<StarterAssets.ThirdPersonController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        starterInputs =
            GetComponent<StarterAssets.StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
        playerInput = GetComponent<PlayerInput>();
#endif

        FindRagdollParts();

        // Al comenzar, el Animator controla el esqueleto.
        SetRagdollPhysics(false);

        if (animator != null)
        {
            animator.enabled = true;
            animator.applyRootMotion = false;
        }

        if (characterController != null)
            characterController.enabled = true;

        if (playerController != null)
            playerController.enabled = true;
    }

    private void Update()
    {
        if (!ragdollActive || recovering)
            return;

        // Evita levantarse nada más empezar a volar.
        if (Time.time - ragdollStartTime < minimumRagdollTime)
            return;

        if (RagdollIsStill())
        {
            stillTimer += Time.deltaTime;

            if (stillTimer >= timeStillRequired)
                StartCoroutine(GetUpRoutine());
        }
        else
        {
            stillTimer = 0f;
        }
    }

    private void FindRagdollParts()
    {
        Rigidbody[] allBodies =
            GetComponentsInChildren<Rigidbody>(true);

        List<Rigidbody> validBodies = new List<Rigidbody>();

        foreach (Rigidbody body in allBodies)
        {
            if (body == null)
                continue;

            // Excluye cualquier Rigidbody colocado en la raíz.
            if (body.transform == transform)
                continue;

            validBodies.Add(body);
        }

        ragdollBodies = validBodies.ToArray();

        Collider[] allColliders =
            GetComponentsInChildren<Collider>(true);

        List<Collider> validColliders = new List<Collider>();

        foreach (Collider currentCollider in allColliders)
        {
            if (currentCollider == null)
                continue;

            // El CharacterController no forma parte del ragdoll.
            if (currentCollider == characterController)
                continue;

            // Conserva cualquier collider colocado en la raíz.
            if (currentCollider.transform == transform)
                continue;

            validColliders.Add(currentCollider);
        }

        ragdollColliders = validColliders.ToArray();
    }

    /// <summary>
    /// Activa el ragdoll y aplica la explosión.
    /// Este método debe ser llamado por la mina.
    /// </summary>
    public void Explode(
        Vector3 explosionPosition,
        float explosionForce,
        float explosionRadius,
        float upwardsModifier)
    {
        if (ragdollActive || recovering)
            return;

        StopAllCoroutines();

        ragdollActive = true;
        recovering = false;

        stillTimer = 0f;
        ragdollStartTime = Time.time;

        // Primero se corta cualquier movimiento del jugador.
        ClearPlayerInput();

        if (playerController != null)
            playerController.enabled = false;

#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
            playerInput.enabled = false;
#endif

        if (characterController != null)
            characterController.enabled = false;

        if (animator != null)
            animator.enabled = false;

        // Ahora las físicas toman el control de los huesos.
        SetRagdollPhysics(true);

        Physics.SyncTransforms();

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            body.AddExplosionForce(
                explosionForce,
                explosionPosition,
                explosionRadius,
                upwardsModifier,
                ForceMode.Impulse
            );
        }
    }

    private bool RagdollIsStill()
    {
        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null)
                continue;

            if (body.linearVelocity.magnitude > velocityThreshold)
                return false;

            if (body.angularVelocity.magnitude >
                angularVelocityThreshold)
            {
                return false;
            }
        }

        return IsNearGround();
    }

    private bool IsNearGround()
    {
        if (hips == null)
            return true;

        LayerMask mask = groundLayers;

        if (mask.value == 0 && playerController != null)
            mask = playerController.GroundLayers;

        Vector3 origin =
            hips.worldCenterOfMass + Vector3.up * 0.25f;

        return Physics.Raycast(
            origin,
            Vector3.down,
            groundRayDistance,
            mask,
            QueryTriggerInteraction.Ignore
        );
    }

    private IEnumerator GetUpRoutine()
    {
        recovering = true;
        ragdollActive = false;

        Vector3 hipsPosition = hips.position;

        Vector3 targetPosition = transform.position;
        targetPosition.x = hipsPosition.x;
        targetPosition.z = hipsPosition.z;

        LayerMask mask = groundLayers;

        if (mask.value == 0 && playerController != null)
            mask = playerController.GroundLayers;

        Vector3 rayOrigin =
            hipsPosition + Vector3.up * groundRayHeight;

        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                groundRayDistance,
                mask,
                QueryTriggerInteraction.Ignore))
        {
            targetPosition.y =
                groundHit.point.y + groundOffset;
        }
        else if (characterController != null)
        {
            // Plan B por si el Raycast no encuentra suelo.
            targetPosition.y =
                hipsPosition.y - characterController.center.y;
        }

        // Detenemos completamente las físicas antes del Animator.
        SetRagdollPhysics(false);

        transform.position = targetPosition;

        // Evita que el objeto raíz se quede inclinado.
        transform.rotation = Quaternion.Euler(
            0f,
            transform.eulerAngles.y,
            0f
        );

        Physics.SyncTransforms();

        ClearPlayerInput();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.enabled = true;

            // Limpia la pose física anterior.
            animator.Rebind();
            animator.Update(0f);

            ResetAnimatorParameters();

            animator.ResetTrigger(getUpTriggerName);
            animator.SetTrigger(getUpTriggerName);
        }

        yield return null;

        // Espera a que el Animator entre en Kip Up.
        float enterTimeout = 2f;
        bool enteredGetUpState = false;

        while (enterTimeout > 0f)
        {
            AnimatorStateInfo currentState =
                animator.GetCurrentAnimatorStateInfo(0);

            AnimatorStateInfo nextState =
                animator.GetNextAnimatorStateInfo(0);

            if (currentState.IsName(getUpStateName) ||
                nextState.IsName(getUpStateName))
            {
                enteredGetUpState = true;
                break;
            }

            enterTimeout -= Time.deltaTime;
            yield return null;
        }

        if (!enteredGetUpState)
        {
            Debug.LogWarning(
                $"No se ha podido entrar en el estado " +
                $"'{getUpStateName}'. Revisa el Trigger " +
                $"'{getUpTriggerName}' y la transición del Animator.",
                this
            );
        }
        else
        {
            // Espera a que termine la animación.
            float animationTimeout = 10f;

            while (animationTimeout > 0f)
            {
                AnimatorStateInfo state =
                    animator.GetCurrentAnimatorStateInfo(0);

                bool isGetUpState =
                    state.IsName(getUpStateName);

                if (isGetUpState &&
                    state.normalizedTime >= 0.95f)
                {
                    break;
                }

                // También termina si ya salió del estado.
                if (!isGetUpState &&
                    !animator.IsInTransition(0))
                {
                    break;
                }

                animationTimeout -= Time.deltaTime;
                yield return null;
            }
        }

        ClearPlayerInput();
        ResetAnimatorParameters();

        // Primero vuelve la cápsula.
        if (characterController != null)
            characterController.enabled = true;

        Physics.SyncTransforms();

        yield return null;

#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
            playerInput.enabled = true;
#endif

        ClearPlayerInput();

        // Y al final devolvemos el control.
        if (playerController != null)
            playerController.enabled = true;

        recovering = false;
        stillTimer = 0f;
    }

    private void SetRagdollPhysics(bool enabled)
    {
        if (ragdollBodies != null)
        {
            foreach (Rigidbody body in ragdollBodies)
            {
                if (body == null)
                    continue;

                if (!enabled)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = !enabled;
                body.useGravity = enabled;
                body.detectCollisions = enabled;
            }
        }

        if (ragdollColliders != null)
        {
            foreach (Collider currentCollider in ragdollColliders)
            {
                if (currentCollider != null)
                    currentCollider.enabled = enabled;
            }
        }
    }

    private void ClearPlayerInput()
    {
        if (starterInputs == null)
            return;

        starterInputs.move = Vector2.zero;
        starterInputs.look = Vector2.zero;
        starterInputs.jump = false;
        starterInputs.sprint = false;
    }

    private void ResetAnimatorParameters()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", 0f);
        animator.SetFloat("MotionSpeed", 0f);

        animator.SetBool("Jump", false);
        animator.SetBool("FreeFall", false);
        animator.SetBool("Grounded", true);
    }
}