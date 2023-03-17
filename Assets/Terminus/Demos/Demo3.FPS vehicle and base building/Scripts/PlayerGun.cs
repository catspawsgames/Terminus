using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class PlayerGun : MonoBehaviour {

		public Transform spawner;
		public GameObject projectile;
		public Rigidbody shooterRigidbody;
		public TerminusObject shooterTerminusObject;
		public TerminusObject recoilTerminusObject;
		public float force;
		public float reloadDelay;

		protected float reloadTimer = 0;
		protected AudioSource audioSource;

		public void Fire()
		{
			if (reloadTimer <= 0)
			{
				reloadTimer = reloadDelay;
				GameObject obj = GameObject.Instantiate(projectile, spawner.position,spawner.rotation);
				Rigidbody projRbody = obj.GetComponent<Rigidbody>();

				if (shooterRigidbody != null)
					projRbody.velocity = shooterRigidbody.velocity;
				else if (shooterTerminusObject != null && shooterTerminusObject.mainOrWeldedRigidbody != null)
					projRbody.velocity = shooterTerminusObject.mainOrWeldedRigidbody.velocity;
				
				projRbody.AddForce(spawner.forward * force,ForceMode.Impulse);
			}
			if (recoilTerminusObject != null && recoilTerminusObject.mainOrWeldedRigidbody != null)
				recoilTerminusObject.mainOrWeldedRigidbody.AddForceAtPosition(-spawner.forward * force, spawner.position, ForceMode.Impulse);
			if (audioSource != null)
				audioSource.Play();
		}

		void Update ()
		{
			if (reloadTimer > 0)
				reloadTimer -= Time.deltaTime;
		}

		void Awake ()
		{
			audioSource = GetComponent<AudioSource>();
		}

	}
}