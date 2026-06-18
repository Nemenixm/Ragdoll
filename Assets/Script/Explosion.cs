using UnityEngine;
using System;

public class Explosion : MonoBehaviour
{
	#region Properties
	#endregion

	#region Fields
	[SerializeField] private float _explosionArea;
	[SerializeField] private float _explosionForce = 1000;
	[SerializeField] private GameObject _effect;
	#endregion

	#region Unity Callbacks

	private void OnTriggerEnter(Collider other)
	{
		_effect.SetActive(true);
		Animator playeAnim = other.GetComponentInParent<Animator>();
		if (playeAnim != null)
			playeAnim.enabled = false;

		ExplosionForce();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, _explosionArea);
	}
	#endregion

	#region Public Methods
	#endregion

	#region Private Methods
	private void ExplosionForce()
	{
		Collider[] objects = Physics.OverlapSphere(transform.position, _explosionArea);
		for (int i = 0; i < objects.Length; i++)
		{
			Rigidbody objectRB = objects[i].GetComponent<Rigidbody>();
			if (objectRB != null)
			{
				objectRB.AddExplosionForce(_explosionForce, transform.position, _explosionArea);
			}
		}
	}


	#endregion
}