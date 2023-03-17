using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class Projectile : MonoBehaviour {

		public float damage = 100;
		public float force = 200;
		public GameObject onHitPrefab;
		public float onHitDestroyTimer = 5;
		public ParticleSystem[] particleSystems;
		public float lifetime = 10;
		public float destructTimer = 2;
		public float explosionRadius = 1.5f;
		public float explosionForce = 200;
		public AnimationCurve explosionCurve;


		protected bool active = true;

		void OnTriggerEnter (Collider other)
		{
			if (!active)
				return;
			
			active = false;

			if (other.attachedRigidbody != null)			
				other.attachedRigidbody.AddForceAtPosition(transform.forward * force, transform.position + (other.transform.position - transform.position).normalized * 0.3f, ForceMode.Impulse);
			
			//other.gameObject.SendMessage("TakeDamage",damage,SendMessageOptions.DontRequireReceiver);

			RaycastHit[] hits = Physics.SphereCastAll(transform.position+transform.forward * explosionRadius * 0.25f,explosionRadius,Vector3.forward,0);
			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].rigidbody != null)
				{
					Vector3 hitPos = transform.position + (hits[i].transform.position - transform.position).normalized * 0.3f;
					float forceCoef = explosionCurve.Evaluate((hitPos - transform.position).magnitude / explosionRadius);
					hits[i].rigidbody.AddForceAtPosition((transform.forward + (hits[i].transform.position - transform.position).normalized).normalized * explosionForce * forceCoef, hitPos, ForceMode.Impulse);
				}
			}

			Rigidbody rbody = gameObject.GetComponent<Rigidbody>();

			if (onHitPrefab != null)
			{
				RaycastHit hit;
				other.Raycast(new Ray(transform.position - rbody.velocity * 10, rbody.velocity * 11), out hit, 10000);

				Quaternion rotation;
				if (hit.normal == Vector3.zero)
					rotation = transform.rotation;				
				else if (hit.normal != Vector3.up)
					rotation = Quaternion.LookRotation(hit.normal,Vector3.up);
				else
					rotation = Quaternion.LookRotation(hit.normal,Vector3.forward);

				GameObject obj = GameObject.Instantiate(onHitPrefab,transform.position,rotation);
				GameObject.Destroy(obj,onHitDestroyTimer);
			}

			GameObject.Destroy(gameObject.GetComponent<Collider>());

			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem.EmissionModule em = particleSystems[i].emission;
				em.enabled = false;
			}

			rbody.velocity = Vector3.zero;

			Destroy(gameObject,destructTimer);

			DestructableObject destrObj = other.gameObject.GetComponent<DestructableObject>();
			if (destrObj != null)
				destrObj.TakeDamage(damage);			
		}

		void Awake()
		{
			GameObject.Destroy(gameObject,lifetime);
		}

	}
}