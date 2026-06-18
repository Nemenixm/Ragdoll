using System.Collections;
using UnityEngine;

public class MineExplosion : MonoBehaviour
{
    [Header("Explosión")]
    [SerializeField] private float explosionForce = 15f;
    [SerializeField] private float explosionRadius = 7f;
    [SerializeField] private float upwardsModifier = 2.5f;

    private bool exploded;

    private void OnTriggerEnter(Collider other)
    {
        if (exploded)
            return;

        RagdollRecovery recovery =
            other.GetComponentInParent<RagdollRecovery>();

        if (recovery == null)
            return;

        if (!recovery.CompareTag("Player"))
            return;

        exploded = true;

        // RagdollRecovery se encarga de absolutamente todo:
        // controles, Animator, CharacterController y rigidbodies.
        recovery.Explode(
            transform.position,
            explosionForce,
            explosionRadius,
            upwardsModifier
        );

        StartCoroutine(DestroyMine());
    }

    private IEnumerator DestroyMine()
    {
        Collider mineCollider = GetComponent<Collider>();

        if (mineCollider != null)
            mineCollider.enabled = false;

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();

        foreach (Renderer currentRenderer in renderers)
        {
            currentRenderer.enabled = false;
        }

        // Permite que Unity procese la fuerza física.
        yield return new WaitForFixedUpdate();

        Destroy(gameObject);
    }
}